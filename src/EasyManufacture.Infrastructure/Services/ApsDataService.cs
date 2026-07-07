using EasyManufacture.Application.Abstractions;
using EasyManufacture.Infrastructure.Legacy;

namespace EasyManufacture.Infrastructure.Services;

/// <summary>
/// APSData 入口：可选转发旧 Web；本地走 <see cref="ApsCoreEngine.RunAPSData"/>。
/// EnableLegacyApsCoreSource=true 时 RunAPSData 调用 LegacyCore.APSData() 全量逻辑。
/// </summary>
public sealed class ApsDataService : IApsDataService
{
    private readonly ApsCoreEngine _engine;
    private readonly LegacyApsDataForwarder _forwarder;

    public ApsDataService(ApsCoreEngine engine, LegacyApsDataForwarder forwarder)
    {
        _engine = engine;
        _forwarder = forwarder;
    }

    /// <inheritdoc />
    public async Task<string> ApsDataAsync(string bodyJson, CancellationToken cancellationToken = default)
    {
        // LegacyWeb:ForwardApsData=true 时 POST 到旧 Web /APSAPI/APSData，保证与 3700+ 行旧逻辑一致
        if (_forwarder.ShouldForward)
            return await _forwarder.ForwardAsync(bodyJson, cancellationToken).ConfigureAwait(false);

        _engine.BodyJson = bodyJson;
        return _engine.RunAPSData();
    }
}
