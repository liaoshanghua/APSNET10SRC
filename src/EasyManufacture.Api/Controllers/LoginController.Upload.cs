using EasyManufacture.Infrastructure.Legacy;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>上传头像（旧 <c>UploadImage</c>）。</summary>
    [HttpPost]
    public async Task<string> UploadImage([FromQuery] string account, IFormFile? file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return JsonConvert.SerializeObject(new { msg = "上传失败", result = false });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".jpg" and not ".png")
            {
                return JsonConvert.SerializeObject(new
                {
                    msg = "上传失败，只允许上传 jpg 和 png 类型的文件",
                    result = false
                });
            }

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AvatarForAll");
            Directory.CreateDirectory(folderPath);

            var fileName = account + ext;
            var physicalPath = Path.Combine(folderPath, fileName);
            await using (var stream = System.IO.File.Create(physicalPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var urlPath = "/AvatarForAll/" + fileName;
            SqlHelper.ExecuteNonQuery(
                SqlHelper.MSSQLConnectionString,
                System.Data.CommandType.Text,
                $@"
UPDATE A SET A.AvatarURL = '{StringHelper.ReplaceSQL(urlPath)}'
FROM Dev_Account A
WHERE A.Account = '{StringHelper.ReplaceSQL(account)}'");

            return JsonConvert.SerializeObject(new
            {
                msg = "上传成功",
                result = true,
                account,
                AvatarURL = urlPath,
                fileName
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { msg = ex.Message, result = false });
        }
    }
}
