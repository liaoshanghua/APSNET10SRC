using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace EasyManufacture.Api.Controllers;

/// <summary>
/// 机器授权（/APSAPI/GetRegister、/APSAPI/SeRegister）。
/// 独立 Controller，不依赖 APSAPIController 构造时的数据库初始化。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
[ApiController]
[Route("APSAPI")]
public sealed class RegisterController : ControllerBase
{
    private readonly JDRegister _register;

    public RegisterController(JDRegister register) => _register = register;

    [HttpGet("GetRegister")]
    public IActionResult GetRegister()
    {
        try
        {
            return Content(_register.ToStatusJson(), "application/json; charset=utf-8");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { result = false, msg = ex.Message });
        }
    }

    [HttpGet("SeRegister")]
    [HttpPost("SeRegister")]
    public IActionResult SeRegister([FromQuery] string? pwd = null, [FromQuery] string? ssn = null)
    {
        try
        {
            pwd ??= Request.GetRequestValue("pwd") ?? string.Empty;
            ssn ??= Request.GetRequestValue("ssn") ?? string.Empty;

            _register.Registration(pwd, ssn);
            return Content(_register.ToStatusJson(), "application/json; charset=utf-8");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { result = false, msg = ex.Message });
        }
    }
}
