using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Application.Security;
using EasyManufacture.Domain.Options;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EasyManufacture.Api.Middleware;

/// <summary>
/// 每个请求解析登录态，对齐旧 <c>BaseController.OnAuthorization</c>：
/// Session → Cookie(AppCode) → Form/Body CurrentAccount → Query → Header token → 刷新 Cookie。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public sealed class AccountAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public AccountAuthenticationMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUser currentUser,
        IAccountService accountService,
        IRequestBodyAccessor bodyAccessor)
    {
        var appCode = string.IsNullOrWhiteSpace(_appSettings.AppCode)
            ? AppInfo.AppCode
            : _appSettings.AppCode;

        V_Dev_Account? legacyAccount = null;

        // 1. 与旧站一致：优先 Session（对应 HttpContext.Session["Dev_Account"]）
        var sessionAccount = context.Session.GetString(AuthTokenResolver.SessionAccountKey);
        if (!string.IsNullOrWhiteSpace(sessionAccount))
        {
            legacyAccount = await LoadLegacyAccountAsync(accountService, sessionAccount);
        }

        // 2. 本请求 Items 已写入（同请求内重复进入）
        legacyAccount ??= V_Dev_Account.GetDev_Account();

        // 3. 按 OnAuthorization / token 凭证解析（与 CheckAccount 刷新逻辑共用）
        legacyAccount ??= await LegacyAccountResolver.TryResolveFromCredentialsAsync(
            context, bodyAccessor.BodyJson, appCode, accountService);

        if (legacyAccount != null)
        {
            legacyAccount.LastVisitTime = DateTime.Now;
            ApplyExtendFields(legacyAccount);
            LoadRoleMap(legacyAccount);

            var domainAccount = MapToDevAccount(legacyAccount);
            currentUser.SetAccount(domainAccount);
            V_Dev_Account.SetDev_Account(context, legacyAccount);

            context.Session.SetString(AuthTokenResolver.SessionAccountKey, legacyAccount.Account);

            var encrypted = DesCrypto.Encrypt(legacyAccount.Account);
            AuthCookieHelper.AppendLoginCookies(context.Response, context.Request, appCode, encrypted);
        }
        else
        {
            V_Dev_Account.SetDev_Account(context, null);
            // 请求里带了 token 但校验失败时，保留 Session，避免 DB 瞬时失败误清登录态
            if (!AuthTokenResolver.HasAnyCredential(context.Request, bodyAccessor.BodyJson, appCode))
                context.Session.Remove(AuthTokenResolver.SessionAccountKey);
        }

        await _next(context);
    }

    /// <summary>等价旧 <c>CheckDev_Account(account, "", AppCode, true)</c>。</summary>
    private static async Task<V_Dev_Account?> LoadLegacyAccountAsync(
        IAccountService accountService,
        string accountName,
        bool isAutoLogin = true)
    {
        var user = await accountService.GetByAccountAsync(accountName);
        if (user == null && isAutoLogin)
        {
            user = await accountService.CheckAccountAsync(accountName, "", isAutoLogin: true);
        }

        return user == null ? null : MapLegacyAccount(user);
    }

    private static V_Dev_Account MapLegacyAccount(Domain.Models.DevAccount account) => new()
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

    private static Domain.Models.DevAccount MapToDevAccount(V_Dev_Account a) => new()
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

    /// <summary>旧 OnAuthorization 内 Dev_Account 扩展字段查询。</summary>
    private static void ApplyExtendFields(V_Dev_Account account)
    {
        try
        {
            var dt = SqlHelper.ExecuteDataTable(
                $"SELECT Extend1, Extend2, Extend3 FROM Dev_Account(NOLOCK) WHERE Status = 1 AND Account = '{account.Account.Replace("'", "''")}'");
            if (dt.Rows.Count == 0)
                return;

            account.Extend1 = dt.Rows[0]["Extend1"]?.ToString();
            account.Extend2 = dt.Rows[0]["Extend2"]?.ToString();
            account.Extend3 = dt.Rows[0]["Extend3"]?.ToString();
        }
        catch
        {
            /* ignore */
        }
    }

    private static void LoadRoleMap(V_Dev_Account account) =>
        account.RoleMap = VDevAccountRoleMapLoader.Load(account.Account);
}

