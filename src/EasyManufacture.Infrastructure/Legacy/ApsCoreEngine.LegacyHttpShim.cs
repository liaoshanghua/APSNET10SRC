using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// HttpContext / Request / Response 兼容层（替代 System.Web.HttpContext）。
/// LegacyApi 中 Request.UserHostAddress、Form 等经此访问。
/// </summary>
public partial class ApsCoreEngine
{
    private IHttpContextAccessor? _httpContextAccessor;

    internal void BindHttpContext(IHttpContextAccessor accessor) => _httpContextAccessor = accessor;

    protected Microsoft.AspNetCore.Http.HttpContext? LegacyHttpContext => _httpContextAccessor?.HttpContext;

    protected HttpResponse? LegacyResponse => LegacyHttpContext?.Response;

    protected ApsLegacySession Session => new(LegacyHttpContext);

    protected ApsLegacyHttpResponse Response => new(LegacyHttpContext);

    protected ApsLegacyServer Server => new();

    protected ApsLegacyHttpRequest Request => new(LegacyHttpContext);

    protected IActionResult File(byte[] contents, string contentType) =>
        new FileContentResult(contents, contentType);

    protected IActionResult File(byte[] contents, string contentType, string fileDownloadName) =>
        new FileContentResult(contents, contentType) { FileDownloadName = fileDownloadName };

    protected IActionResult File(string physicalPath, string contentType, string? fileDownloadName = null)
    {
        if (fileDownloadName == null)
            return new PhysicalFileResult(physicalPath, contentType);
        return new PhysicalFileResult(physicalPath, contentType) { FileDownloadName = fileDownloadName };
    }

    protected IActionResult HttpNotFound(string message) =>
        new NotFoundObjectResult(message);
}
