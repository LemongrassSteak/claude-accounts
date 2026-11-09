# Contributing

Use a Windows machine and the .NET 10 SDK. Run the regression runner and build the app before submitting a pull request. Keep changes focused, explain the user-visible behavior, and include relevant validation.

Profile safety comes first: do not read or copy authentication tokens, merge profile contents, silently delete account data, or claim account identity based on a profile label. Use temporary directories for tests. Never commit real account labels, user paths, session data, screenshots of private content, or signing credentials.

Compatibility reports should include Windows architecture, Claude Desktop version, Store/standalone installation type, and steps to reproduce. Redact emails, conversation content, and user paths. Do not attach profile folders or account JSON.

The current test runner covers persistence, recovery, path validation, legacy data, deletion with profile retention, settings, protocol routing, and shell-free launch arguments. Real Claude sign-in and machine-level service behavior require manual verification with accounts you are authorized to use.

Contributions are provided under the repository's MIT license. Keep discussion respectful and specific.
