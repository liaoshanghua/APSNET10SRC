using EasyManufacture.Application.Abstractions;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace EasyManufacture.Api.Controllers;

/// <summary>
/// 兼容 vue-element-admin 刷新流程（<c>GET /user/info</c>，code 20000 / 50008）。
/// 同时支持旧项目直接调 <c>/Login/GetSession</c>。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IAccountService _accountService;
    private readonly LoginSessionEnricher _sessionEnricher;

    public UserController(
        ICurrentUser currentUser,
        IAccountService accountService,
        LoginSessionEnricher sessionEnricher)
    {
        _currentUser = currentUser;
        _accountService = accountService;
        _sessionEnricher = sessionEnricher;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetInfo(CancellationToken cancellationToken)
    {
        if (_currentUser.Account == null)
        {
            return Ok(new { code = 50008, message = "未登录" });
        }

        var legacy = V_Dev_Account.GetDev_Account();
        if (legacy == null)
        {
            legacy = new V_Dev_Account
            {
                Account = _currentUser.Account.Account,
                Name = _currentUser.Account.Name,
                OrganizeID = _currentUser.Account.OrganizeID ?? 0,
                CenterID = _currentUser.Account.CenterID ?? 0,
                GroupID = _currentUser.Account.GroupID ?? 0
            };
            var enriched = await _sessionEnricher.EnrichAsync(legacy, cancellationToken);
            legacy = enriched.Account;
            V_Dev_Account.SetDev_Account(HttpContext, legacy);
        }

        var roleMaps = await _accountService.GetRoleMapsAsync(legacy.Account, cancellationToken);
        var roles = roleMaps
            .Select(r => r.RoleID)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roles.Length == 0)
            roles = ["default"];

        return Ok(new
        {
            code = 20000,
            data = new
            {
                roles,
                name = legacy.Name ?? legacy.Account,
                avatar = string.Empty,
                introduction = legacy.Name ?? legacy.Account
            }
        });
    }
}
