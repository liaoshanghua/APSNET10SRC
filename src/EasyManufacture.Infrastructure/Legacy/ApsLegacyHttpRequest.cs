using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Infrastructure.Legacy;

public sealed class ApsLegacyHttpRequest
{
    private readonly HttpContext? _context;

    public ApsLegacyHttpRequest(HttpContext? context) => _context = context;

    public IFormFileCollection Files =>
        _context?.Request is { HasFormContentType: true } req ? req.Form.Files : new FormFileCollection();

    public IHeaderDictionary Headers => _context?.Request.Headers ?? new HeaderDictionary();

    public string this[string key]
    {
        get
        {
            var request = _context?.Request;
            if (request == null) return "";
            if (request.Query.TryGetValue(key, out var queryValue))
                return queryValue.ToString();
            if (request.HasFormContentType && request.Form.TryGetValue(key, out var formValue))
                return formValue.ToString();
            return "";
        }
    }

    public string? UserAgent =>
        _context?.Request.Headers.UserAgent.ToString();

    public string UserHostAddress =>
        _context?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
}
