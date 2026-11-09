using System.Diagnostics;

namespace ClaudeAccounts.Core;

public static class ClaudeLaunch
{
    public static ProcessStartInfo CreateStartInfo(string executable, string profile)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable))
            throw new FileNotFoundException("Select the installed Claude Desktop executable in Settings.", executable);
        if (!Path.IsPathFullyQualified(profile)) throw new ArgumentException("The profile path must be absolute.");
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(executable)! };
        // ArgumentList handles spaces and quoting. Never invoke a shell with user input.
        info.ArgumentList.Add("--user-data-dir=" + profile);
        return info;
    }

    public static ProcessStartInfo CreateGoogleCallbackStartInfo(string executable, string profile, string callback)
    {
        if (callback.Length > 16384 || callback.Any(char.IsControl) || callback.Contains('"') ||
            !Uri.TryCreate(callback, UriKind.Absolute, out var uri) || uri.Scheme != "claude" ||
            uri.Host != "login" || uri.AbsolutePath != "/google-auth" || uri.UserInfo.Length != 0 ||
            uri.Port != -1 || uri.Fragment.Length != 0 ||
            !uri.Query.TrimStart('?').Split('&').Any(p => p.StartsWith("code=", StringComparison.Ordinal) && p.Length > 5))
            throw new ArgumentException("Paste the claude://login/google-auth link behind the browser’s Open Claude button, not the browser address or a password.");
        var info = CreateStartInfo(executable, profile);
        info.ArgumentList.Add(callback);
        return info;
    }
}
