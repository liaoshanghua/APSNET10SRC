namespace EasyManufacture.Domain.Options;

/// <summary>对应旧版 Global.asax Application_Start 中的 System.Timers 定时任务。</summary>
public sealed class ScheduledTasksOptions
{
    public const string SectionName = "ScheduledTasks";

    /// <summary>是否启用后台定时任务（对应 Global.asax 定时器总开关）。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>启动时执行数据库结构自检/补丁（Global.asax 中 Dev_DictionaryField 等 ALTER）。</summary>
    public bool RunSchemaUpgradeOnStartup { get; set; } = true;

    /// <summary>盈瑞丰产能 Excel 扫描目录（PushType=YRF）。</summary>
    public string YrfExcelDirectory { get; set; } = @"D:\共享文件\产能";

    /// <summary>ISGO 图纸目录（PushType=ISGO）。</summary>
    public string IsgoDrawingDirectory { get; set; } = @"D:\共享文件\JS004\图纸";

    /// <summary>ISGO 3D/CAD 目录（PushType=ISGO）。</summary>
    public string IsgoCadDirectory { get; set; } = @"D:\共享文件\JS004\3D文档";
}
