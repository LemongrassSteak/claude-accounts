using System.Diagnostics;
using System.Text.Json;

namespace ClaudeAccounts.App;

internal sealed record ClaudeInstallation(string Path, string Source)
{
    public string Version
    {
        get { try { return FileVersionInfo.GetVersionInfo(Path).ProductVersion ?? "unknown"; } catch { return "unknown"; } }
    }
}

internal static class ClaudeLocator
{
    public static async Task<ClaudeInstallation?> FindAsync(string? customPath)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
            return File.Exists(customPath) ? new(customPath, "Custom location") : null;
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] paths = [Path.Combine(local, "AnthropicClaude", "claude.exe"), Path.Combine(local, "Programs", "Claude", "Claude.exe")];
        foreach (string path in paths) if (File.Exists(path)) return new(path, "Desktop installation");
        // Query the signed Windows app inventory, rather than guessing a versioned WindowsApps path.
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
        var info = new ProcessStartInfo(powershell) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-Command");
        info.ArgumentList.Add("Get-AppxPackage -Name '*Claude*' | Where-Object { $_.Name -match '^Anthropic' } | Select-Object -ExpandProperty InstallLocation | ConvertTo-Json -Compress");
        try
        {
            using var process = Process.Start(info);
            if (process == null) return null;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); return null; }
            string json = (await output).Trim(); await error;
            if (process.ExitCode != 0 || json.Length == 0) return null;
            using var doc = JsonDocument.Parse(json);
            IEnumerable<string?> folders = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().Select(e => e.GetString()).ToArray()
                : [doc.RootElement.GetString()];
            foreach (string? folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                string executable = Path.Combine(folder, "app", "claude.exe");
                if (File.Exists(executable)) return new(executable, "Microsoft Store");
            }
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or JsonException or InvalidOperationException) { }
        return null;
    }
}
