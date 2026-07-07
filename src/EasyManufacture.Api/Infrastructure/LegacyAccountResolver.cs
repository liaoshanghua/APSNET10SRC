using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Models;
using EasyManufacture.Domain.Options;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Services;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>
/// 从 Session / Items / token 凭证解析 <see cref="V_Dev_Account"/>，
/// 供 <c>CheckAccount</c> 刷新登录态与 <see cref="Middleware.AccountAuthenticationMiddleware"/> 共用逻辑。
/// </summary>
public static class LegacyAccountResolver
{
    public static V_Dev_Account? GetCurrentLegacyAccount(ICurrentUser currentUser) =>
        V_Dev_Account.GetDev_Account() ?? MapLegacyAccount(currentUser.Account);

    public static async Task<V_Dev_Account?> TryResolveFromCredentialsAsync(
        HttpContext context,
        string? bodyJson,
        string configuredAppCode,
        IAccountService accountService,
        CancellationToken cancellationToken = default)
    {
        foreach (var credential in AuthTokenResolver.CollectCredentials(context.Request, bodyJson, configuredAppCode))
        {
            var skipDecrypt = credential.Source == AuthTokenResolver.CredentialSource.QueryUserName;
            var accountName = AuthTokenResolver.ResolveAccountName(credential.Raw, skipDecrypt);
            if (string.IsNullOrEmpty(accountName))
                continue;

            var user = await accountService.GetByAccountAsync(accountName, cancellationToken);
            if (user == null)
            {
                user = await accountService.CheckAccountAsync(
                    accountName, "", isAutoLogin: true, cancellationToken: cancellationToken);
            }

            if (user != null)
                return MapLegacyAccount(user);
        }

        return null;
    }

    public static string ResolveAppCode(AppSettings? appSettings) =>
        string.IsNullOrWhiteSpace(appSettings?.AppCode) ? AppInfo.AppCode : appSettings.AppCode.Trim();

    /// <summary>
    /// 刷新时前端常只提交 Account、Pwd 为 null（依赖上次 CheckAccount 返回的 dev_Account.Pwd 缓存）。
    /// 等价旧站：LoginCheck 失败后仍可从 Session/token 恢复，或 CheckDev_Account 免密。
    /// </summary>
    public static async Task<V_Dev_Account?> TryRestoreWhenPasswordMissingAsync(
        HttpContext context,
        string? bodyJson,
        string configuredAppCode,
        IAccountService accountService,
        CancellationToken cancellationToken = default)
    {
        if (!LoginFormHelper.TryReadLoginForm(context.Request, bodyJson, out var accountName, out var pwd))
            return null;

        if (!LoginFormHelper.IsMissingPassword(pwd))
            return null;

        accountName = accountName.Trim().TrimEnd('.');

        var sessionAccount = context.Session.GetString(AuthTokenResolver.SessionAccountKey);
        if (!string.IsNullOrWhiteSpace(sessionAccount)
            && AccountEquals(sessionAccount, accountName))
        {
            var fromSession = await LoadAccountAsync(accountService, sessionAccount, cancellationToken);
            if (fromSession != null)
                return fromSession;
        }

        var fromToken = await TryResolveFromCredentialsAsync(
            context, bodyJson, configuredAppCode, accountService, cancellationToken);
        if (fromToken != null && AccountEquals(fromToken.Account, accountName))
            return fromToken;

        if (context.Request.Form["isAutoLogin"] == "true")
        {
            return await LoadAccountAsync(accountService, accountName, cancellationToken);
        }

        // 与 OnAuthorization 一致：仅账号 + 空密码时免密校验（刷新场景）
        return await LoadAccountAsync(accountService, accountName, cancellationToken);
    }

    private static async Task<V_Dev_Account?> LoadAccountAsync(
        IAccountService accountService,
        string accountName,
        CancellationToken cancellationToken)
    {
        var user = await accountService.GetByAccountAsync(accountName, cancellationToken);
        if (user == null)
        {
            user = await accountService.CheckAccountAsync(
                accountName, "", isAutoLogin: true, cancellationToken: cancellationToken);
        }

        return MapLegacyAccount(user);
    }

    private static bool AccountEquals(string a, string b) =>
        string.Equals(a.Trim().TrimEnd('.'), b.Trim().TrimEnd('.'), StringComparison.OrdinalIgnoreCase);

    private static V_Dev_Account? MapLegacyAccount(DevAccount? account)
    {
        if (account == null)
            return null;

        return new V_Dev_Account
        {
            Account = account.Account,
            Name = account.Name,
            OrganizeID = account.OrganizeID ?? 0,
            CenterID = account.CenterID ?? 0,
            GroupID = account.GroupID ?? 0,
            WorkFlowInstanceID = account.WorkFlowInstanceID ?? account.Extend1,
            Extend1 = account.Extend1,
            Extend2 = account.Extend2,
            Extend3 = account.Extend3,
            Status = account.Status
        };
    }
}
