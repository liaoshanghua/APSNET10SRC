using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Application.Security;
using EasyManufacture.Domain.Models;
using EasyManufacture.Domain.Options;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Services;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;

namespace EasyManufacture.Api.Controllers;

/// <summary>
/// 登录接口，路由与旧站 <c>EasyManufacture.Web.Controllers.LoginController</c> 一致。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
[ApiController]
[Route("Login/[action]")]
public partial class LoginController : ControllerBase
{
    private readonly IRequestBodyAccessor _body;
    private readonly ICurrentUser _currentUser;
    private readonly IAccountService _accountService;
    private readonly LicenceLoginService _licenceLogin;
    private readonly LoginSessionEnricher _sessionEnricher;
    private readonly JDRegister _jdRegister;
    private readonly AppSettings _appSettings;

    public LoginController(
        IRequestBodyAccessor body,
        ICurrentUser currentUser,
        IAccountService accountService,
        LicenceLoginService licenceLogin,
        LoginSessionEnricher sessionEnricher,
        JDRegister jdRegister,
        IOptions<AppSettings> appSettings)
    {
        _body = body;
        _currentUser = currentUser;
        _accountService = accountService;
        _licenceLogin = licenceLogin;
        _sessionEnricher = sessionEnricher;
        _jdRegister = jdRegister;
        _appSettings = appSettings.Value;
    }

    /// <summary>
    /// 用户登录 / 刷新（对齐旧 <c>CheckAccount</c>：先 LoginCheck，再 XBLoginCheck 清 Session，有 dev_Account 则成功）。
    /// </summary>
    [HttpPost]
    [HttpGet]
    public async Task<string> CheckAccount([FromQuery] bool isSSO = false, CancellationToken cancellationToken = default)
    {
        var msg = "成功";
        var result = true;
        string? token = null;

        try
        {
            V_Dev_Account? devAccount = null;

            if (!isSSO)
            {
                // 显式账号密码登录：先清掉上一用户的 Session/Cookie，避免仍显示 A
                if (IsExplicitLoginAttempt())
                    ClearLoginState();

                var (ok, loginMsg, loginAccount) = await _licenceLogin.LoginCheckAsync(
                    HttpContext, _body.BodyJson, cancellationToken: cancellationToken);
                result = ok;
                msg = loginMsg;
                if (loginAccount != null)
                    devAccount = loginAccount;
            }
            else
            {
                devAccount = LegacyAccountResolver.GetCurrentLegacyAccount(_currentUser);
            }

            if (devAccount == null && !IsExplicitLoginAttempt())
                devAccount = LegacyAccountResolver.GetCurrentLegacyAccount(_currentUser);

            if (devAccount != null)
                token = DesCrypto.Encrypt(devAccount.Account);

            // 旧站更新：LoginCheck 失败且未启用 XBLoginCheck 时清空 Session
            if (!result && !AppInfo.XBLoginCheck)
            {
                ClearLoginState();
                devAccount = null;
            }

            if (devAccount == null)
            {
                devAccount = await TryRestoreAfterFailedLoginCheckAsync(cancellationToken);
                if (devAccount != null)
                {
                    result = true;
                    msg = "成功";
                    token = DesCrypto.Encrypt(devAccount.Account);
                }
            }

            if (devAccount == null)
                return SerializeFail(string.IsNullOrWhiteSpace(msg) ? "账号或密码错误" : msg, token);

            return await BuildCheckAccountSuccessJsonAsync(devAccount, cancellationToken);
        }
        catch (Exception ex)
        {
            return SerializeFail(ex.Message, token);
        }
    }

    /// <summary>LoginCheck 失败后：跨域刷新（Pwd 为空 / token）。</summary>
    private async Task<V_Dev_Account?> TryRestoreAfterFailedLoginCheckAsync(CancellationToken cancellationToken)
    {
        // 用户提交了密码但登录失败，不要用旧 token 恢复上一用户
        if (IsExplicitLoginAttempt())
            return null;

        var appCode = LegacyAccountResolver.ResolveAppCode(_appSettings);

        var legacy = await LegacyAccountResolver.TryRestoreWhenPasswordMissingAsync(
            HttpContext, _body.BodyJson, appCode, _accountService, cancellationToken);
        if (legacy != null)
            return legacy;

        return await LegacyAccountResolver.TryResolveFromCredentialsAsync(
            HttpContext, _body.BodyJson, appCode, _accountService, cancellationToken);
    }

    private void ClearLoginState() =>
        LoginStateHelper.ClearLoginState(HttpContext, _currentUser, LoginStateHelper.ResolveAppCode(_appSettings));

    private bool IsExplicitLoginAttempt()
    {
        if (!LoginFormHelper.TryReadLoginForm(HttpContext.Request, _body.BodyJson, out _, out var pwd))
            return false;
        return !LoginFormHelper.IsMissingPassword(pwd);
    }

    private async Task<string> BuildCheckAccountSuccessJsonAsync(
        V_Dev_Account devAccount,
        CancellationToken cancellationToken)
    {
        var token = DesCrypto.Encrypt(devAccount.Account);
        AuthCookieHelper.AppendLoginCookies(Response, Request, _appSettings.AppCode, token);

        var enriched = await _sessionEnricher.EnrichAsync(devAccount, cancellationToken);
        devAccount = enriched.Account;

        if (!string.IsNullOrEmpty(AppInfo.ExternalNetwork))
        {
            var referer = Request.Headers.Referer.ToString();
            if (referer.Contains(AppInfo.ExternalNetwork, StringComparison.OrdinalIgnoreCase)
                && devAccount.Extend3 != "是")
            {
                return SerializeFail("该账号没有访问外网的权限", token);
            }
        }

        _currentUser.SetAccount(MapToDevAccount(devAccount));
        V_Dev_Account.SetDev_Account(HttpContext, devAccount);
        HttpContext.Session.SetString(AuthTokenResolver.SessionAccountKey, devAccount.Account);

        return JsonConvert.SerializeObject(new
        {
            msg = "成功",
            result = true,
            Account = devAccount.Account,
            Name = devAccount.Name,
            token,
            dev_Account = devAccount,
            ValidityDays = _jdRegister.ValidityDays,
            ValidityDate = _jdRegister.ValidityDate,
            IsChangePwd = enriched.IsChangePwd,
            IsUpLoadAvatar = enriched.IsUpLoadAvatar,
            AvatarURL = enriched.AvatarUrl
        });
    }

    private static string SerializeFail(string msg, string? token) =>
        JsonConvert.SerializeObject(new { msg, result = false, token });

    private static DevAccount MapToDevAccount(V_Dev_Account a) => new()
    {
        Account = a.Account,
        Name = a.Name,
        OrganizeID = a.OrganizeID,
        CenterID = a.CenterID,
        GroupID = a.GroupID,
        WorkFlowInstanceID = a.WorkFlowInstanceID ?? a.Extend1,
        Extend1 = a.Extend1,
        Extend2 = a.Extend2,
        Extend3 = a.Extend3,
        Status = a.Status ?? 1
    };
}
