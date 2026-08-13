using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace EasyManufacture.Api.Infrastructure;

internal sealed class ApsStatusForm : Form
{
    private readonly string _homeUrl;
    private readonly string _logsDir;

    public ApsStatusForm(string title, string homeUrl, string logsDir)
    {
        _homeUrl = homeUrl;
        _logsDir = logsDir;

        Text = title;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        ClientSize = new Size(420, 250);
        ShowInTaskbar = true;

        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;

            e.Cancel = true;
            if (ApsShutdownConfirmation.TryConfirmShutdown())
                ApsShutdownConfirmation.ApproveAndStop();
            else
                Hide();
        };

        var info = new Label
        {
            AutoSize = false,
            Location = new Point(16, 16),
            Size = new Size(388, 64),
            Text = $"服务运行中{Environment.NewLine}{homeUrl}{Environment.NewLine}日志目录：{logsDir}"
        };

        var btnOpen = new Button
        {
            Text = "打开管理页",
            Location = new Point(16, 96),
            Size = new Size(120, 32)
        };
        btnOpen.Click += (_, _) => OpenUrl(_homeUrl);

        var btnLogs = new Button
        {
            Text = "打开日志",
            Location = new Point(152, 96),
            Size = new Size(120, 32)
        };
        btnLogs.Click += (_, _) =>
        {
            Directory.CreateDirectory(_logsDir);
            Process.Start(new ProcessStartInfo("explorer.exe", _logsDir) { UseShellExecute = true });
        };

        var btnConsole = new Button
        {
            Text = "显示控制台",
            Location = new Point(288, 96),
            Size = new Size(116, 32)
        };
        btnConsole.Click += (_, _) => ApsConsoleWindow.Show();

        var btnDisableAutoStart = new Button
        {
            Text = "禁止开机启动",
            Location = new Point(16, 144),
            Size = new Size(120, 32)
        };
        btnDisableAutoStart.Click += (_, _) => DisableAutoStart();

        var btnDb = new Button
        {
            Text = "还原/清空库",
            Location = new Point(152, 144),
            Size = new Size(120, 32)
        };
        btnDb.Click += (_, _) => ApsDatabaseRestoreForm.ShowRestoreDialog(this);

        var btnHide = new Button
        {
            Text = "最小化到托盘",
            Location = new Point(16, 192),
            Size = new Size(120, 32)
        };
        btnHide.Click += (_, _) => Hide();

        var btnExit = new Button
        {
            Text = "退出 APS",
            Location = new Point(288, 192),
            Size = new Size(116, 32)
        };
        btnExit.Click += (_, _) => ApsShutdownConfirmation.RequestShutdown();

        Controls.Add(info);
        Controls.Add(btnOpen);
        Controls.Add(btnLogs);
        Controls.Add(btnConsole);
        Controls.Add(btnDisableAutoStart);
        Controls.Add(btnDb);
        Controls.Add(btnHide);
        Controls.Add(btnExit);
    }

    private void DisableAutoStart()
    {
        var confirm = MessageBox.Show(
            this,
            "确定禁止开机启动？\n将删除计划任务 APS / APS-Logon，并阻止下次启动时自动重新注册。",
            "禁止开机启动",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes)
            return;

        var (ok, message) = ApsAutoStartInstaller.TryUninstall();
        MessageBox.Show(
            this,
            message,
            "禁止开机启动",
            MessageBoxButtons.OK,
            ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    public void ShowAndActivate()
    {
        if (InvokeRequired)
        {
            BeginInvoke(ShowAndActivate);
            return;
        }

        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
