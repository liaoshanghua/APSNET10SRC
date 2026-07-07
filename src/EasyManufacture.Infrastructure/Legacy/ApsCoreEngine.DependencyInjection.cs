using EasyManufacture.Application.Abstractions;
using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using EasyManufacture.Licence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>启用 LegacyCore 时的 DI 注入（替代精简版 Host，避免字段重复）。</summary>
public partial class ApsCoreEngine
{
    private readonly IRequestBodyAccessor _bodyAccessor;
    private readonly ManufactureDbContext _dbContext;
    private readonly EasyManufactureEntities _entities;
    private readonly JDRegister _jdRegister;
    protected readonly ICurrentUser _currentUser;
    protected JDRegister jDRegister => _jdRegister;

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
        try
        {
            dtWorkTimes = SqlHelper.ExecuteDataTable(
                "SELECT WorkingTimesID,WorkingTimesName FROM APS_WorkingTimes") ?? new DataTable();
        }
        catch
        {
            dtWorkTimes = new DataTable();
        }
        BindHttpContext(httpContextAccessor);
    }

    public string BodyJson
    {
        get => _bodyAccessor.BodyJson;
        set => _bodyAccessor.BodyJson = value;
    }

    protected V_Dev_Account? dev_Account =>
        V_Dev_Account.GetDev_Account() ?? MapAccount(_currentUser.Account);

    protected EasyManufactureEntities Entities => _entities;

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

    public string RunSaveData()
    {
#if LEGACY_APS_CORE
        return SaveData().ToJson();
#else
        return SaveData();
#endif
    }

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

    internal static JArray? ParseSaveDataJson(string? bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return null;
        var text = bodyJson.Trim();
        if (text.StartsWith('=')) text = text[1..].Trim();
        if (text.Contains('%') && text.Contains('='))
        {
            var idx = text.IndexOf('=');
            if (idx >= 0 && idx < text.Length - 1)
            {
                var value = Uri.UnescapeDataString(text[(idx + 1)..].Trim());
                if (value.StartsWith('[') || value.StartsWith('{')) text = value;
            }
        }
        try
        {
            var token = JsonConvert.DeserializeObject(text);
            return token switch
            {
                JArray arr => arr,
                JObject obj => obj["data"] is JArray nested ? nested
                    : obj["list"] is JArray l ? l
                    : obj["rows"] is JArray r ? r
                    : new JArray(obj),
                _ => null
            };
        }
        catch { return null; }
    }
}
