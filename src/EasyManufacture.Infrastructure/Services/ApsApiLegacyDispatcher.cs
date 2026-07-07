using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Legacy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// 反射调用 <see cref="ApsCoreEngine"/> 上与旧 APSAPIController/APSCore 同名的 public 方法。
/// 找不到方法或执行失败且 <c>LegacyWeb:ForwardAllApsApi=true</c> 时转发旧 EasyManufacture.Web。
/// </summary>
public sealed class ApsApiLegacyDispatcher
{
    private readonly ApsCoreEngine _engine;
    private readonly LegacyApsApiForwarder _forwarder;
    private readonly IRequestBodyAccessor _body;
    private readonly ILogger<ApsApiLegacyDispatcher> _logger;

    public ApsApiLegacyDispatcher(
        ApsCoreEngine engine,
        LegacyApsApiForwarder forwarder,
        IRequestBodyAccessor body,
        ILogger<ApsApiLegacyDispatcher> logger)
    {
        _engine = engine;
        _forwarder = forwarder;
        _body = body;
        _logger = logger;
    }

    public Task<string?> TryInvokeAsync(string action, HttpContext httpContext, CancellationToken ct) =>
        TryInvokeAsync(action, httpContext, Array.Empty<object?>(), ct);

    public async Task<string?> TryInvokeAsync(string action, HttpContext httpContext, object?[] args, CancellationToken ct)
    {
        _engine.BodyJson = _body.BodyJson;

        var candidates = typeof(ApsCoreEngine).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.Equals(action, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var method = candidates.FirstOrDefault(m => m.GetParameters().Length == args.Length)
                     ?? candidates.FirstOrDefault(m => m.GetParameters().Length == 0);

        if (method == null)
            return await ForwardOrNotFoundAsync(action, httpContext, ct).ConfigureAwait(false);

        var invokeArgs = BuildInvokeArgs(method, args, httpContext);

        try
        {
            var raw = method.Invoke(_engine, invokeArgs);
            var text = await CoerceToJsonStringAsync(raw, ct).ConfigureAwait(false);
            if (text != null)
                return text;
            return await ForwardOrNotFoundAsync(action, httpContext, ct).ConfigureAwait(false);
        }
        catch (TargetInvocationException ex)
        {
            _logger.LogError(ex.InnerException ?? ex, "APSAPI/{Action} 本地执行失败", action);
            if (_forwarder.CanForwardAll)
                return await _forwarder.ForwardActionAsync(action, _body.BodyJson, httpContext, ct)
                    .ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<string?> CoerceToJsonStringAsync(object? raw, CancellationToken ct)
    {
        if (raw == null)
            return null;

        if (raw is string s)
            return s;

        if (raw is ApsLegacyJsonResult jr)
            return jr.ToJson();

        if (raw is ContentResult cr)
            return cr.Content;

        if (raw is Task task)
        {
            await task.ConfigureAwait(false);
            if (task.GetType().IsGenericType)
            {
                var resultProp = task.GetType().GetProperty("Result");
                if (resultProp != null)
                    return await CoerceToJsonStringAsync(resultProp.GetValue(task), ct).ConfigureAwait(false);
            }
            return null;
        }

        if (raw is IActionResult)
            return null;

        return raw.ToString();
    }

    private static object?[] BuildInvokeArgs(MethodInfo method, object?[] args, HttpContext httpContext)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return Array.Empty<object?>();

        var result = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            object? value = i < args.Length ? args[i] : null;
            if (value == null && p.HasDefaultValue)
                value = p.DefaultValue;

            if (value == null && p.ParameterType == typeof(string))
            {
                value = httpContext.Request.Query[p.Name!].FirstOrDefault()
                        ?? httpContext.Request.Form[p.Name!].FirstOrDefault();
            }

            if (value != null && p.ParameterType != typeof(string))
            {
                try
                {
                    value = Convert.ChangeType(value, Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType);
                }
                catch
                {
                    // 保持原值，由被调方法处理
                }
            }

            result[i] = value;
        }

        return result;
    }

    private async Task<string> ForwardOrNotFoundAsync(string action, HttpContext httpContext, CancellationToken ct)
    {
        if (_forwarder.CanForwardAll)
            return await _forwarder.ForwardActionAsync(action, _body.BodyJson, httpContext, ct)
                .ConfigureAwait(false);

        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            result = false,
            msg = $"接口 APSAPI/{action} 尚未在 Net10 启用；请配置 LegacyWeb:BaseUrl 与 ForwardAllApsApi=true 转发旧站，或检查 ApsCoreEngine.LegacyApi 是否已编译。"
        });
    }
}
