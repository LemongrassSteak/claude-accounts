# Architecture

The UI is a native .NET 10 Windows Forms executable. Release publishing bundles the runtime into a single-file distribution per Windows architecture. PowerShell is used during installation/building and to query Windows' app inventory for Microsoft Store discovery; it does not host the application UI.

The core library owns account metadata and settings. Each profile uses an immutable GUID as its directory name, independent of its label. Validation rejects malformed IDs, duplicate IDs, blank/oversized names, and duplicate new names. Delete removes the label and ID from the launcher, while retaining Claude's profile files. The UI has no profile-wipe action.

Configuration writes serialize to a unique temporary file in the same directory, flush to disk, and replace the primary while keeping the previous save. Startup falls back to a validated backup if the primary is unreadable. Unrecoverable data fails closed. Settings and account metadata are separate files. The manager uses a per-session named mutex to prevent simultaneous manager processes; a named event brings the original window forward on another launch. Launching the old prototype at the same time is unsupported; both use the same mutex to avoid accidental coexistence.

Discovery checks a saved override, common standalone paths, then queries installed Anthropic packages with Windows PowerShell and resolves `app/claude.exe`. Discovery repeats before each launch to handle Store updates. The app passes a single `--user-data-dir=<profile>` argument with `ProcessStartInfo.ArgumentList`. It does not construct shell commands from account names or paths. New profile directories are created immediately before launch. The default Claude profile is never modified by launcher code.

The launcher does not read browser cookies, storage databases, passwords, or tokens. Launch status means a process was started, not that the profile was honored or an account is logged in. Claude decides whether to honor the startup argument, persist login, route protocol callbacks, or share system services. This means compatibility verification is a required release activity, not an inferred guarantee from the unit tests.

Browser sign-in uses a reversible registry adapter behind a testable routing controller. The recovery record is flushed before changing the command. Restoration checks that the handler still matches the applied command; another application's changes are not overwritten. After launching the chosen profile, the UI queries the effective Windows association. If unconfirmed, it restores the handler and offers a manual Google callback dialog. That dialog validates the callback scheme, host, path and size, then passes its opaque contents directly to the installed Claude executable with the chosen data directory. Claude owns state/PKCE validation; the launcher neither bypasses nor implements it. Callbacks are never included in configuration, diagnostics, or error text.

## Manual release checks

1. Build/test both runtime targets; launch on the target architecture.
2. Use disposable test profiles and authorized accounts to verify separate identity and restart persistence.
3. Test email/Google login callbacks with multiple instances; document any routing limits.
4. Check tray open/quit, account selection, Add/Rename/Delete, and second-launch activation.
5. Test common display scaling and keyboard navigation, including Enter to launch and dialogs.
6. Test portable startup, per-user install, upgrade, and uninstall; verify profile files are preserved.
7. Check Claude Code/Cowork only where supported and document any shared-service behavior.
8. Scan release contents for private data, sign the release binaries, and record SHA-256 checksums.
