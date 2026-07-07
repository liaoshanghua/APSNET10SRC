using EasyManufacture.Infrastructure.Legacy;

namespace EasyManufacture.Infrastructure.SystemInterface.EAST;

/// <summary>兼容旧 EAST SRM 推送（编译桩）。</summary>
public class SRM
{
    public static JsonInterFace PushSRM(string account) =>
        new() { code = "400", message = "SRM 未配置" };
}
