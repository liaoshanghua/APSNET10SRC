namespace EasyManufacture.Licence;

/// <summary>单机部署上限（编译期常量，不可通过 appsettings 修改）。</summary>
internal static class LicenceDeployLimits
{
    /// <summary>同一台物理机最多允许运行的 APS 安装目录数（典型：生产 + 测试各 1 套）。</summary>
    public const int MaxInstancesPerMachine = 2;
}
