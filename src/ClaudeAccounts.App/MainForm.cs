using System.Diagnostics;
using ClaudeAccounts.Core;

namespace ClaudeAccounts.App;

internal sealed class MainForm : Form
{
    private readonly AccountStore store;
    private readonly bool preview;
    private readonly ListBox accounts = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 76, IntegralHeight = false, BorderStyle = BorderStyle.None, BackColor = Color.White, AccessibleName = "Account profiles", DisplayMember = "Name" };
    private readonly Label empty = new() { Text = "No accounts", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(35), ForeColor = Theme.Muted, BackColor = Color.White, Visible = false };
    private readonly Label installationLabel = Theme.Label("Looking for Claude Desktop…", 9);
    private readonly Label status = Theme.Label("", 9);
    private readonly Button launchButton;
    private readonly Button renameButton;
    private readonly Button deleteButton;
    private readonly Button signInButton;
    private readonly SignInRoute signInRoute;
    private readonly System.Windows.Forms.Timer routeTimer = new() { Interval = 600000 };
    private readonly NotifyIcon tray;
    private readonly ContextMenuStrip trayMenu = new();
    private ClaudeInstallation? installation;
    private bool quitting;
    private bool launching;
    private bool refreshing;
    private bool released;
    private string? selectedId;
    private Account? Selected => accounts.SelectedItem as Account;

    internal MainForm(AccountStore store, bool preview = false)
    {
        this.store = store; this.preview = preview;
        signInRoute = new SignInRoute(new WindowsProtocolRegistry(), Path.Combine(store.Root, "sign-in-route.json"));
        if (!preview) signInRoute.Restore();
        routeTimer.Tick += (_, _) => EndSignIn();
        Text = "Claude Accounts"; ClientSize = new Size(740, 470); MinimumSize = new Size(680, 390);
        AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10); BackColor = Theme.Background; ForeColor = Theme.Ink;
        StartPosition = FormStartPosition.CenterScreen;
        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("ClaudeAccounts.App.Assets.app.ico"))
        {
            if (iconStream != null) { using var sourceIcon = new Icon(iconStream); Icon = (Icon)sourceIcon.Clone(); }
            else Icon = SystemIcons.Application;
        }
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 12), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        Controls.Add(root);

        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.Controls.Add(Theme.Button("+ Add account", AddAccount), 0, 0);
        var settings = Theme.Button("Settings", ShowSettings); settings.Margin = Padding.Empty; toolbar.Controls.Add(settings, 1, 0);
        root.Controls.Add(toolbar, 0, 0);
        var listPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        listPanel.Controls.Add(accounts); listPanel.Controls.Add(empty); root.Controls.Add(listPanel, 0, 1);
        accounts.DrawItem += DrawAccount;
        accounts.SelectedIndexChanged += (_, _) => { if (!refreshing) { selectedId = Selected?.Id; UpdateSelection(); } };
        accounts.DoubleClick += async (_, _) => await OpenSelectedAsync();
        accounts.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; await OpenSelectedAsync(); } };

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty, Padding = new Padding(0, 12, 0, 0) };
        launchButton = Theme.Button("Open", () => { _ = OpenSelectedAsync(); }, true);
        renameButton = Theme.Button("Rename", RenameAccount);
        deleteButton = Theme.Button("Delete", DeleteAccount);
        signInButton = Theme.Button("Browser sign-in", () => { _ = BeginSignInAsync(); });
        foreach (var button in new[] { launchButton, signInButton, renameButton, deleteButton }) actions.Controls.Add(button);
        root.Controls.Add(actions, 0, 2);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1, Margin = Padding.Empty };
        installationLabel.ForeColor = Theme.Muted; installationLabel.AutoSize = false; installationLabel.Dock = DockStyle.Fill;
        status.ForeColor = Theme.Accent; status.AutoSize = false; status.AutoEllipsis = true; status.Dock = DockStyle.Fill;
        footer.Controls.Add(status); root.Controls.Add(footer, 0, 3);
        var legal = Theme.Label("Not affiliated with Anthropic", 8.5f); legal.ForeColor = Theme.Muted; root.Controls.Add(legal, 0, 4);

        tray = new NotifyIcon { Icon = Icon, Text = "Claude Accounts", Visible = !preview, ContextMenuStrip = trayMenu };
        tray.DoubleClick += (_, _) => RestoreWindow();
        FormClosing += (_, e) => { if (!quitting && !preview && store.Settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); tray.ShowBalloonTip(1500, "Claude Accounts", "Still available from the system tray. Choose Quit to exit.", ToolTipIcon.Info); } };
        Shown += async (_, _) =>
        {
            if (!preview) await DetectAsync();
            else { installationLabel.Text = "Claude Desktop  ·  Separate local profiles"; UpdateSelection(); }
            if (store.RecoveryNotice != null) MessageBox.Show(this, store.RecoveryNotice, "Recovered previous save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        RefreshAccounts();
    }

    private void DrawAccount(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var a = (Account)accounts.Items[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? Theme.Selected : Color.White);
        e.Graphics.FillRectangle(background, e.Bounds);
        float scale = DeviceDpi / 96f;
        int pad = (int)(18 * scale), badgeSize = (int)(42 * scale);
        var badge = new Rectangle(e.Bounds.Left + pad, e.Bounds.Top + (e.Bounds.Height - badgeSize) / 2, badgeSize, badgeSize);
        Color badgeColor;
        try { badgeColor = ColorTranslator.FromHtml(a.Color); } catch { badgeColor = Theme.Accent; }
        using var brush = new SolidBrush(badgeColor); e.Graphics.FillEllipse(brush, badge);
        using var initialFont = new Font("Segoe UI", 14, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, a.Name[..1].ToUpperInvariant(), initialFont, badge, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using var nameFont = new Font("Segoe UI", 11, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 9);
        int x = badge.Right + pad, width = e.Bounds.Right - x - pad;
        TextRenderer.DrawText(e.Graphics, a.Name, nameFont, new Rectangle(x, e.Bounds.Top + (int)(12 * scale), width, (int)(26 * scale)), Theme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        string caption = a.LastOpened == null ? "Not opened yet" : "Last opened " + a.LastOpened;
        TextRenderer.DrawText(e.Graphics, caption, subFont, new Rectangle(x, e.Bounds.Top + (int)(39 * scale), width, (int)(24 * scale)), Theme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
    }

    private void RefreshAccounts(string? select = null)
    {
        selectedId = select ?? selectedId; refreshing = true;
        accounts.BeginUpdate(); accounts.Items.Clear();
        foreach (var a in store.Accounts.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)) accounts.Items.Add(a);
        int index = -1;
        for (int i = 0; i < accounts.Items.Count; i++) if (((Account)accounts.Items[i]).Id == selectedId) index = i;
        accounts.SelectedIndex = index >= 0 ? index : accounts.Items.Count > 0 ? 0 : -1;
        selectedId = Selected?.Id; accounts.EndUpdate(); refreshing = false;
        empty.Visible = accounts.Items.Count == 0;
        UpdateSelection(); RebuildTray();
    }

    private void UpdateSelection()
    {
        var a = Selected;
        launchButton.Enabled = a != null && !launching;
        renameButton.Enabled = deleteButton.Enabled = a != null && !launching;
        signInButton.Enabled = !preview && !launching && (signInRoute.IsActive || a != null);
        signInButton.Text = signInRoute.IsActive ? "End browser sign-in" : "Browser sign-in";
    }

    private async Task DetectAsync()
    {
        installationLabel.Text = "Looking for Claude Desktop…";
        installation = await ClaudeLocator.FindAsync(store.Settings.ClaudeExecutable);
        if (IsDisposed) return;
        installationLabel.Text = installation == null ? "Claude Desktop not found. Select its executable in Settings." : "Claude Desktop " + installation.Version + "  ·  " + installation.Source;
        UpdateSelection();
    }

    private async Task OpenSelectedAsync() { if (Selected is { } a) await OpenAccountAsync(a); }
    private async Task<bool> OpenAccountAsync(Account a)
    {
        if (launching) return false;
        if (signInRoute.IsActive) EndSignIn();
        launching = true; UpdateSelection();
        try
        {
            // Re-resolve for Store updates that replaced the executable since the last launch.
            installation = await ClaudeLocator.FindAsync(store.Settings.ClaudeExecutable);
            if (installation == null) throw new FileNotFoundException("Claude Desktop was not found. Open Settings and select its installed executable.");
            string profile = store.ProfilePath(a);
            var start = ClaudeLaunch.CreateStartInfo(installation.Path, profile);
            Directory.CreateDirectory(profile);
            using var process = Process.Start(start) ?? throw new IOException("Claude Desktop did not start.");
            a.LastOpened = DateTime.Now.ToString("g"); store.SaveAccounts();
            status.Text = "Opened " + a.Name;
            RefreshAccounts(a.Id);
            await Task.Delay(1200);
            if (process.HasExited && process.ExitCode != 0) throw new IOException("Claude exited with code " + process.ExitCode + ". Check Settings and your Claude Desktop installation.");
            return true;
        }
        catch (Exception ex) { status.Text = "Could not launch " + a.Name + "."; Program.ShowError(ex); return false; }
        finally { launching = false; if (!IsDisposed) UpdateSelection(); }
    }

    private async Task BeginSignInAsync()
    {
        try
        {
            if (signInRoute.IsActive) { EndSignIn(); return; }
            if (Selected is not { } a) return;
            if (!await OpenAccountAsync(a) || installation == null) return;
            string profile = store.ProfilePath(a);
            signInRoute.Start(installation.Path, profile);
            if (!WindowsProtocolRegistry.EffectiveCommandContains(profile))
            {
                signInRoute.Restore();
                bool sent = CallbackDialog.Complete(this, a.Name, installation.Path, profile);
                status.Text = sent ? "Sign-in link sent to " + a.Name : "Browser sign-in canceled";
                UpdateSelection(); return;
            }
            routeTimer.Stop(); routeTimer.Start();
            status.Text = "Browser sign-in → " + a.Name + " (10 minutes)";
            UpdateSelection();
            MessageBox.Show(this, "Choose Sign in with Google in the “" + a.Name + "” Claude window.\n\nClaude sign-in links now open that profile. Routing ends after 10 minutes, when you click End browser sign-in, or when you quit this launcher.\n\nThe callback is passed directly to Claude Desktop. This launcher does not read or store it.", "Browser sign-in", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Program.ShowError(ex); UpdateSelection(); }
    }

    private void EndSignIn()
    {
        routeTimer.Stop();
        try { bool restored = signInRoute.Restore(); status.Text = restored ? "Browser sign-in ended" : "Link handler changed outside this app; left unchanged"; }
        catch (Exception ex) { Program.ShowError(ex); }
        UpdateSelection();
    }

    private void AddAccount()
    {
        string? name = NameDialog.Ask(this, "Add account", ""); if (name == null) return;
        var a = store.Add(name); RefreshAccounts(a.Id);
        status.Text = "Added " + a.Name;
    }
    private void RenameAccount() { if (Selected is not { } a) return; string? name = NameDialog.Ask(this, "Rename account", a.Name); if (name == null) return; store.Rename(a, name); RefreshAccounts(a.Id); }
    private void DeleteAccount()
    {
        if (Selected is not { } a) return;
        if (MessageBox.Show(this, "Delete “" + a.Name + "” from the account list?\n\nIts saved Claude files stay on disk.", "Delete account", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        if (signInRoute.IsActive) EndSignIn();
        store.Delete(a); selectedId = null; RefreshAccounts(); status.Text = "Deleted " + a.Name;
    }
    private async void ShowSettings()
    {
        try { using var dialog = new SettingsForm(store, installation); if (dialog.ShowDialog(this) == DialogResult.OK) await DetectAsync(); }
        catch (Exception ex) { Program.ShowError(ex); }
    }
    private void RebuildTray()
    {
        // Clear disposes menu entries and their callbacks before rebuilding.
        foreach (ToolStripItem item in trayMenu.Items.Cast<ToolStripItem>().ToArray()) item.Dispose();
        trayMenu.Items.Clear();
        trayMenu.Items.Add("Open launcher", null, (_, _) => RestoreWindow());
        trayMenu.Items.Add(new ToolStripSeparator());
        foreach (var a in store.Accounts.OrderBy(a => a.Name).Take(20))
            trayMenu.Items.Add(a.Name.Replace("&", "&&"), null, async (_, _) => await OpenAccountAsync(a));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Quit", null, (_, _) => { quitting = true; Close(); });
    }

    internal void RestoreWindow()
    {
        Show(); if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate(); Program.Native.SetForegroundWindow(Handle);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !released)
        {
            released = true;
            routeTimer.Stop(); routeTimer.Dispose();
            if (!preview) try { signInRoute.Restore(); } catch (Exception ex) { Program.ShowError(ex); }
            tray.Visible = false; tray.Dispose(); trayMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}
