namespace ClaudeAccounts.App;

internal static class NameDialog
{
    internal static string? Ask(IWin32Window owner, string title, string value)
    {
        using var dialog = new Form { Text = title, ClientSize = new Size(440, 175), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Theme.Background, Font = new Font("Segoe UI", 10) };
        var label = Theme.Label("Account name"); label.Location = new Point(22, 20); dialog.Controls.Add(label);
        var input = new TextBox { Text = value, Location = new Point(22, 50), Width = 394, MaxLength = 70, AccessibleName = "Account name" }; dialog.Controls.Add(input);
        var ok = Theme.Button("Save", () => { if (!string.IsNullOrWhiteSpace(input.Text)) dialog.DialogResult = DialogResult.OK; }, true); ok.Location = new Point(306, 110); dialog.Controls.Add(ok);
        var cancel = Theme.Button("Cancel", () => dialog.DialogResult = DialogResult.Cancel); cancel.Location = new Point(186, 110); dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog(owner) == DialogResult.OK ? input.Text.Trim() : null;
    }
}
