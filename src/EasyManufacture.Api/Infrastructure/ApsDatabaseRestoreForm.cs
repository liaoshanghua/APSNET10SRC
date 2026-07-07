using System.Drawing;
using System.Windows.Forms;

namespace EasyManufacture.Api.Infrastructure;

/// <summary>托盘 / 状态窗：还原备份或按前缀清空 APS 业务表。</summary>
internal sealed class ApsDatabaseRestoreForm : Form
{
    private readonly RadioButton _rbRestore;
    private readonly RadioButton _rbTruncateOnly;
    private readonly TextBox _txtBackup;
    private readonly Button _btnBrowse;
    private readonly Label _lblTarget;
    private readonly CheckBox _chkTruncateAfterRestore;
    private readonly TextBox _txtLog;
    private readonly Button _btnRun;

    public ApsDatabaseRestoreForm()
    {
        Text = "数据库还原 / 清空业务表";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 400);

        var lblInfo = new Label
        {
            AutoSize = false,
            Location = new Point(16, 12),
            Size = new Size(488, 48),
            Text = "清空 dbo 下表名以以下前缀开头的用户表：" + Environment.NewLine + ApsDatabaseRestoreService.PrefixDescription
        };

        _rbRestore = new RadioButton
        {
            Text = "从 .bak 还原数据库",
            Location = new Point(16, 68),
            AutoSize = true,
            Checked = true
        };
        _rbTruncateOnly = new RadioButton
        {
            Text = "仅清空前缀表（不还原，APS 可继续运行）",
            Location = new Point(16, 92),
            AutoSize = true
        };

        var lblBackup = new Label
        {
            Text = "备份文件：",
            Location = new Point(16, 124),
            AutoSize = true
        };
        _txtBackup = new TextBox
        {
            Location = new Point(16, 144),
            Size = new Size(388, 23),
            ReadOnly = true
        };
        _btnBrowse = new Button
        {
            Text = "浏览…",
            Location = new Point(412, 142),
            Size = new Size(92, 28)
        };

        _chkTruncateAfterRestore = new CheckBox
        {
            Text = "还原成功后按前缀清空业务表",
            Location = new Point(32, 176),
            AutoSize = true,
            Checked = true
        };

        _lblTarget = new Label
        {
            AutoSize = false,
            Location = new Point(16, 204),
            Size = new Size(488, 36),
            ForeColor = Color.DimGray
        };
        RefreshTargetLabel();

        _txtLog = new TextBox
        {
            Location = new Point(16, 244),
            Size = new Size(488, 96),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Visible = false
        };

        _btnRun = new Button
        {
            Text = "执行",
            Location = new Point(324, 352),
            Size = new Size(88, 32)
        };
        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(416, 352),
            Size = new Size(88, 32),
            DialogResult = DialogResult.Cancel
        };

        _rbRestore.CheckedChanged += (_, _) => UpdateModeUi();
        _rbTruncateOnly.CheckedChanged += (_, _) => UpdateModeUi();
        _btnBrowse.Click += (_, _) => BrowseBackup();
        _btnRun.Click += (_, _) => RunOperation();

        Controls.Add(lblInfo);
        Controls.Add(_rbRestore);
        Controls.Add(_rbTruncateOnly);
        Controls.Add(lblBackup);
        Controls.Add(_txtBackup);
        Controls.Add(_btnBrowse);
        Controls.Add(_chkTruncateAfterRestore);
        Controls.Add(_lblTarget);
        Controls.Add(_txtLog);
        Controls.Add(_btnRun);
        Controls.Add(btnCancel);

        CancelButton = btnCancel;
        UpdateModeUi();
    }

    public static void ShowRestoreDialog(IWin32Window? owner)
    {
        using var form = new ApsDatabaseRestoreForm();
        form.ShowDialog(owner);
    }

    private void RefreshTargetLabel()
    {
        try
        {
            var cs = ApsDatabaseRestoreService.ReadConnectionString();
            var (server, db) = ApsDatabaseRestoreService.ParseServerDatabase(cs);
            _lblTarget.Text = $"目标服务器：{server}    数据库：{db}";
        }
        catch (Exception ex)
        {
            _lblTarget.Text = "无法读取连接串：" + ex.Message;
            _btnRun.Enabled = false;
        }
    }

    private void UpdateModeUi()
    {
        var restore = _rbRestore.Checked;
        _txtBackup.Enabled = restore;
        _btnBrowse.Enabled = restore;
        _chkTruncateAfterRestore.Enabled = restore;
        _chkTruncateAfterRestore.Visible = restore;
    }

    private void BrowseBackup()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "SQL Server 备份 (*.bak)|*.bak|所有文件 (*.*)|*.*",
            Title = "选择数据库备份文件"
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtBackup.Text = dlg.FileName;
    }

    private void RunOperation()
    {
        if (_rbRestore.Checked)
        {
            if (string.IsNullOrWhiteSpace(_txtBackup.Text))
            {
                MessageBox.Show(this, "请选择 .bak 备份文件。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var msg = "将还原数据库并"
                + (_chkTruncateAfterRestore.Checked ? "清空匹配前缀的业务表。" : "保留备份中的业务数据。")
                + Environment.NewLine + Environment.NewLine
                + "APS 将自动退出，并在新窗口中执行还原。" + Environment.NewLine
                + "完成后请重新运行 APS-启动.bat。" + Environment.NewLine + Environment.NewLine
                + "此操作不可撤销，是否继续？";

            if (MessageBox.Show(this, msg, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                != DialogResult.Yes)
                return;

            try
            {
                ApsDatabaseRestoreService.LaunchRestoreScript(
                    _txtBackup.Text.Trim(),
                    _chkTruncateAfterRestore.Checked);

                MessageBox.Show(this,
                    "已启动还原脚本，APS 即将退出。请在 PowerShell 窗口查看进度，完成后重新启动 APS。",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
                Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    ApsShutdownConfirmation.ApproveAndStop();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;
        }

        if (MessageBox.Show(this,
                "将清空所有匹配前缀的业务表数据，是否继续？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        _btnRun.Enabled = false;
        _txtLog.Visible = true;
        _txtLog.Text = "正在清空，请稍候…" + Environment.NewLine;
        ClientSize = new Size(520, 400);

        Task.Run(() =>
        {
            try
            {
                var tables = ApsDatabaseRestoreService.TruncatePrefixTables();
                BeginInvoke(() =>
                {
                    _txtLog.Text += $"完成，共清空 {tables.Count} 张表：" + Environment.NewLine
                        + string.Join(Environment.NewLine, tables);
                    MessageBox.Show(this,
                        $"已清空 {tables.Count} 张表。",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    _btnRun.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                BeginInvoke(() =>
                {
                    _txtLog.Text += "失败：" + ex.Message;
                    MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _btnRun.Enabled = true;
                });
            }
        });
    }
}
