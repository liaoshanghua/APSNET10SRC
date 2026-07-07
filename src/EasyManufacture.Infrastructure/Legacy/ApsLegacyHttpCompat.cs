using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容 System.Web.HttpPostedFileBase（上传接口从 Controller 注入）。</summary>
public class ApsHttpPostedFileBase
{
    public IFormFile? FormFile { get; init; }
    public string? FileName => FormFile?.FileName;
    public Stream? InputStream => FormFile?.OpenReadStream();
    public int ContentLength => (int)(FormFile?.Length ?? 0);

    public void SaveAs(string path)
    {
        if (FormFile == null)
            throw new InvalidOperationException("未绑定上传文件。");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        using var stream = System.IO.File.Create(path);
        FormFile.CopyTo(stream);
    }
}
