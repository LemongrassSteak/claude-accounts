using System.Runtime.InteropServices;
using System.Text;
using ClaudeAccounts.Core;
using Microsoft.Win32;

namespace ClaudeAccounts.App;

internal sealed class WindowsProtocolRegistry : IProtocolRegistry
{
    private const string Root = @"Software\Classes\claude";
    public ProtocolRegistration Read()
    {
        using var root = Registry.CurrentUser.OpenSubKey(Root);
        using var command = Registry.CurrentUser.OpenSubKey(Root + @"\shell\open\command");
        static string? Value(RegistryKey? key, string name)
        {
            object? value = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (value != null && value is not string) throw new InvalidOperationException("The existing Claude link handler uses an unsupported registry value type.");
            return value as string;
        }
        string? previous = Value(command, "");
        return new(Value(root, ""), Value(root, "URL Protocol"), previous, previous == null ? 1 : (int)command!.GetValueKind(""), root != null);
    }

    public void Write(ProtocolRegistration value)
    {
        using (var command = Registry.CurrentUser.CreateSubKey(Root + @"\shell\open\command"))
            Set(command, "", value.Command, value.CommandKind);
        using (var root = Registry.CurrentUser.CreateSubKey(Root))
        {
            Set(root, "", value.Title); Set(root, "URL Protocol", value.ProtocolMarker);
        }
        if (value.Command == null)
        {
            RemoveIfEmpty(Root + @"\shell\open\command"); RemoveIfEmpty(Root + @"\shell\open"); RemoveIfEmpty(Root + @"\shell");
        }
        if (!value.RootExisted) RemoveIfEmpty(Root);
        SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    private static void Set(RegistryKey key, string name, string? value, int kind = 1)
    {
        if (value == null) key.DeleteValue(name, false);
        else key.SetValue(name, value, (RegistryValueKind)kind);
    }
    private static void RemoveIfEmpty(string path)
    {
        bool empty;
        using (var key = Registry.CurrentUser.OpenSubKey(path)) empty = key != null && key.ValueCount == 0 && key.SubKeyCount == 0;
        if (empty) Registry.CurrentUser.DeleteSubKey(path, false);
    }

    internal static bool EffectiveCommandContains(string profile)
    {
        return EffectiveCommand().Contains("--user-data-dir=" + profile, StringComparison.OrdinalIgnoreCase);
    }
    internal static string EffectiveCommand()
    {
        uint length = 32768; var text = new StringBuilder((int)length);
        int result = AssocQueryString(0x1000, 1, "claude", "open", text, ref length);
        return result == 0 ? text.ToString() : "Association query HRESULT: " + result.ToString("X8");
    }
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryString(uint flags, uint query, string association, string extra, StringBuilder output, ref uint length);
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint change, uint flags, IntPtr item1, IntPtr item2);
}
