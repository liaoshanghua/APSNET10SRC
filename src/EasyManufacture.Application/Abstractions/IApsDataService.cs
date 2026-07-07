namespace EasyManufacture.Application.Abstractions;

/// <summary>APS 数据查询（旧 <c>APSAPIController.APSData</c> / <c>APSCore.APSData</c>）。</summary>
public interface IApsDataService
{
    /// <summary>POST Body JSON，返回与旧站相同结构的 JSON 字符串。</summary>
    Task<string> ApsDataAsync(string bodyJson, CancellationToken cancellationToken = default);
}
