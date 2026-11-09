# Security and privacy

This app launches an existing executable. It does not authenticate to Anthropic, decrypt sessions, proxy messages, collect telemetry, or issue API requests. Claude Desktop itself accesses the network normally.

Browser sign-in can temporarily modify the current user's `claude://` protocol command to include a selected profile. It preserves a recovery record of the previous values and restores only a handler that it still owns. It does not modify Windows' protected UserChoice default-app selection. If Windows does not confirm the route, the registry change is undone. The optional manual Google callback field accepts only `claude://login/google-auth` links with an authorization code, forwards the opaque link to the selected Claude process, and clears the field. No callback is persisted, decoded for credentials, or logged. The callback is briefly present in process arguments, as with normal desktop protocol activation, and is sensitive. Never include it in support reports.

Account names are labels supplied by users; they are not verified identities. Separate profile directories do not isolate machine-level services or establish a security boundary. Anyone with access to the same Windows account may access profile data. A custom executable path is a trusted local setting: choose only your installed Claude Desktop executable.

Please do not post tokens, profile files, personal information, or sensitive vulnerability details in public issues. Repository maintainers should enable GitHub private vulnerability reporting before publishing and provide a maintained private contact channel. Until a private channel is available, do not submit secrets to a public tracker.

Unsigned binaries, dependencies, and build artifacts must be reviewed by distributors. No signing key or public repository has been provisioned by this project scaffold.
