using System.Diagnostics;
using System.Runtime.InteropServices;
using ClaudeAccounts.Core;

namespace ClaudeAccounts.App;

internal static class Program
{
    internal static string DataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudeAccounts");

    [STAThread]
    private static int Main(string[] args)
    {
        Native.FreeConsole();
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowError(e.ExceptionObject as Exception ?? new Exception("Unexpected error."));
        try
        {
            // Screenshot mode never reads or writes real account data.
            if (args.Length == 2 && args[0] is "--screenshot" or "--screenshot-settings") return Screenshot(args[1], args[0] == "--screenshot-settings");
            if (args.Length == 1 && args[0] == "--demo") return Demo();
            if (args.Length == 2 && args[0] == "--check-routing") return CheckRouting(args[1]);
            if (args.Length == 1 && args[0] == "--restore-sign-in-routing")
            { new SignInRoute(new WindowsProtocolRegistry(), Path.Combine(DataRoot, "sign-in-route.json")).Restore(); return 0; }
            using var mutex = new Mutex(true, "Local\\ClaudeAccountsLauncher", out bool first);
            using var activate = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\ClaudeAccounts.Activate");
            if (!first) { activate.Set(); return 0; }
            var store = new AccountStore(DataRoot);
            using var form = new MainForm(store);
            using var cancel = new CancellationTokenSource();
            Task listener = Task.Run(() =>
            {
                while (WaitHandle.WaitAny([activate, cancel.Token.WaitHandle]) == 0)
                    if (form.IsHandleCreated && !form.IsDisposed)
                        try { form.BeginInvoke(form.RestoreWindow); } catch (InvalidOperationException) { }
            });
            Application.Run(form);
            cancel.Cancel();
            listener.Wait(TimeSpan.FromSeconds(2));
            return 0;
        }
        catch (Exception ex) { ShowError(ex); return 1; }
    }

    private static int Screenshot(string destination, bool settings)
    {
        string root = Path.Combine(Path.GetTempPath(), "ClaudeAccounts-preview-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new AccountStore(root);
            store.Add("Personal");
            store.Add("Work"); store.Add("Research");
            using Form form = settings ? new SettingsForm(store, null) : new MainForm(store, preview: true);
            form.Show(); Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.GetFullPath(destination));
            form.Close(); return 0;
        }
        finally { Directory.Delete(root, true); }
    }

    private static int Demo()
    {
        string root = Path.Combine(Path.GetTempPath(), "ClaudeAccounts-demo-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new AccountStore(root); store.Add("Personal"); store.Add("Work");
            using var form = new MainForm(store, preview: true); Application.Run(form); return 0;
        }
        finally { Directory.Delete(root, true); }
    }

    private static int CheckRouting(string report)
    {
        string root = Path.Combine(Path.GetTempPath(), "ClaudeAccounts-routing-test-" + Guid.NewGuid().ToString("N"));
        var registry = new WindowsProtocolRegistry();
        ProtocolRegistration before = registry.Read();
        var route = new SignInRoute(registry, Path.Combine(root, "recovery.json"));
        bool effective = false, restored = false; string command = "";
        try
        {
            var installed = ClaudeLocator.FindAsync(null).GetAwaiter().GetResult() ?? throw new FileNotFoundException("Claude Desktop not detected.");
            route.Start(installed.Path, root);
            Thread.Sleep(800);
            command = WindowsProtocolRegistry.EffectiveCommand();
            effective = WindowsProtocolRegistry.EffectiveCommandContains(root);
        }
        finally
        {
            route.Restore(); restored = registry.Read() == before;
            File.WriteAllText(Path.GetFullPath(report), "Effective profile routing: " + effective + Environment.NewLine + "Original handler restored: " + restored + Environment.NewLine + command);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        return effective && restored ? 0 : 1;
    }

    internal static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "Claude Accounts", MessageBoxButtons.OK, MessageBoxIcon.Error);
    internal static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, ArgumentList = { path } });
    }

    internal static class Native
    {
        [DllImport("kernel32.dll")] internal static extern bool FreeConsole();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr handle);
    }
}
