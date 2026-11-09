# Preview validation

Validated on Windows ARM64 during development:

- .NET 10 Release build: zero warnings and errors.
- All 26 regression tests passed after the final simplification, including deletion, profile retention, configuration recovery, legacy data, reversible routing, and callback validation. Release compilation completed with zero warnings or errors.
- Main window and Settings were rendered and visually checked before the final simplification. The final layout removes the header, favorites, search and archive controls; its footer contains only the affiliation notice.
- Interactive UI verification of the final simplified layout was stopped by the user. The running app was left unchanged; reopen it to load the rebuilt version.
- Official Claude Desktop Store 4.23118.0 started with a separate profile beside the default instance.
- Protocol routing diagnostic restored the exact original registry registration. Effective Windows association lookup returned no confirmed route in this environment, so the app offers the manual callback fallback.
- ARM64 and x64 self-contained preview builds compiled. These are unsigned. Attempts to start the packaged executable returned Windows access-denied errors from both the process launcher and desktop shell in this environment. The reason has not been conclusively diagnosed. A trusted signature is recommended for public distribution; it is not claimed to resolve every policy restriction.

Not verified: completed Google authentication, full multi-account identity/persistence across Claude restarts, x64 execution on native x64 hardware, shared Cowork/Code services, or end-to-end installer/uninstaller behavior. No automated tests used real credentials or profile content.

This is an early preview, not a certified compatibility release. See ARCHITECTURE.md for manual release checks.
