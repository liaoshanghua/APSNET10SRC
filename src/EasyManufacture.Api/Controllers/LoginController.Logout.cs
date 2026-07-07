using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Entitys;
using Microsoft.AspNetCore.Mvc;

namespace EasyManufacture.Api.Controllers;

public partial class LoginController
{
    /// <summary>对齐旧 <c>APSAPI/CheckLogin</c>；<paramref name="loginOut"/> = 1 时清空 Session 与登录 Cookie。</summary>
    [HttpGet]
    [HttpPost]
    public string CheckLogin([FromQuery] string? loginOut)
    {
        if (loginOut == "1")
            LoginStateHelper.ClearLoginState(HttpContext, _currentUser, LoginStateHelper.ResolveAppCode(_appSettings));

        var devAccount = V_Dev_Account.GetDev_Account();
        return LoginStateHelper.BuildCheckLoginJson(devAccount, loginOut == "1");
    }
}
