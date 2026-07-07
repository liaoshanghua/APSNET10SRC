using EasyManufacture.Licence;
using System.Data;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>LegacyCore 未声明的共用实例状态。</summary>
public partial class ApsCoreEngine
{
    protected string lang = "zh-CN";
    protected DataTable? dtLanguage;
    protected bool result;
    protected string msg = "";
    protected Licence.SystemLog systemLog = new();
}
