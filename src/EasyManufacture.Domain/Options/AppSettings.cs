namespace EasyManufacture.Domain.Options;

public sealed class AppSettings
{
    public const string SectionName = "App";

    public string AppCode { get; set; } = "ISGO";
    public string LoginUrl { get; set; } = "/Login/Login";
    public string PushType { get; set; } = "YS";
    public string IsVue { get; set; } = "0";
    public string SchedulingDays { get; set; } = "60";
    public string IsSaveLog { get; set; } = "0";
    public string ConfigStartWeek { get; set; } = "1";
    public string IsSafe { get; set; } = "0";
    public string AesKeyt { get; set; } = "";
    public string AesIv { get; set; } = "";
    public string ErpPro { get; set; } = "";
}

public sealed class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    public string MSSQLConnectionString { get; set; } = string.Empty;
    public string MSSQLConnectionStringSCM { get; set; } = string.Empty;

    public string NormalizedMSSQLConnectionString =>
        Data.SqlConnectionStringHelper.Normalize(MSSQLConnectionString);

    public string NormalizedMSSQLConnectionStringSCM =>
        Data.SqlConnectionStringHelper.Normalize(MSSQLConnectionStringSCM);
}
