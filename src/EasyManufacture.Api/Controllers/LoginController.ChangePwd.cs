using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>修改密码（旧 <c>ChangePwd</c>）。</summary>
    [HttpPost]
    public IActionResult ChangePwd()
    {
        var legacy = V_Dev_Account.GetDev_Account();
        if (legacy == null)
            return new JsonResult(new { msg = "未登录", result = false });

        try
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(_body.BodyJson);
            if (jObject == null)
                return new JsonResult(new { msg = "未接收到数据", result = false });

            var pwd = legacy.Pwd?.ToLowerInvariant() ?? string.Empty;
            var pwd1 = jObject["pwd1"]?.ToString().ToLowerInvariant() ?? string.Empty;
            var pwd2 = jObject["pwd2"]?.ToString() ?? string.Empty;
            var pwd3 = jObject["pwd3"]?.ToString() ?? string.Empty;

            if (pwd != pwd1)
                return new JsonResult(new { msg = "旧密码输入错误", result = false });

            if (string.IsNullOrWhiteSpace(pwd2))
                return new JsonResult(new { msg = "新密码不能为空", result = false });

            if (pwd2 != pwd3)
                return new JsonResult(new { msg = "新密码不一致", result = false });

            SqlHelper.ExecuteNonQuery(
                SqlHelper.MSSQLConnectionString,
                System.Data.CommandType.Text,
                $"UPDATE Dev_Account SET Pwd='{StringHelper.ReplaceSQL(pwd2)}' WHERE Account='{StringHelper.ReplaceSQL(legacy.Account)}'");

            legacy.Pwd = pwd2;
            V_Dev_Account.SetDev_Account(HttpContext, legacy);

            return new JsonResult(new { msg = "密码修改成功", result = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { msg = "密码保存失败，错误信息：" + ex.Message, result = false });
        }
    }
}
