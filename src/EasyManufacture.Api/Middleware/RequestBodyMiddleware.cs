using System.Text;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Legacy;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace EasyManufacture.Api.Middleware;

/// <summary>
/// 将 POST/PUT/PATCH 请求体读入 <see cref="IRequestBodyAccessor.BodyJson"/>，
/// 对应旧 <c>BaseController.Initialize</c> 中 <c>Request.InputStream</c> → <c>BodyJson</c>。
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public sealed class RequestBodyMiddleware
{
    private readonly RequestDelegate _next;

    public RequestBodyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IRequestBodyAccessor bodyAccessor)
    {
        if (IsBodyMethod(context.Request.Method) && context.Request.ContentLength != 0)
        {
            bodyAccessor.BodyJson = await TryReadBodyAsync(context.Request) ?? string.Empty;
            ApsLegacyHttpRequestHelper.SetBodyJson(context, bodyAccessor.BodyJson);
        }

        await _next(context);
    }

    private static bool IsBodyMethod(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    private static async Task<string?> TryReadBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();

            if (request.Body.CanSeek)
                request.Body.Position = 0;

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            if (request.Body.CanSeek)
                request.Body.Position = 0;
            return body;
        }
        catch (BadHttpRequestException)
        {
            if (request.Body.CanSeek)
                request.Body.Position = 0;
            return string.Empty;
        }
        catch (IOException)
        {
            if (request.Body.CanSeek)
                request.Body.Position = 0;
            return string.Empty;
        }
    }
}
