using EasyManufacture.Api.Infrastructure;
using EasyManufacture.Application.Abstractions;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Infrastructure.Services;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace EasyManufacture.Api.Controllers;

/// <summary>
/// APS 业务 API（对齐旧 <c>APSAPIController : APSCore</c>）。
/// <para>业务方法在 <c>Legacy/APSAPIController.LegacyBusiness.cs</c>（partial）。</para>
/// <para>APSCore 基类：<see cref="ApsCoreEngine"/>（Infrastructure / LegacyCore.cs）。</para>
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
[ApiController]
[Route("APSAPI/[action]")]
public partial class APSAPIController : ApsCoreEngine
{
    private readonly IConfigService _configService;
    private readonly ISaveDataService _saveDataService;
    private readonly LegacyApsDataForwarder _apsDataForwarder;

    public APSAPIController(
        IRequestBodyAccessor body,
        ICurrentUser currentUser,
        ManufactureDbContext dbContext,
        JDRegister jdRegister,
        IHttpContextAccessor httpContextAccessor,
        IConfigService configService,
        ISaveDataService saveDataService,
        LegacyApsDataForwarder apsDataForwarder)
        : base(body, currentUser, dbContext, jdRegister, httpContextAccessor)
    {
        _configService = configService;
        _saveDataService = saveDataService;
        _apsDataForwarder = apsDataForwarder;
    }

    [HttpPost]
    public Task<string> GetConfig(CancellationToken cancellationToken) =>
        _configService.GetConfigAsync(BodyJson, cancellationToken);

    [HttpPost]
    public Task<string> ConfigTable(CancellationToken cancellationToken) =>
        _configService.ConfigTableAsync(BodyJson, cancellationToken);

    [HttpPost]
    public Task<string> SaveData(CancellationToken cancellationToken) =>
        _saveDataService.SaveDataAsync(BodyJson, cancellationToken);

    /// <summary>浏览器直接打开 /APSAPI/APSData 时返回调用说明（业务须 POST）。</summary>
    [HttpGet]
    [ActionName("APSData")]
    public IActionResult APSDataGet() => new OkObjectResult(new
    {
        result = true,
        msg = "请使用 POST 调用本接口",
        path = "/APSAPI/APSData"
    });

    [HttpPost]
    public override string APSData()
    {
        if (_apsDataForwarder.ShouldForward)
            return _apsDataForwarder.ForwardAsync(BodyJson, CancellationToken.None).GetAwaiter().GetResult();
        return RunLegacyApsDataWithDicHooks();
    }

    /// <summary>服务端 Excel 导出（须走 dic 钩子 + isDownload，与旧站 POST /APSAPI/APSDataExcel 一致）。</summary>
    [HttpPost]
    public override IActionResult APSDataExcel()
    {
        var result = base.APSDataExcel();
        if (LegacyHttpContext?.Response.HasStarted == true)
            return new EmptyResult();
        return result;
    }
}
