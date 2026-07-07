using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Application.Security;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>JSON 登录（旧 <c>CheckAccountJson</c>，简化版无菜单 enrichment）。</summary>
    [HttpPost]
    public async Task<IActionResult> CheckAccountJson(CancellationToken cancellationToken = default)
    {
        string? token = null;
        try
        {
            if (IsExplicitLoginAttempt())
                ClearLoginState();

            var (ok, msg, account) = await _licenceLogin.LoginCheckJsonAsync(
                HttpContext, _body.BodyJson, cancellationToken);

            V_Dev_Account? devAccount = account;
            if (devAccount != null)
            {
                token = DesCrypto.Encrypt(devAccount.Account);
                V_Dev_Account.SetDev_Account(HttpContext, devAccount);
                HttpContext.Session.SetString(AuthTokenResolver.SessionAccountKey, devAccount.Account);
            }
            else if (!ok && !AppInfo.XBLoginCheck)
            {
                ClearLoginState();
            }

            return new JsonResult(new
            {
                msg,
                result = ok,
                Account = devAccount?.Account ?? "",
                Name = devAccount?.Name ?? "",
                token,
                dev_Account = devAccount
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { msg = ex.Message, result = false, token });
        }
    }
}
