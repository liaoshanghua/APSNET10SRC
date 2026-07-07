using EasyManufacture.Application.Abstractions;
using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using EasyManufacture.Infrastructure.Legacy;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Reflection;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// 精简模式宿主：构造函数、BodyJson、EF/Dapper 注入。
/// EnableLegacyApsCoreSource=false 时使用；全量模式见 DependencyInjection.cs。
/// </summary>
public delegate void SetDt(ref DataTable dt);

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public partial class ApsCoreEngine
{
    private readonly IRequestBodyAccessor _bodyAccessor;
    private readonly ManufactureDbContext _dbContext;
    private readonly EasyManufactureEntities _entities;
    private readonly JDRegister _jdRegister;

    public ApsCoreEngine(
        IRequestBodyAccessor bodyAccessor,
        ICurrentUser currentUser,
        ManufactureDbContext dbContext,
        JDRegister jdRegister,
        IHttpContextAccessor httpContextAccessor)
    {
        _bodyAccessor = bodyAccessor;
        _dbContext = dbContext;
        _entities = new EasyManufactureEntities(dbContext);
        _currentUser = currentUser;
        _jdRegister = jdRegister;
        _dtWorkTimes = SqlHelper.ExecuteDataTable("SELECT WorkingTimesID,WorkingTimesName FROM APS_WorkingTimes");
        BindHttpContext(httpContextAccessor);
    }

    private readonly ICurrentUser _currentUser;

    public string BodyJson
    {
        get => _bodyAccessor.BodyJson;
        set => _bodyAccessor.BodyJson = value;
    }

    protected V_Dev_Account? dev_Account =>
        V_Dev_Account.GetDev_Account() ?? MapAccount(_currentUser.Account);

    protected EasyManufactureEntities Entities => _entities;

    protected DataTable? dtLanguage { get; set; }
    protected string lang { get; set; } = "zh-CN";

    protected bool isDownload;
    protected SetDt? setDt;
    protected JObject? jObject;
    /// <summary>旧 APSAPIController 实例字段（SetDt/setDetail 等回调共用）。</summary>
    protected bool result;
    protected string msg = "";
    protected List<ElementTableOuput> AppColumns = new();

    protected int dicID;
    protected readonly SystemLog systemLog = new();

    List<int> lstDayList { get; } = [5585, 6734, 7946, 7945, 7955, 7954, 7957, 7956, 9016, 9020, 9025, 10115];

    private static V_Dev_Account? MapAccount(Domain.Models.DevAccount? account)
    {
        if (account == null) return null;
        return new V_Dev_Account
        {
            Account = account.Account,
            Name = account.Name,
            OrganizeID = account.OrganizeID ?? 0,
            CenterID = account.CenterID ?? 0,
            GroupID = account.GroupID ?? 0,
            WorkFlowInstanceID = account.WorkFlowInstanceID ?? account.Extend1,
            Extend1 = account.Extend1,
            Extend2 = account.Extend2,
            Extend3 = account.Extend3,
            Status = account.Status
        };
    }

    public string RunGetConfig() => GetConfig();

    public string RunSaveData() => SaveData();

    /// <summary>每次 SaveData 前重置，避免 scoped 实例上残留 jArray / 字段缓存。</summary>
    public void ResetSaveDataState()
    {
        jArray = null;
        lstKeyValue.Clear();
        dev_DictionaryFieldsAll = new List<Dev_DictionaryField>();
        isAllOK = true;
        allMsg = "";
        listErrorRows.Clear();
        ReturnSql = "";
        dicInsert.Clear();
    }

    /// <summary>SaveData 请求体须为 JSON 数组；兼容单对象、form 提交、外层 data 包装。</summary>
    internal static JArray? ParseSaveDataJson(string? bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson))
            return null;

        var text = bodyJson.Trim();

        // 部分客户端 form 提交：=[{...}] 或 key=urlencoded
        if (text.StartsWith('='))
            text = text[1..].Trim();

        if (text.Contains('%') && text.Contains('='))
        {
            var idx = text.IndexOf('=');
            if (idx >= 0 && idx < text.Length - 1)
            {
                var value = Uri.UnescapeDataString(text[(idx + 1)..].Trim());
                if (value.StartsWith('[') || value.StartsWith('{'))
                    text = value;
            }
        }

        try
        {
            var token = JsonConvert.DeserializeObject(text);
            switch (token)
            {
                case JArray arr:
                    return arr;
                case JObject obj:
                    foreach (var key in new[] { "data", "list", "rows", "Data", "List", "Rows" })
                    {
                        if (obj[key] is JArray nested)
                            return nested;
                    }
                    return new JArray(obj);
                case JValue { Type: JTokenType.String } s when !string.IsNullOrWhiteSpace(s.ToString()):
                    return ParseSaveDataJson(s.ToString());
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    internal static bool TryGetRowDicId(JObject row, out int dicId)
    {
        dicId = 0;
        foreach (var key in new[] { "dicID", "DicID", "DICID", "DictionaryID" })
        {
            if (!row.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var token))
                continue;
            var text = token?.ToString();
            if (string.IsNullOrWhiteSpace(text))
                continue;
            if (int.TryParse(text, out dicId))
                return true;
        }
        return false;
    }

    /// <summary>合计行/占位行：无 dicID 且 RowNumber 为空时跳过（与旧版一致，但允许带 dicID 的业务行 RowNumber 为空）。</summary>
    internal static bool ShouldSkipSaveDataRow(JObject row)
    {
        if (TryGetRowDicId(row, out _))
            return false;

        if (!row.ContainsKey("RowNumber"))
            return false;

        return string.IsNullOrEmpty(row["RowNumber"]?.ToString());
    }
}
