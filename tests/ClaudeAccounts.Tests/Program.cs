using System.Text.Json;
using ClaudeAccounts.Core;

// Dependency-free regression runner. Every test uses a fresh temporary folder.
int failed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
void Test(string name, Action<string> action)
{
    string root = Path.Combine(Path.GetTempPath(), "ClaudeAccounts-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try { action(root); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    finally { Directory.Delete(root, true); }
}

Test("New store starts empty", root => Check(new AccountStore(root).Accounts.Count == 0, "Not empty"));
Test("Profiles have distinct stable identifiers", root =>
{
    var s = new AccountStore(root); var a = s.Add("Personal"); var b = s.Add("Work");
    Check(s.ProfilePath(a) != s.ProfilePath(b), "Profiles overlap");
    Check(new AccountStore(root).Accounts[0].Id == a.Id, "ID changed");
});
Test("Trim and Unicode names round-trip", root =>
{
    var s = new AccountStore(root); s.Add("  Équipe 日本語  ");
    Check(new AccountStore(root).Accounts[0].Name == "Équipe 日本語", "Bad name");
});
Test("Invalid and duplicate names are rejected", root =>
{
    var s = new AccountStore(root); s.Add("Work");
    Throws<ArgumentException>(() => s.Add("work")); Throws<ArgumentException>(() => s.Add(" "));
    Throws<ArgumentException>(() => s.Add("a\nb")); Throws<ArgumentException>(() => s.Add(new string('a', 71)));
});
Test("Rename preserves profile path", root =>
{
    var s = new AccountStore(root); var a = s.Add("Personal"); string path = s.ProfilePath(a);
    s.Rename(a, "Home"); Check(new AccountStore(root).Accounts[0].Name == "Home", "Rename lost");
    Check(s.ProfilePath(a) == path, "Profile moved");
});
Test("Delete persists removal from the list", root =>
{
    var s = new AccountStore(root); var a = s.Add("Work"); s.Add("Personal"); s.Delete(a);
    Check(new AccountStore(root).Accounts is [{ Name: "Personal" }], "Account not deleted");
});
Test("Delete retains saved Claude session files", root =>
{
    var s = new AccountStore(root); var a = s.Add("Work"); string path = s.ProfilePath(a);
    Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, "session-sentinel"), "keep");
    s.Delete(a); Check(File.ReadAllText(Path.Combine(path, "session-sentinel")) == "keep", "Session touched");
});
Test("Deleted names can be reused with a fresh profile", root =>
{
    var s = new AccountStore(root); var a = s.Add("Work"); s.Delete(a); var b = s.Add("Work");
    Check(a.Id != b.Id && s.ProfilePath(a) != s.ProfilePath(b), "Deleted profile silently reused");
});
Test("Path traversal and quoted IDs rejected", root =>
{
    var s = new AccountStore(root);
    foreach (string id in new[] { "../escape", "..\\escape", "bad\"path", "", "C:\\elsewhere" })
        Throws<InvalidDataException>(() => s.ProfilePath(new Account { Id = id }));
});
Test("Legacy launcher JSON loads without migration", root =>
{
    string id = Guid.NewGuid().ToString("N");
    File.WriteAllText(Path.Combine(root, "accounts.json"), "[{\"Id\":\"" + id + "\",\"Name\":\"Legacy\",\"LastOpened\":\"11/9/2026 2:00 PM\"}]");
    var s = new AccountStore(root); Check(s.Accounts[0].Id == id && s.Accounts[0].LastOpened != null, "Legacy lost");
});
Test("Corrupt primary recovers previous save", root =>
{
    var s = new AccountStore(root); s.Add("Personal"); s.Add("Work");
    File.WriteAllText(Path.Combine(root, "accounts.json"), "{broken");
    var recovered = new AccountStore(root);
    Check(recovered.Accounts.Count == 1 && recovered.RecoveryNotice != null, "Recovery failed");
    Check(Directory.GetFiles(root, "*.damaged-*").Length == 1, "Damaged file lost");
});
Test("Missing primary recovers backup", root =>
{
    var s = new AccountStore(root); s.Add("Personal"); s.Add("Work"); File.Delete(Path.Combine(root, "accounts.json"));
    Check(new AccountStore(root).Accounts.Count == 1, "Missing primary ignored backup");
});
Test("Unrecoverable config fails without touching profiles", root =>
{
    Directory.CreateDirectory(Path.Combine(root, "Profiles")); File.WriteAllText(Path.Combine(root, "Profiles", "sentinel"), "keep");
    File.WriteAllText(Path.Combine(root, "accounts.json"), "null");
    Throws<InvalidDataException>(() => new AccountStore(root));
    Check(File.Exists(Path.Combine(root, "Profiles", "sentinel")), "Profile lost");
});
Test("Duplicate identifiers are rejected on load", root =>
{
    var a = new Account { Name = "One" }; var b = new Account { Name = "Two", Id = a.Id };
    File.WriteAllText(Path.Combine(root, "accounts.json"), JsonSerializer.Serialize(new[] { a, b }));
    Throws<InvalidDataException>(() => new AccountStore(root));
});
Test("Settings persist independently from account list", root =>
{
    var s = new AccountStore(root); s.Add("Personal"); s.Settings.MinimizeToTray = true; s.Settings.ClaudeExecutable = "C:\\Apps\\Claude.exe"; s.SaveSettings();
    var r = new AccountStore(root); Check(r.Settings.MinimizeToTray && r.Accounts.Count == 1 && r.Settings.ClaudeExecutable == s.Settings.ClaudeExecutable, "Settings lost");
});
Test("Launch arguments preserve spaces without a shell", root =>
{
    string exe = Path.Combine(root, "test executable.exe"); File.WriteAllText(exe, "not executable; never launched by this test");
    string profile = Path.Combine(root, "profile with spaces"); var info = ClaudeLaunch.CreateStartInfo(exe, profile);
    Check(!info.UseShellExecute && info.ArgumentList.Count == 1 && info.ArgumentList[0] == "--user-data-dir=" + profile, "Unsafe arguments");
});
Test("Missing executable and relative profile rejected", root =>
{
    Throws<FileNotFoundException>(() => ClaudeLaunch.CreateStartInfo(Path.Combine(root, "missing.exe"), root));
    string exe = Path.Combine(root, "placeholder.exe"); File.WriteAllText(exe, "test");
    Throws<ArgumentException>(() => ClaudeLaunch.CreateStartInfo(exe, "relative"));
});
Test("Browser sign-in command includes the exact profile", root =>
{
    string exe = Path.Combine(root, "Claude Desktop.exe"); string profile = Path.Combine(root, "Profile One");
    Check(SignInRoute.Command(exe, profile) == "\"" + exe + "\" \"--user-data-dir=" + profile + "\" \"%1\"", "Incorrect protocol command");
});
Test("Protocol registration restores its prior values", root =>
{
    var original = new ProtocolRegistration("Old title", "", "old-command", 2);
    var registry = new FakeRegistry(original); var route = new SignInRoute(registry, Path.Combine(root, "route.json"));
    route.Start(Path.Combine(root, "Claude.exe"), root); Check(registry.Current.Command!.Contains("--user-data-dir="), "Route missing");
    Check(route.Restore() && registry.Current == original && !route.IsActive, "Original not restored");
});
Test("Switching target keeps the original backup", root =>
{
    var original = new ProtocolRegistration(null, null, null, RootExisted: false); var registry = new FakeRegistry(original);
    var route = new SignInRoute(registry, Path.Combine(root, "route.json")); string exe = Path.Combine(root, "Claude.exe");
    route.Start(exe, Path.Combine(root, "A")); route.Start(exe, Path.Combine(root, "B"));
    Check(registry.Current.Command!.Contains(Path.Combine(root, "B")), "Target unchanged");
    route.Restore(); Check(registry.Current == original, "Original replaced by intermediate target");
});
Test("Protocol recovery works after process restart", root =>
{
    var original = new ProtocolRegistration("Claude", "", "old-command"); var registry = new FakeRegistry(original); string path = Path.Combine(root, "route.json");
    new SignInRoute(registry, path).Start(Path.Combine(root, "Claude.exe"), root);
    Check(new SignInRoute(registry, path).Restore() && registry.Current == original && !File.Exists(path), "Recovery failed");
});
Test("Restoration never overwrites another app's handler", root =>
{
    var registry = new FakeRegistry(new("Claude", "", "original")); var route = new SignInRoute(registry, Path.Combine(root, "route.json"));
    route.Start(Path.Combine(root, "Claude.exe"), root); var external = new ProtocolRegistration("Another", "", "changed-elsewhere"); registry.Current = external;
    Check(!route.Restore() && registry.Current == external, "External change overwritten");
});
Test("Malformed routing paths rejected before registry writes", root =>
{
    var original = new ProtocolRegistration(null, null, null); var registry = new FakeRegistry(original); var route = new SignInRoute(registry, Path.Combine(root, "route.json"));
    Throws<ArgumentException>(() => route.Start("relative", root));
    Throws<ArgumentException>(() => route.Start(Path.Combine(root, "bad\"name.exe"), root));
    Check(registry.Current == original && !route.IsActive, "Registry touched");
});
Test("Corrupt routing backup leaves registry untouched", root =>
{
    string path = Path.Combine(root, "route.json"); File.WriteAllText(path, "null");
    var original = new ProtocolRegistration("Claude", "", "original"); var registry = new FakeRegistry(original);
    Throws<InvalidDataException>(() => new SignInRoute(registry, path).Restore()); Check(registry.Current == original, "Registry touched");
});
Test("Google callback forwarded as a separate opaque argument", root =>
{
    string exe = Path.Combine(root, "Claude.exe"); File.WriteAllText(exe, "test");
    const string callback = "claude://login/google-auth?code=synthetic-test-code&state=synthetic-test-state";
    var info = ClaudeLaunch.CreateGoogleCallbackStartInfo(exe, root, callback);
    Check(!info.UseShellExecute && info.ArgumentList.Count == 2 && info.ArgumentList[1] == callback, "Callback altered");
});
Test("Unrelated or malformed callbacks rejected", root =>
{
    string exe = Path.Combine(root, "Claude.exe"); File.WriteAllText(exe, "test");
    foreach (string link in new[] { "https://accounts.google.com/", "claude://evil/google-auth?code=x", "claude://login/google-auth", "claude://login/google-auth?code=x\r\n", "claude://login/google-auth?code=x\" --flag", "claude://login/google-auth?code=x#fragment", "password" })
        Throws<ArgumentException>(() => ClaudeLaunch.CreateGoogleCallbackStartInfo(exe, root, link));
});
Console.WriteLine(failed == 0 ? "All 26 regression tests passed." : failed + " test(s) failed.");
return failed == 0 ? 0 : 1;

sealed class FakeRegistry(ProtocolRegistration value) : IProtocolRegistry
{
    public ProtocolRegistration Current { get; set; } = value;
    public ProtocolRegistration Read() => Current;
    public void Write(ProtocolRegistration registration) => Current = registration;
}
