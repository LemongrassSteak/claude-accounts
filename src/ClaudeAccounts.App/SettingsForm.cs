using ClaudeAccounts.Core;

namespace ClaudeAccounts.App;

internal sealed class SettingsForm : Form
{
    internal SettingsForm(AccountStore store, ClaudeInstallation? installation)
    {
        Text = "Settings · Claude Accounts"; ClientSize = new Size(660, 570); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; BackColor = Theme.Background; Font = new Font("Segoe UI", 10);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 9 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(Theme.Label("Settings", 22, true));
        root.Controls.Add(Theme.Label("Claude Desktop executable · leave blank for automatic detection", 9));
        var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 47, ColumnCount = 2, Margin = Padding.Empty };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var path = new TextBox { Text = store.Settings.ClaudeExecutable ?? "", PlaceholderText = "Detect automatically", Dock = DockStyle.Fill, AccessibleName = "Claude executable path", Margin = new Padding(0, 7, 12, 0) };
        var browse = Theme.Button("Browse…", () => { using var pick = new OpenFileDialog { Title = "Select the installed Claude Desktop executable", Filter = "Claude executable (*.exe)|*.exe", CheckFileExists = true }; if (pick.ShowDialog(this) == DialogResult.OK) path.Text = pick.FileName; });
        row.Controls.Add(path, 0, 0); row.Controls.Add(browse, 1, 0); root.Controls.Add(row);
        var found = Theme.Label(installation == null ? "No installation detected." : installation.Source + " · " + installation.Version, 9); found.ForeColor = Theme.Muted; root.Controls.Add(found);
        var tray = new CheckBox { Text = "Keep launcher in the system tray when I close its window", Checked = store.Settings.MinimizeToTray, AutoSize = true, Margin = new Padding(0, 15, 0, 18) }; root.Controls.Add(tray);
        var data = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        data.Controls.Add(Theme.Button("Open data folder", () => Program.OpenFolder(store.Root)));
        data.Controls.Add(Theme.Button("About / compatibility", () => MessageBox.Show(this, "Claude Accounts 0.1.0\nMIT License · Independent community tool\n\nLaunches the official Claude Desktop with a separate --user-data-dir for each profile. No telemetry, password collection, or token extraction.\n\nTested isolated startup with Store version 4.23118.0. Sign-in callback routing and Cowork/Code services may be shared between instances. Updates can change compatibility. This is not a security boundary.\n\nNot affiliated with Anthropic. Claude is a trademark of Anthropic.", "About Claude Accounts", MessageBoxButtons.OK, MessageBoxIcon.Information))); root.Controls.Add(data);
        var note = Theme.Label("Profiles contain signed-in sessions. Deleting an account removes it from the list and preserves its files on disk.", 9);
        note.AutoSize = false; note.Height = 90; note.Dock = DockStyle.Top; note.ForeColor = Theme.Muted; root.Controls.Add(note);
        var spacer = new Panel { Dock = DockStyle.Fill }; root.Controls.Add(spacer);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var save = Theme.Button("Save settings", () =>
        {
            string candidate = path.Text.Trim();
            if (candidate.Length > 0 && (!Path.IsPathFullyQualified(candidate) || !File.Exists(candidate) || !candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Choose an existing executable using its full path, or leave the field blank.");
            var oldPath = store.Settings.ClaudeExecutable; bool oldTray = store.Settings.MinimizeToTray;
            store.Settings.ClaudeExecutable = candidate.Length == 0 ? null : candidate; store.Settings.MinimizeToTray = tray.Checked;
            try { store.SaveSettings(); } catch { store.Settings.ClaudeExecutable = oldPath; store.Settings.MinimizeToTray = oldTray; throw; }
            DialogResult = DialogResult.OK;
        }, true);
        var cancel = Theme.Button("Cancel", () => DialogResult = DialogResult.Cancel); bottom.Controls.Add(save); bottom.Controls.Add(cancel); root.Controls.Add(bottom);
        AcceptButton = save; CancelButton = cancel;
        for (int i = 0; i < 9; i++) root.RowStyles.Add(new RowStyle(i == 7 ? SizeType.Percent : SizeType.AutoSize, i == 7 ? 100 : 0));
    }
}
