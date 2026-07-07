namespace EasyManufacture.Api.Infrastructure;

/// <summary>wwwroot 启动预压缩配置（绑定 ResponseCompression 节点）。</summary>
internal sealed class WwwrootCompressOptions
{
    public bool Enabled { get; set; } = true;
    public bool PreCompressedStaticFiles { get; set; } = true;
    public bool AutoPrecompressOnStartup { get; set; } = true;
    public int AutoPrecompressMinSizeKB { get; set; } = 10;
    public string AutoPrecompressLevel { get; set; } = "Optimal";
}

internal readonly record struct WwwrootCompressResult(int Scanned, int Compressed, int Skipped);
