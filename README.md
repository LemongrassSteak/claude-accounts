# Claude Accounts

A small, independent Windows desktop app for opening multiple accounts in the **official Claude Desktop application**. Give each account a name, sign in once, and reopen its saved profile from the launcher or system tray.

**Early preview, v0.1.0.** Not affiliated with Anthropic. Claude is a trademark of Anthropic. The MIT license covers this launcher and its original artwork, not Claude Desktop or any Anthropic service.

## What it does

- Opens the installed Claude Desktop executable with a separate local data directory for every named account.
- Keeps account profiles across launcher restarts; Claude controls login expiry and sign-in requirements.
- Provides an account list with Add, Open, Rename, and Delete, plus a tray menu.
- Provides profile-specific browser sign-in routing and a manual callback fallback for Google login.
- Detects Microsoft Store and common standalone installations; supports a custom executable location.
- Provides a portable executable and an optional per-user installation with desktop/Start menu shortcuts and a Windows uninstall entry.
- Reuses the original prototype's `%LOCALAPPDATA%\ClaudeAccounts` data, including profile IDs.
- Has no telemetry, token extraction, subscription API calls, or account-password collection.

## Requirements and installation

Windows 11 and the official Claude Desktop app. Choose the release matching your processor: `win-arm64` for Windows on ARM or `win-x64` for Intel/AMD. Releases bundle the .NET runtime; users do not need an SDK or a separate .NET installation.

1. Extract the release ZIP into a folder.
2. Open `ClaudeAccounts.exe` for portable use, or run `Install.cmd` to install for your Windows user.
3. Add a profile, click **Open Claude Desktop**, and sign in in Claude's own window.
4. Repeat for each account. Sign in to one new profile at a time.

### Google opens the wrong Claude window

Select the intended profile and click **Browser sign-in** before choosing Google in Claude. The launcher attempts a temporary per-user `claude://` command pointing directly to the installed Claude executable and that profile. It checks Windows' association lookup before declaring the route active. Routing ends after 10 minutes, when you select **End browser sign-in**, open another profile, or quit the launcher. Original registry values are backed up before changes and restored when the route is still owned by this tool; unrelated changes are preserved. A backup also supports restoration after a crash on the next startup.

If Windows cannot confirm routing, the original handler is restored and a **Complete Google sign-in** dialog appears. Complete Google login in the browser, copy the `claude://login/google-auth?...` link behind its **Open Claude** button, and paste it into that dialog. The link is handed directly to the selected, already-running Claude profile using a separate process argument. It is not saved or logged. Do not paste this sign-in link into chat, GitHub issues, or shared documents. The original profile must still be running because Claude validates the login attempt's state.

If the browser does not expose a copyable link, use Claude's email sign-in. Full Google sign-in completion still requires manual account testing. A local integration check restored the prior handler correctly, but Windows' association lookup did not confirm the temporary route in the development environment; no successful automatic routing claim is made for that environment.

Unsigned preview builds may trigger Windows reputation prompts. Public distributors should sign release binaries with their own trusted code-signing certificate. This repository does not contain a signing key.

## Compatibility limits

This launcher uses the `--user-data-dir` startup argument. It does not modify Claude Desktop or emulate its services. Separate profile startup was observed with Windows Store version **4.23118.0** on Windows ARM64, alongside the existing default instance. This is an observation, not a supported Anthropic integration contract.

- **Signed-in persistence, OAuth callback routing, and simultaneous account use require real account testing.** No end-to-end multi-account authentication test is claimed for this release.
- Windows sign-in links may be routed to the wrong running Claude instance. If that happens, quit the other Claude instances, reopen the intended profile, and retry the login. Do not log out of the other account just to set up a profile.
- Cowork, Claude Code, global shortcuts, protocol handlers, plugins, and other machine-level services may be shared or conflict. Profile separation is **not a security boundary** between Windows users or organizations.
- Claude updates may change profile handling. If accounts stop staying separate, close the affected windows and stop using the launcher until compatibility is checked.
- Existing default Claude Desktop sessions are not imported. Each new launcher profile needs its own first sign-in.
- The UI reports **launched**, not **signed in** or **connected**. The launcher does not inspect account identity or authentication state.

## Your data

All mutable data lives outside the install folder:

```text
%LOCALAPPDATA%\ClaudeAccounts\
  accounts.json          # labels, IDs, last launch time
  accounts.json.bak      # previous successful save
  settings.json          # custom Claude location and tray preference
  sign-in-route.json     # temporary original link-handler backup, while routing is active
  Profiles\<random-id>\  # managed exclusively by Claude Desktop
```

Profile folders contain sensitive signed-in sessions. Do not upload them to GitHub, issue reports, or shared storage. The source and release archives do not include account data. `.gitignore` excludes common profile and configuration paths as a second precaution.

Delete removes an account from the launcher after confirmation. Its profile files remain on disk; it does not log out of or delete the Claude account. Re-adding the same name creates a fresh profile. Previously archived accounts from the earlier preview appear in the normal list. Uninstall preserves account data. To remove sessions permanently, first quit the associated Claude windows, then manage the local data yourself.

Writes are atomic and retain the prior configuration. If a primary file is damaged, the app can restore its previous save while retaining the damaged file. With no valid copy, it fails safely without overwriting the configuration or touching profiles. Keep your own private backups if this data matters to you.

## Build from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows. No third-party NuGet packages are required. Run from the repository root:

```powershell
dotnet run --project tests/ClaudeAccounts.Tests -c Release
dotnet run --project src/ClaudeAccounts.App
powershell -NoProfile -File scripts/build.ps1 -Runtime win-arm64
# Or: -Runtime win-x64
```

The build script tests, publishes a self-contained executable, copies install helpers and license text, and creates a ZIP in `artifacts/`. To build the other architecture, rerun it with that runtime. Runtime packs are restored from Microsoft's standard .NET/NuGet endpoints during the first build. Single-file .NET apps extract native runtime files to the user's temporary directory at runtime.

`scripts/New-Icon.ps1` regenerates the original geometric app icon using System.Drawing. It contains no Anthropic logo. The committed icon is sufficient for ordinary builds.

For a screenshot with sample data and no real account access:

```powershell
.\artifacts\win-arm64\ClaudeAccounts.exe --screenshot "$PWD\docs\screenshot.png"
```

`--demo` opens an interactive UI with temporary sample profiles and disables browser sign-in controls. For developers, `--check-routing <report-file>` temporarily writes and restores a per-user Claude protocol command and reports the effective association; run it only when no sign-in is in progress. It does not initiate authentication. `--restore-sign-in-routing` restores an outstanding backup without opening the manager.

## Project layout

```text
src/ClaudeAccounts.Core/    Models, validated persistence, launch arguments
src/ClaudeAccounts.App/     Windows Forms UI, discovery, tray integration
tests/ClaudeAccounts.Tests/ Dependency-free regression runner
scripts/                   Build, package, install, uninstall, icon generation
.github/workflows/         Windows CI and manual/tagged release artifacts
```

## Open-source publishing

This folder is ready to be a Git repository. Review the MIT license and contributor documents, then create your repository and push the source. Do not push the `artifacts/` folder or any local profile data. The included GitHub workflow runs checks and uploads downloadable ARM64/x64 release artifacts on a `v*` tag or a manual workflow run. It does **not** publish a public GitHub Release automatically. Review and sign binaries before attaching them to a public release.

See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
