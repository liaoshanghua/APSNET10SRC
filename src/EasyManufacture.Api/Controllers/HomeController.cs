using EasyManufacture.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;

namespace EasyManufacture.Api.Controllers;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[ApiController]
[Route("Home/[action]")]
public class HomeController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ICurrentUser _currentUser;

    public HomeController(IMenuService menuService, ICurrentUser currentUser)
    {
        _menuService = menuService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [HttpPost]
    public async Task<string> GetMenuVue(CancellationToken cancellationToken)
    {
        if (_currentUser.Account == null)
        {
            return JsonConvert.SerializeObject(new { result = false, msg = "未登录" });
        }

        return await _menuService.GetMenuVueAsync(_currentUser.Account.Account, cancellationToken);
    }
}
