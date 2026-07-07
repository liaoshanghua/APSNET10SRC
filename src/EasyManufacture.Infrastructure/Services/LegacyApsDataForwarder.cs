using EasyManufacture.Application.Abstractions;
using EasyManufacture.Domain.Options;
using EasyManufacture.Licence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>将 APSData 转发至旧版 Web（完整 3700+ 行逻辑未迁移时的并行方案）。</summary>
public sealed class LegacyApsDataForwarder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LegacyWebOptions _options;
    private readonly ILogger<LegacyApsDataForwarder> _logger;

    public LegacyApsDataForwarder(
        IHttpClientFactory httpClientFactory,
        IOptions<LegacyWebOptions> options,
        ILogger<LegacyApsDataForwarder> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool ShouldForward => _options.ForwardApsData && !string.IsNullOrWhiteSpace(_options.BaseUrl);

    public async Task<string> ForwardAsync(string bodyJson, CancellationToken cancellationToken)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var client = _httpClientFactory.CreateClient(nameof(LegacyApsDataForwarder));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/APSAPI/APSData")
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        var ctx = LicenceRuntime.Http.HttpContext;
        if (ctx != null)
        {
            foreach (var headerName in new[] { "token", "Token", "X-Token" })
            {
                if (ctx.Request.Headers.TryGetValue(headerName, out var token))
                    request.Headers.TryAddWithoutValidation(headerName, token.ToString());
            }
            if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie.ToString());
            if (ctx.Request.Headers.TryGetValue("User-Agent", out var ua))
                request.Headers.UserAgent.ParseAdd(ua.ToString());
        }

        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("转发 APSData 失败 HTTP {Code}: {Body}", (int)response.StatusCode, text);
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                result = false,
                msg = $"旧站 APSData 返回 {(int)response.StatusCode}"
            });
        }

        return text;
    }
}
