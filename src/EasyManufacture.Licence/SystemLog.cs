using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace EasyManufacture.Licence;

/// <summary>系统日志（写入 Dev_SysLog）</summary>
public class SystemLog
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();

    public SystemLog()
    {
        _sw.Start();
    }

    public enum SystemLogType
    {
        页面错误, 数据库错误, 登录错误, 登录成功, 接口访问, 页面被访问, 导入, 导出, SQL删除, SQL添加, SQL更新, SQL查询, SQL存储过程查询, 程序异常, 接口推送, 接口访问错误, 亮灯错误, 邮件发送日志, 接口推送失败,
        备料, 亮灯日志, 备料错误, 自动日计划, 单据退回, 获取ERP数据, 下载PDF, 查看PDF, 下载CAD, 下载附件, 下载Excel, 下载Word, 下载图片, 下载视频, 下载音频, 上传文件, 上传图片, 上传视频, 上传音频, 上传Excel, 上传Word, 上传PDF, 上传CAD, 上传附件, 上传压缩包, 其他日志, 其他错误, 其他信息, 其他警告, 其他调试, 其他跟踪, 其他异常, 其他通知, 其他提示, 其他警告信息, 其他错误信息, 其他调试信息, 其他跟踪信息, 其他异常信息, 其他通知信息, 其他提示信息, 其他警告日志, 其他错误日志, 其他调试日志, 其他跟踪日志, 其他异常日志, 其他通知日志, 其他提示日志, 获取ERP数据错误, 获取ERP数据完成
    }

    public Stopwatch TimeWatch => _sw;

    public void SaveLog(
        SystemLogType systemLogType,
        string content,
        ISysLogUser? dev_Account = null,
        SqlParameter[]? parameters = null,
        double spents = 0,
        int dicID = 0)
    {
        try
        {
            string? menuName = null;
            var ctx = LicenceRuntime.Http.HttpContext;
            if (ctx != null)
            {
                var menuHeader = ctx.Request.Headers["Vuemenunameforlog"].FirstOrDefault();
                if (!string.IsNullOrEmpty(menuHeader))
                {
                    menuName = Uri.UnescapeDataString(menuHeader);
                    content += "。菜单地址：" + menuName;
                }
            }

            if (parameters != null)
            {
                content += "内容：";
                foreach (var p in parameters)
                {
                    if (p != null)
                        content += p.ParameterName + ":" + p.Value;
                }
            }

            var url = ctx?.Request.Headers["Referer"].FirstOrDefault() ?? "";
            var createdBy = dev_Account?.Account;
            var createdByName = dev_Account?.Name;
            var elapsed = spents == 0 ? Math.Round(_sw.Elapsed.TotalSeconds, 1) : spents;

            InsertLog(systemLogType.ToString(), content, url, createdBy, createdByName, elapsed, menuName);
            _sw.Restart();
        }
        catch
        {
            // 与旧版一致：日志失败不阻断业务
        }
    }

    private static void InsertLog(
        string title,
        string content,
        string url,
        string? createdBy,
        string? createdByName,
        double spents,
        string? menuName)
    {
        if (string.IsNullOrEmpty(LicenceRuntime.SqlConnectionString))
            return;

        const string sql = """
            INSERT INTO Dev_SysLog (Title, Content, Url, CreatedBy, CreatedByName, CreatedOn, Spents, MenuName)
            VALUES (@Title, @Content, @Url, @CreatedBy, @CreatedByName, @CreatedOn, @Spents, @MenuName)
            """;

        using var conn = new SqlConnection(LicenceRuntime.SqlConnectionString);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Title", title);
        cmd.Parameters.AddWithValue("@Content", content.Length > 4000 ? content[..4000] : content);
        cmd.Parameters.AddWithValue("@Url", (object?)url ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedByName", (object?)createdByName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
        cmd.Parameters.AddWithValue("@Spents", spents);
        var menuValue = string.IsNullOrEmpty(menuName)
            ? (object)DBNull.Value
            : (menuName.Length > 200 ? menuName[..200] : menuName);
        cmd.Parameters.AddWithValue("@MenuName", menuValue);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}
