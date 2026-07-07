using EasyManufacture.Core.ConfigTable;
using EasyManufacture.Entitys;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>
/// APSData 钩子字段：setWhere、MSSQLCore 实例、列配置列表。
/// 与 LegacyCore / ApsDataFields 配合；SetDt 由 YrfDicHooks 或旧 dic switch 挂载。
/// </summary>
public delegate void SetWhere(MSSQLCore mSSQLCore);

public partial class ApsCoreEngine
{
    protected MSSQLCore? mSSQLCore;
    protected SetWhere? setWhere;
    protected List<int> lstAllSelectDicID { get; } = [24430];

    /// <summary>列配置（与旧 APSCore 一致：按字典 ID 每项一个内层 List，勿预置空 List）。</summary>
    protected List<List<ElementTableOuput>> ElementColumn = new();
    protected DataTable? dataFooter;
    protected List<ElementTableOuput> elementTableOuputs = new();
    protected List<SearchForm> searchFormsAll = new();
    protected List<List<SearchForm>> searchForms = new();
    protected List<List<Jspreadsheet>> ExcelColumns = new();
    protected int count;
}
