namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容 System.Web.HttpServerUtility.MapPath。</summary>
public sealed class ApsLegacyServer
{
    public string MapPath(string virtualPath)
    {
        var path = virtualPath.Replace("~/", "").TrimStart('/', '\\');
        return Path.Combine(AppContext.BaseDirectory, path.Replace('/', Path.DirectorySeparatorChar));
    }
}
