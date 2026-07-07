using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace EasyManufacture.Licence;

/// <summary>
/// 机器注册与授权（Kestrel 自宿主）。
/// 校验 register.ini 硬件绑定 + 单机最多 <see cref="LicenceDeployLimits.MaxInstancesPerMachine"/> 个安装目录。
/// </summary>
public class JDRegister
{
    public string CpuID { get; }
    public string MacAddress { get; }
    public string DiskID { get; }
    public string IpAddress { get; }
    public string LoginUserName { get; }
    public string ComputerName { get; }
    public string SystemType { get; }
    public string TotalPhysicalMemory { get; }

    /// <summary>当前 APS 安装目录。</summary>
    public string DeployPath { get; private set; } = "";

    /// <summary>本机当前运行的 APS 安装套数。</summary>
    public int DeploymentsInUse { get; set; }

    public string? SSN { get; set; }

    public string LastCheckMessage { get; private set; } = "";

    private readonly string _iniFile;
    private string _validityDate = string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
    private bool _isReg;
    private DateTime _lastCheckDate = DateTime.MinValue;

    public string ValidityDate => _validityDate;

    public double ValidityDays
    {
        get
        {
            if (string.IsNullOrEmpty(_validityDate)) return 0;
            return (DateTime.Parse(_validityDate) - DateTime.Now.Date).TotalDays;
        }
    }

    public bool IsRegister
    {
        get
        {
            if (!_isReg)
                Check();
            else if (_lastCheckDate.Date != DateTime.Now.Date)
            {
                Check();
                _lastCheckDate = DateTime.Now.Date;
            }
            return _isReg;
        }
    }

    public JDRegister()
    {
        CpuID = MachineFingerprint.GetCpuId();
        MacAddress = MachineFingerprint.GetMacAddress();
        DiskID = MachineFingerprint.GetDiskId();
        IpAddress = MachineFingerprint.GetIpAddress();
        LoginUserName = Environment.UserName;
        SystemType = MachineFingerprint.GetSystemType();
        TotalPhysicalMemory = MachineFingerprint.GetTotalPhysicalMemory();
        ComputerName = Environment.MachineName;
        DeployPath = Path.GetFullPath(LicenceRuntime.Environment?.ContentRootPath ?? AppContext.BaseDirectory)
            .TrimEnd('\\', '/');

        _iniFile = LicenceRuntime.MapContentPath("register.ini");
        if (!File.Exists(_iniFile))
            File.WriteAllText(_iniFile, "");

        Check();
    }

    public void Registration(string pwd, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            LastCheckMessage = "未提供授权码（ssn 为空），请访问 /APSAPI/SeRegister?ssn=授权码";
            Check();
            return;
        }

        key = key.Trim();
        try
        {
            File.WriteAllText(_iniFile, key, Encoding.Default);
            SSN = key;
        }
        catch (Exception ex)
        {
            LastCheckMessage = $"写入 register.ini 失败：{ex.Message}";
            _isReg = false;
            return;
        }

        Check();
    }

    public string ToStatusJson()
    {
        var hardwareOk = !string.IsNullOrEmpty(CpuID) && !string.IsNullOrEmpty(MacAddress);
        return JsonConvert.SerializeObject(new
        {
            CpuID,
            MacAddress,
            hardwareOk,
            hint = hardwareOk
                ? (string?)null
                : "无法读取 CPU/MAC：请确认 Windows Management Instrumentation 服务已启动；或以管理员打开 /APSAPI/GetRegister",
            DeployPath,
            DeploymentsInUse,
            maxDeployments = LicenceDeployLimits.MaxInstancesPerMachine,
            IsRegister = _isReg,
            registerIni = _iniFile,
            registerIniWritten = !string.IsNullOrWhiteSpace(
                File.Exists(_iniFile) ? File.ReadAllText(_iniFile, Encoding.Default) : ""),
            ValidityDate = _validityDate,
            ValidityDays,
            msg = LastCheckMessage,
            ComputerName,
            DiskID,
            IpAddress
        });
    }

    private void Check()
    {
        LastCheckMessage = "";
        _isReg = false;

#if DEBUG
        // 仅 Debug 编译本地联调免授权；Release 发布不存在此分支。
        if (string.Equals(LicenceRuntime.Environment?.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            _isReg = true;
            _validityDate = string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(300));
            LastCheckMessage = "DEBUG Development";
            return;
        }
#endif

        // 旧版 IIS：本机浏览器访问（127.0.0.1 / ::1）时跳过授权校验。
        if (LicenceRuntime.IsLoopbackClient)
        {
            _isReg = true;
            _validityDate = string.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(300));
            LastCheckMessage = "本机访问免授权";
            return;
        }

        var registerKey = File.Exists(_iniFile) ? File.ReadAllText(_iniFile, Encoding.Default) : "";
        if (string.IsNullOrWhiteSpace(registerKey))
        {
            LastCheckMessage = "尚未注册，请访问 /APSAPI/SeRegister?ssn=授权码 写入 register.ini";
            return;
        }

        registerKey = DesDecrypt(registerKey);
        var ss = registerKey.Split('$');
        if (ss.Length != 4)
        {
            LastCheckMessage = "register.ini 格式无效";
            return;
        }

        _validityDate = ss[3];

        if (!DateTime.TryParse(_validityDate, out var validityDate))
        {
            LastCheckMessage = "register.ini 授权日期无效";
            return;
        }

        if (validityDate.Date < DateTime.Now.Date)
        {
            LastCheckMessage = "软件授权已过期";
            return;
        }

        if (ss[0] != MacAddress || ss[1] != CpuID || ss[2] != "jdkj2020")
        {
            LastCheckMessage = string.IsNullOrEmpty(MacAddress) || string.IsNullOrEmpty(CpuID)
                ? "无法读取本机 MAC/CPU，不能校验授权，请先访问 /APSAPI/GetRegister 排查"
                : "授权与当前服务器硬件不匹配";
            return;
        }

        var machineKey = BuildMachineKey(ss[0], ss[1]);
        if (!LicenceInstanceRegistry.TryRegisterDeployment(DeployPath, machineKey, out var active, out var deployMsg))
        {
            DeploymentsInUse = active;
            LastCheckMessage = deployMsg;
            return;
        }

        DeploymentsInUse = active;
        _isReg = true;
        LastCheckMessage = "OK";
    }

    private static string BuildMachineKey(string mac, string cpu) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mac}|{cpu}|jdkj2020")));

    private static string DesDecrypt(string str)
    {
        try
        {
            using var des = DES.Create();
            var input = new byte[str.Length / 2];
            for (var x = 0; x < str.Length / 2; x++)
                input[x] = Convert.ToByte(str.Substring(x * 2, 2), 16);
            des.Key = Encoding.ASCII.GetBytes("jdkj2020");
            des.IV = des.Key;
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                cs.Write(input, 0, input.Length);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return str;
        }
    }
}
