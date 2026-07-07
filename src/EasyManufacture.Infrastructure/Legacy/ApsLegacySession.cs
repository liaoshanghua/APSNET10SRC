using EasyManufacture.Entitys;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容旧 System.Web.HttpSessionState（CheckLogin 登出）。</summary>
public sealed class ApsLegacySession
{
    private readonly HttpContext? _context;

    public ApsLegacySession(HttpContext? context) => _context = context;

    public void Clear()
    {
        _context?.Session?.Clear();
        if (_context != null)
            V_Dev_Account.SetDev_Account(_context, null);
    }
}

/// <summary>兼容旧 System.Web.HttpResponse（Cookies.Clear）。</summary>
public sealed class ApsLegacyHttpResponse
{
    private readonly HttpContext? _context;

    public ApsLegacyHttpResponse(HttpContext? context)
    {
        _context = context;
        Cookies = new ApsLegacyCookieCollection(context);
        Headers = new ApsLegacyResponseHeaders(context);
    }

    public ApsLegacyCookieCollection Cookies { get; }
    public ApsLegacyResponseHeaders Headers { get; }
}

public sealed class ApsLegacyResponseHeaders
{
    private readonly HttpContext? _context;

    public ApsLegacyResponseHeaders(HttpContext? context) => _context = context;

    public void Add(string name, string value)
    {
        if (_context != null)
            _context.Response.Headers[name] = value;
    }

    public void Clear() => _context?.Response.Headers.Clear();
}

public sealed class ApsLegacyCookieCollection
{
    private readonly HttpContext? _context;

    public ApsLegacyCookieCollection(HttpContext? context) => _context = context;

    public void Clear()
    {
        if (_context == null)
            return;

        foreach (var key in _context.Request.Cookies.Keys)
            _context.Response.Cookies.Delete(key);
    }
}
