using System.Text.Json;

namespace ClaudeAccounts.Core;

/// <summary>Stores labels only. Claude itself owns all authentication and profile contents.</summary>
public sealed class AccountStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string Root { get; }
    public List<Account> Accounts { get; private set; }
    public AppSettings Settings { get; private set; }
    public string? RecoveryNotice { get; private set; }
    private string AccountsPath => Path.Combine(Root, "accounts.json");
    private string SettingsPath => Path.Combine(Root, "settings.json");

    public AccountStore(string root)
    {
        Root = Path.GetFullPath(root);
        Directory.CreateDirectory(Root);
        Accounts = ReadWithRecovery(AccountsPath, () => new List<Account>(), ValidateAccounts);
        Settings = ReadWithRecovery(SettingsPath, () => new AppSettings(), s => s != null);
    }

    private T ReadWithRecovery<T>(string path, Func<T> empty, Func<T, bool> valid)
    {
        // A valid backup is also used if the primary file is missing after a crash.
        if (!File.Exists(path) && !File.Exists(path + ".bak")) return empty();
        try { return Read(path, valid); }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            try
            {
                T recovered = Read(path + ".bak", valid);
                if (File.Exists(path)) File.Copy(path, path + ".damaged-" + Guid.NewGuid().ToString("N"));
                // Copy, rather than Save, to avoid replacing the known-good backup with bad data.
                File.Copy(path + ".bak", path, true);
                RecoveryNotice = "Recovered the account list or settings from its previous save. Profile folders were preserved.";
                return recovered;
            }
            catch (Exception backupError) when (backupError is JsonException or IOException or InvalidDataException)
            {
                throw new InvalidDataException("Cannot read " + Path.GetFileName(path) + ". Your profiles are preserved at " + Root + ". Restore a valid backup before continuing.", backupError);
            }
        }
    }

    private static T Read<T>(string path, Func<T, bool> valid)
    {
        T? value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json);
        if (value == null || !valid(value)) throw new InvalidDataException("Invalid configuration.");
        return value;
    }

    private static bool ValidateAccounts(List<Account> accounts) => accounts != null &&
        accounts.All(a => a != null && IsValidId(a.Id) && !string.IsNullOrWhiteSpace(a.Name) && a.Name.Length <= 70) &&
        accounts.Select(a => a.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == accounts.Count;

    public static bool IsValidId(string? id) => Guid.TryParseExact(id, "N", out _);

    public string ProfilePath(Account account)
    {
        if (!IsValidId(account.Id)) throw new InvalidDataException("Invalid profile identifier.");
        return Path.Combine(Root, "Profiles", account.Id);
    }

    private string ValidateName(string input, string? exceptId = null)
    {
        string name = input.Trim();
        if (name.Length is < 1 or > 70 || name.Any(char.IsControl)) throw new ArgumentException("Use a name between 1 and 70 characters without control characters.");
        if (Accounts.Any(a => a.Id != exceptId && string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("There is already an account with that name.");
        return name;
    }

    public Account Add(string name)
    {
        var account = new Account { Name = ValidateName(name), Color = Palette[Accounts.Count % Palette.Length] };
        Accounts.Add(account);
        try { SaveAccounts(); } catch { Accounts.Remove(account); throw; }
        return account;
    }

    public void Rename(Account account, string name)
    {
        string previous = account.Name;
        account.Name = ValidateName(name, account.Id);
        try { SaveAccounts(); } catch { account.Name = previous; throw; }
    }

    public void Delete(Account account)
    {
        int index = Accounts.FindIndex(a => a.Id == account.Id);
        if (index < 0) throw new InvalidOperationException("Account not found.");
        Account removed = Accounts[index]; Accounts.RemoveAt(index);
        try { SaveAccounts(); } catch { Accounts.Insert(index, removed); throw; }
    }

    public void SaveAccounts()
    {
        if (!ValidateAccounts(Accounts)) throw new InvalidDataException("Invalid account configuration.");
        AtomicWrite(AccountsPath, Accounts);
    }
    public void SaveSettings() => AtomicWrite(SettingsPath, Settings);

    private static void AtomicWrite<T>(string path, T value)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, value, Json);
                file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static readonly string[] Palette = ["#BF6548", "#5C6FA7", "#528479", "#9468A1", "#A17C3B"];
}
