using System.Diagnostics;
using ClaudeAccounts.Core;

namespace ClaudeAccounts.App;

internal static class CallbackDialog
{
    internal static bool Complete(IWin32Window owner, string name, string executable, string profile)
    {
        using var dialog = new Form { Text = "Complete Google sign-in · " + name, ClientSize = new Size(600, 300), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Theme.Background, Font = new Font("Segoe UI", 10) };
        var label = Theme.Label("Windows did not confirm automatic link routing.\n\nChoose Sign in with Google in the “" + name + "” Claude window. After signing in, right-click the browser’s Open Claude button and copy its link. Paste that link below.", 10);
        label.AutoSize = false; label.Location = new Point(24, 22); label.Size = new Size(552, 130); dialog.Controls.Add(label);
        var input = new TextBox { Location = new Point(24, 158), Width = 552, MaxLength = 16384, UseSystemPasswordChar = true, AccessibleName = "Claude Google sign-in callback link" }; dialog.Controls.Add(input);
        var note = Theme.Label("Sent directly to the selected Claude profile. Never saved or logged.", 9); note.ForeColor = Theme.Muted; note.Location = new Point(24, 198); dialog.Controls.Add(note);
        var send = Theme.Button("Send to Claude", () =>
        {
            var start = ClaudeLaunch.CreateGoogleCallbackStartInfo(executable, profile, input.Text.Trim());
            using var process = Process.Start(start) ?? throw new IOException("Claude could not receive the sign-in link.");
            input.Clear(); dialog.DialogResult = DialogResult.OK;
        }, true); send.Location = new Point(430, 240); dialog.Controls.Add(send);
        var cancel = Theme.Button("Cancel", () => dialog.DialogResult = DialogResult.Cancel); cancel.Location = new Point(307, 240); dialog.Controls.Add(cancel);
        dialog.AcceptButton = send; dialog.CancelButton = cancel;
        bool sent = dialog.ShowDialog(owner) == DialogResult.OK;
        input.Clear(); return sent;
    }
}
