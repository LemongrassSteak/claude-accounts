namespace ClaudeAccounts.App;

internal static class Theme
{
    internal static readonly Color Background = Color.FromArgb(246, 244, 239);
    internal static readonly Color Ink = Color.FromArgb(40, 43, 41);
    internal static readonly Color Muted = Color.FromArgb(107, 111, 106);
    internal static readonly Color Accent = Color.FromArgb(166, 78, 50);
    internal static readonly Color Border = Color.FromArgb(224, 224, 214);
    internal static readonly Color Selected = Color.FromArgb(242, 228, 216);

    internal static Label Label(string text, float size = 10, bool bold = false) => new()
    {
        Text = text, AutoSize = true, ForeColor = Ink, BackColor = Color.Transparent,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 8)
    };

    internal static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(110, 40), Padding = new Padding(14, 4, 14, 4),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : Ink, Margin = new Padding(0, 0, 10, 8), UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.Click += (_, _) => { try { action(); } catch (Exception ex) { Program.ShowError(ex); } };
        return button;
    }
}
