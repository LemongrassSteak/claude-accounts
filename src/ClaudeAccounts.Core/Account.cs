namespace ClaudeAccounts.Core;

public sealed class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    // Keep this string to read the original launcher's locally formatted timestamps.
    public string? LastOpened { get; set; }
    public string Color { get; set; } = "#BF6548";
}

public sealed class AppSettings
{
    public string? ClaudeExecutable { get; set; }
    public bool MinimizeToTray { get; set; }
}
