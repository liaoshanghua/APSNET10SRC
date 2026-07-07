using System.Text;
using Microsoft.AspNetCore.Http;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>兼容旧 System.Web.HttpContext（Response 桩）。</summary>
public sealed class ApsLegacyHttpContext
{
    private readonly HttpContext? _context;

    public ApsLegacyHttpContext(HttpContext? context) => _context = context;

    public ApsLegacyHttpResponseLegacy Response => new(_context);
}

/// <summary>兼容旧 System.Web.HttpResponse 写文件/下载 API。</summary>
public sealed class ApsLegacyHttpResponseLegacy
{
    private readonly HttpContext? _context;

    public ApsLegacyHttpResponseLegacy(HttpContext? context) => _context = context;

    public bool Buffer { get; set; }

    public string ContentType
    {
        get => _context?.Response.ContentType ?? "";
        set
        {
            if (_context != null)
                _context.Response.ContentType = value;
        }
    }

    public string Charset { get; set; } = "utf-8";

    public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

    public void Clear() => _context?.Response.Clear();

    public void ClearHeaders() => _context?.Response.Headers.Clear();

    public void ClearContent()
    {
        // 旧 System.Web 可清空缓冲；Kestrel 响应流通常不可 Seek/SetLength，写入前 body 已为空即可
        if (_context?.Response.Body is { CanSeek: true } stream)
        {
            stream.SetLength(0);
            stream.Position = 0;
        }
    }

    public void AddHeader(string name, string value)
    {
        if (_context != null)
            _context.Response.Headers[name] = value;
    }

    public void BinaryWrite(byte[] bytes)
    {
        if (_context == null || bytes.Length == 0)
            return;
        _context.Response.Body.WriteAsync(bytes.AsMemory()).GetAwaiter().GetResult();
    }

    public void Flush() =>
        _context?.Response.Body.FlushAsync().GetAwaiter().GetResult();

    public void Close() { }

    public int StatusCode
    {
        get => _context?.Response.StatusCode ?? 200;
        set
        {
            if (_context != null)
                _context.Response.StatusCode = value;
        }
    }
}
