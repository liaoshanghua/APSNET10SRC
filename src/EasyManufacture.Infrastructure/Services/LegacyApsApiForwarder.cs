using EasyManufacture.Domain.Options;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// 将未在 Net10 本地实现的 /APSAPI/{action} 转发至旧版 EasyManufacture.Web（并行部署时使用）。
/// </summary>
public sealed class LegacyApsApiForwarder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LegacyWebOptions _options;
    private readonly ILogger<LegacyApsApiForwarder> _logger;

    public LegacyApsApiForwarder(
        IHttpClientFactory httpClientFactory,
        IOptions<LegacyWebOptions> options,
        ILogger<LegacyApsApiForwarder> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool CanForward => !string.IsNullOrWhiteSpace(_options.BaseUrl);

    public bool CanForwardAll => CanForward && _options.ForwardAllApsApi;

    public async Task<string> ForwardActionAsync(
        string action,
        string? bodyJson,
        HttpContext? httpContext,
        CancellationToken cancellationToken)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var client = _httpClientFactory.CreateClient(nameof(LegacyApsApiForwarder));
        var method = httpContext?.Request.Method ?? HttpMethods.Post;

        using var request = new HttpRequestMessage(
            method.Equals("GET", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Get : HttpMethod.Post,
            $"{baseUrl}/APSAPI/{action}");

        if (!string.IsNullOrEmpty(bodyJson) &&
            request.Method != HttpMethod.Get &&
            request.Method != HttpMethod.Head)
        {
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
        }

        CopyRequestHeaders(httpContext, request);

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("转发 APSAPI/{Action} 失败 HTTP {Code}", action, (int)response.StatusCode);
            return JsonNewtonsoft(text, action, (int)response.StatusCode);
        }

        return text;
    }

    private void CopyRequestHeaders(HttpContext? ctx, HttpRequestMessage request)
    {
        if (ctx == null) return;

        foreach (var headerName in new[] { "token", "Token", "X-Token" })
        {
            if (ctx.Request.Headers.TryGetValue(headerName, out var token))
                request.Headers.TryAddWithoutValidation(headerName, token.ToString());
        }

        if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie.ToString());

        if (ctx.Request.Headers.TryGetValue("User-Agent", out var ua))
            request.Headers.TryAddWithoutValidation("User-Agent", ua.ToString());

        foreach (var q in ctx.Request.Query)
            request.Headers.TryAddWithoutValidation(q.Key, q.Value.ToString());
    }

    private static string JsonNewtonsoft(string body, string action, int code) =>
        Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            result = false,
            msg = $"旧站 APSAPI/{action} 返回 HTTP {code}",
            detail = body.Length > 500 ? body[..500] : body
        });
}
