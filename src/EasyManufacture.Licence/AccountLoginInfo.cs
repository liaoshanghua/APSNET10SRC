namespace EasyManufacture.Licence;

/// <summary>记录登录入侵</summary>
public class AccountLoginInfo
{
    public string Account { get; set; } = "";
    public DateTime LastTime { get; set; }
    public int LoginCount { get; set; }
    public string IPAddress { get; set; } = "";
    public int ErrorCount { get; set; }
    public bool IsLock { get; set; }
}

public class LockedIpEntry
{
    public string IPAddress { get; set; } = "";
    public bool IsLock { get; set; }
    public DateTime LastTime { get; set; } = DateTime.Now;
    public int Visits { get; set; }
}
