using EasyManufacture.Licence;
using System.Net;
using System.Net.Mail;

namespace EasyManufacture.Infrastructure.Legacy;

/// <summary>邮件发送（自 EasyManufacture.Core/Email.cs 迁入）。</summary>
public static class Email
{
    public static bool SendMail(List<string> lstAddress, string subject, string bodyContent)
    {
        var emailConfig = AppInfo.EmailConfig.Split(';');
        if (emailConfig.Length < 4) return false;

        using var msg = new MailMessage();
        foreach (var s in lstAddress)
            msg.To.Add(s);

        msg.From = new MailAddress(emailConfig[1], "APS系统通知", System.Text.Encoding.UTF8);
        msg.Subject = subject;
        msg.SubjectEncoding = System.Text.Encoding.UTF8;
        msg.Body = bodyContent;
        msg.BodyEncoding = System.Text.Encoding.UTF8;
        msg.IsBodyHtml = true;
        msg.Priority = MailPriority.High;

        using var client = new SmtpClient
        {
            Credentials = new NetworkCredential(emailConfig[1], emailConfig[2]),
            Port = int.Parse(emailConfig[3]),
            Host = emailConfig[0]
        };
        client.Send(msg);
        return true;
    }
}
