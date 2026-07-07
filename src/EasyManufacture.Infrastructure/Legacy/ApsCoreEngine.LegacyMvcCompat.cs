using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>旧 MVC BaseController / JsonResult 兼容（供 ApsCoreEngine.LegacyApi 使用）。</summary>
public partial class ApsCoreEngine
{
    /// <summary>旧 APSAPIController.APSData 末尾 <c>return base.APSData()</c> 入口。</summary>
    public string RunApsDataLegacy() => RunAPSData();

    protected ApsLegacyHttpContext? HttpContext =>
        LegacyHttpContext == null ? null : new ApsLegacyHttpContext(LegacyHttpContext);

    protected bool IsPostBack =>
        string.Equals(LegacyHttpContext?.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);

    protected IActionResult View(string viewName)
    {
        // 导出等已通过 OutputClient/BinaryWrite 写响应；旧 Web 的 return View("Index") 在 Api 宿主无需 Razor
        if (LegacyHttpContext?.Response.HasStarted == true)
            return new EmptyResult();
        return new ViewResult { ViewName = viewName };
    }

    protected ApsLegacyJsonResult FormResult(bool result, string msg, object? data) =>
        new() { Data = new { result, msg, data } };

    protected ApsLegacyJsonResult FormResult(object obj) => new() { Data = obj };

    protected IActionResult Content(string content, string contentType = "application/json; charset=utf-8") =>
        new ContentResult { Content = content, ContentType = contentType };
}

/// <summary>兼容 System.Web.Mvc.JsonResult（勿与 Microsoft.AspNetCore.Mvc.JsonResult 混名）。</summary>
public class ApsLegacyJsonResult : IActionResult
{
    public object? Data { get; set; }
    public int MaxJsonLength { get; set; } = int.MaxValue;
    public JsonRequestBehavior JsonRequestBehavior { get; set; }
    public string ToJson() => JsonConvert.SerializeObject(Data);

    public Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json; charset=utf-8";
        return response.WriteAsync(ToJson());
    }
}

public enum JsonRequestBehavior
{
    AllowGet
}
