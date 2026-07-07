using EasyManufacture.Licence;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 机器授权读写（/APSAPI/GetRegister、/APSAPI/SeRegister）。
/// 实现在 Infrastructure，不放在 Api 宿主工程，便于 NuGet 封装对外隐藏。
/// </summary>
public partial class ApsCoreEngine
{
#if LEGACY_APS_CORE
    private JDRegister RegisterInstance => jDRegister;
#else
    private JDRegister RegisterInstance => _jdRegister;
#endif

    /// <summary>读取 CPU/MAC 与授权状态（注册前浏览器 GET 可访问）。</summary>
    public string GetRegister() => RegisterInstance.ToStatusJson();

    /// <summary>写入 register.ini（Query / Form / JSON Body 均可传 pwd、ssn）。</summary>
    public string SeRegister(string pwd = "", string ssn = "")
    {
        if (string.IsNullOrEmpty(pwd))
            pwd = LicenceRuntime.Http.HttpContext?.Request.GetRequestValue("pwd") ?? string.Empty;
        if (string.IsNullOrEmpty(ssn))
            ssn = LicenceRuntime.Http.HttpContext?.Request.GetRequestValue("ssn") ?? string.Empty;

        RegisterInstance.Registration(pwd, ssn);
        return RegisterInstance.ToStatusJson();
    }
}
