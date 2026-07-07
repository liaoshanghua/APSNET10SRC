using EasyManufacture.Application.Abstractions;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// 登录校验外壳，移植自 <c>EasyManufacture.Lisence.BaseControl.LoginCheck</c>。
/// 账号密码验证委托 <see cref="IAccountService.CheckAccountAsync"/>（旧 <c>CheckDev_Account</c>）。
/// </summary>
public sealed class LicenceLoginService
{
    private readonly IAccountService _accountService;
    private readonly SystemLog _systemLog = new();

    public LicenceLoginService(IAccountService accountService) => _accountService = accountService;

    /// <summary>
    /// 解析请求中的账号密码，执行 IP 限流后查库，返回旧版 <c>V_Dev_Account</c> 供 Session 等价物使用。
    /// </summary>
    /// <param name="httpContext">当前请求（Form 或 JSON Body）。</param>
    /// <param name="bodyJson">已由 <c>RequestBodyMiddleware</c> 读取的 JSON。</param>
    /// <param name="isAutoLogin">SSO/免密等场景。</param>
    public async Task<(bool result, string msg, V_Dev_Account? account)> LoginCheckAsync(
        HttpContext httpContext,
        string bodyJson,
        bool isAutoLogin = false,
        CancellationToken cancellationToken = default)
    {
        var msg = "成功";
        V_Dev_Account? account = null;

        if (!isAutoLogin && httpContext.Request.Form["isAutoLogin"] == "true")
            isAutoLogin = true;

        // 与旧站一致：Form 优先，否则 JSON 的 Account/Pwd
        string accountName;
        string pwd;
        if (!LoginFormHelper.TryReadLoginForm(httpContext.Request, bodyJson, out accountName, out pwd))
        {
            accountName = string.Empty;
            pwd = string.Empty;
        }

        // 旧逻辑：账号不以 . 结尾则不允许自动登录
        if (!accountName.EndsWith('.'))
            isAutoLogin = false;
        accountName = accountName.Trim().TrimEnd('.');

        if (string.IsNullOrEmpty(accountName))
            return (false, "账号不能为空", null);
        if (LoginFormHelper.IsMissingPassword(pwd))
            return (false, "密码不能为空", null);

        // --- 以下与 BaseControl.LoginCheck 中 AccountLoginInfos / LockIP 一致 ---
        var ip = LicenceRuntime.ClientIpAddress;
        if (AppInfo.AccountLoginInfos.All(m => m.Account != accountName || m.IPAddress != ip))
            AppInfo.AccountLoginInfos.Add(new AccountLoginInfo
            {
                Account = accountName,
                IPAddress = ip,
                LastTime = DateTime.Now
            });

        var loginInfo = AppInfo.AccountLoginInfos.First(m => m.Account == accountName && m.IPAddress == ip);
        if ((DateTime.Now - loginInfo.LastTime).TotalSeconds >= 600)
        {
            loginInfo.LastTime = DateTime.Now;
            loginInfo.ErrorCount = 0;
        }

        loginInfo.LoginCount += 1;
        if (loginInfo.ErrorCount > 3 && (DateTime.Now - loginInfo.LastTime).Seconds <= 1)
            return (false, "一秒内访问超过3次，本次拒绝访问！", null);

        if (loginInfo.ErrorCount > 10)
        {
            loginInfo.IsLock = true;
            if (AppInfo.LockedIps.All(m => m.IPAddress != ip))
                AppInfo.LockedIps.Add(new LockedIpEntry { IPAddress = ip, IsLock = true });
            var minutesLeft = 10 - (DateTime.Now - loginInfo.LastTime).TotalMinutes;
            return (false, $"您已经累计输入密码错误10次，10分钟后才可以访问！还有{minutesLeft:G2}分解锁", null);
        }

        var user = await _accountService.CheckAccountAsync(accountName, pwd, isAutoLogin, cancellationToken);
        loginInfo.LastTime = DateTime.Now;

        if (user == null)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.登录错误, "账号：" + accountName, null, null);
            loginInfo.ErrorCount += 1;
            return (false, "账号或密码错误", null);
        }

        account = new V_Dev_Account
        {
            Account = user.Account,
            Name = user.Name,
            OrganizeID = user.OrganizeID ?? 0,
            CenterID = user.CenterID ?? 0,
            GroupID = user.GroupID ?? 0,
            WorkFlowInstanceID = user.WorkFlowInstanceID ?? user.Extend1,
            Extend1 = user.Extend1,
            Extend2 = user.Extend2,
            Extend3 = user.Extend3,
            Status = user.Status
        };

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
        var source = userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
            ? "Android" : "PC";
        _systemLog.SaveLog(SystemLog.SystemLogType.登录成功, "电脑名：" + httpContext.Connection.RemoteIpAddress + "，来源：" + source, account, null);

        return (true, msg, account);
    }

    /// <summary>旧 <c>BaseControl.LoginCheckJson</c>：Body 为 JSON，字段 Account/Pwd。</summary>
    public async Task<(bool result, string msg, V_Dev_Account? account)> LoginCheckJsonAsync(
        HttpContext httpContext,
        string bodyJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bodyJson))
            return (false, "未接收到数据", null);

        JObject? jObject;
        try
        {
            jObject = JsonConvert.DeserializeObject<JObject>(bodyJson);
        }
        catch
        {
            return (false, "未接收到数据", null);
        }

        if (jObject == null)
            return (false, "未接收到数据", null);

        var accountName = jObject["Account"]?.ToString() ?? "";
        var pwd = jObject["Pwd"]?.ToString() ?? "";
        var isAutoLogin = false;
        if (!accountName.EndsWith('.'))
            isAutoLogin = false;
        accountName = accountName.Trim().TrimEnd('.');

        if (string.IsNullOrEmpty(accountName))
            return (false, "账号不能为空", null);
        if (LoginFormHelper.IsMissingPassword(pwd))
            return (false, "密码不能为空", null);

        var user = await _accountService.CheckAccountAsync(accountName, pwd, isAutoLogin, cancellationToken);
        if (user == null)
        {
            _systemLog.SaveLog(SystemLog.SystemLogType.登录错误, "账号：" + accountName, null, null);
            return (false, "账号或密码错误", null);
        }

        var legacy = new V_Dev_Account
        {
            Account = user.Account,
            Name = user.Name,
            OrganizeID = user.OrganizeID ?? 0,
            CenterID = user.CenterID ?? 0,
            GroupID = user.GroupID ?? 0,
            WorkFlowInstanceID = user.WorkFlowInstanceID ?? user.Extend1,
            Extend1 = user.Extend1,
            Extend2 = user.Extend2,
            Extend3 = user.Extend3,
            Status = user.Status,
            Pwd = user.Pwd
        };

        return (true, "成功", legacy);
    }
}
