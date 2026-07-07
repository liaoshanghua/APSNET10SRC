using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EasyManufacture.Licence;

/// <summary>
/// 单机部署登记（Kestrel 自宿主）：按<strong>安装目录</strong>计数，上限写死在代码里。
/// 登记文件带 HMAC，直接改 JSON 无法通过校验。
/// </summary>
internal static class LicenceInstanceRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string RegistryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EasyManufacture",
            "aps-deployments.v1.json");

    private sealed class DeployRecord
    {
        public string Path { get; set; } = "";
        public int Pid { get; set; }
        public string UpdatedUtc { get; set; } = "";
        public string Sig { get; set; } = "";
    }

    private sealed class RegistryFile
    {
        public List<DeployRecord> Instances { get; set; } = [];
    }

    /// <param name="machineKey">MAC+CPU 等机器指纹，与 register.ini 绑定一致。</param>
    public static bool TryRegisterDeployment(string contentRoot, string machineKey, out int activeCount, out string message)
    {
        activeCount = 0;
        message = "";

        if (string.IsNullOrWhiteSpace(machineKey))
        {
            message = "机器指纹无效";
            return false;
        }

        var normalizedRoot = NormalizePath(contentRoot);
        var registry = LoadAndPrune(machineKey, out activeCount);

        var existing = registry.Instances.FirstOrDefault(r => PathsEqual(r.Path, normalizedRoot));
        if (existing != null)
        {
            existing.Pid = Environment.ProcessId;
            existing.UpdatedUtc = DateTime.UtcNow.ToString("o");
            existing.Sig = Sign(existing.Path, existing.Pid, machineKey);
            Save(registry);
            activeCount = registry.Instances.Count;
            return true;
        }

        if (registry.Instances.Count >= LicenceDeployLimits.MaxInstancesPerMachine)
        {
            var occupied = string.Join("; ", registry.Instances.Select(i => i.Path));
            message =
                $"本机已有 {registry.Instances.Count} 套 APS 在运行（上限 {LicenceDeployLimits.MaxInstancesPerMachine}）：{occupied}";
            activeCount = registry.Instances.Count;
            return false;
        }

        var record = new DeployRecord
        {
            Path = normalizedRoot,
            Pid = Environment.ProcessId,
            UpdatedUtc = DateTime.UtcNow.ToString("o"),
            Sig = Sign(normalizedRoot, Environment.ProcessId, machineKey)
        };
        registry.Instances.Add(record);
        Save(registry);
        activeCount = registry.Instances.Count;
        return true;
    }

    public static int CountActiveDeployments(string machineKey)
    {
        LoadAndPrune(machineKey, out var count);
        return count;
    }

    private static RegistryFile LoadAndPrune(string machineKey, out int activeCount)
    {
        activeCount = 0;
        var registry = LoadRaw();
        var before = registry.Instances.Count;
        var valid = new List<DeployRecord>();

        foreach (var rec in registry.Instances)
        {
            if (string.IsNullOrWhiteSpace(rec.Path))
                continue;

            if (!VerifySig(rec, machineKey))
                continue;

            if (!IsProcessAlive(rec.Pid))
                continue;

            valid.Add(rec);
        }

        registry.Instances = valid;
        activeCount = valid.Count;

        if (valid.Count != before)
            Save(registry);

        return registry;
    }

    private static string Sign(string path, int pid, string machineKey)
    {
        var payload = $"{NormalizePath(path)}|{pid}|{machineKey}";
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("EM-ApsDeploy|" + machineKey));
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static bool VerifySig(DeployRecord rec, string machineKey) =>
        string.Equals(rec.Sig, Sign(rec.Path, rec.Pid, machineKey), StringComparison.OrdinalIgnoreCase);

    private static bool IsProcessAlive(int pid)
    {
        if (pid <= 0) return false;
        try
        {
            var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd('\\', '/');

    private static bool PathsEqual(string a, string b) =>
        string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);

    private static RegistryFile LoadRaw()
    {
        try
        {
            if (!File.Exists(RegistryPath))
                return new RegistryFile();

            return JsonSerializer.Deserialize<RegistryFile>(File.ReadAllText(RegistryPath)) ?? new RegistryFile();
        }
        catch
        {
            return new RegistryFile();
        }
    }

    private static void Save(RegistryFile registry)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath)!);
        File.WriteAllText(RegistryPath, JsonSerializer.Serialize(registry, JsonOptions));
    }
}
