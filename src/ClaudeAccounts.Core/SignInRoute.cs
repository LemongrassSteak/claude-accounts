using System.Text.Json;

namespace ClaudeAccounts.Core;

public sealed record ProtocolRegistration(string? Title, string? ProtocolMarker, string? Command, int CommandKind = 1, bool RootExisted = true);
public interface IProtocolRegistry
{
    ProtocolRegistration Read();
    void Write(ProtocolRegistration value);
}

/// <summary>Routes links directly to Claude. No callback URL or authentication value is read.</summary>
public sealed class SignInRoute(IProtocolRegistry registry, string recoveryPath)
{
    private sealed record Recovery(ProtocolRegistration Original, string AppliedCommand);
    private Recovery? active;
    public bool IsActive => active != null;

    public static string Command(string executable, string profile)
    {
        if (!Path.IsPathFullyQualified(executable) || !Path.IsPathFullyQualified(profile) ||
            executable.IndexOfAny(['"', '\0', '\r', '\n']) >= 0 || profile.IndexOfAny(['"', '\0', '\r', '\n']) >= 0)
            throw new ArgumentException("Invalid executable or profile path.");
        return "\"" + executable + "\" \"--user-data-dir=" + profile + "\" \"%1\"";
    }

    public void Start(string executable, string profile)
    {
        string command = Command(executable, profile);
        var current = registry.Read();
        if (active != null && current.Command != active.AppliedCommand)
            throw new InvalidOperationException("Another app changed the Claude link handler. End browser sign-in before trying again.");
        var next = new Recovery(active?.Original ?? current, command);
        // Save original values before changing anything. A crash can be recovered on next startup.
        string temporary = recoveryPath + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(recoveryPath))!);
        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        { JsonSerializer.Serialize(file, next); file.Flush(true); }
        File.Move(temporary, recoveryPath, true);
        active = next;
        try { registry.Write(new("URL:Claude Desktop profile", "", command)); }
        catch { Restore(); throw; }
    }

    public bool Restore()
    {
        var record = active;
        if (record == null && File.Exists(recoveryPath))
        {
            record = JsonSerializer.Deserialize<Recovery>(File.ReadAllText(recoveryPath));
            if (record?.Original == null || string.IsNullOrWhiteSpace(record.AppliedCommand)) throw new InvalidDataException("Invalid sign-in routing backup. The link handler was left unchanged.");
        }
        if (record == null) return true;
        // Never overwrite changes made by Claude, another launcher, or the user after us.
        bool owned = registry.Read().Command == record.AppliedCommand;
        if (owned) registry.Write(record.Original);
        if (File.Exists(recoveryPath)) File.Delete(recoveryPath);
        active = null;
        return owned;
    }
}
