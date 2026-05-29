## ProxySync

ProxySync manages proxy configuration for the current user.

### Commands

- `proxysync set` - create or update the legacy `config.json` configuration.
- `proxysync sync` - applies the active profile when one exists, otherwise falls back to the legacy `config.json` workflow.
- `proxysync disable` - clears proxy settings.
- `proxysync profile add <name>` - create a named profile.
- `proxysync profile list` - show all profiles and mark the active profile.
- `proxysync profile switch <name>` - make a profile active.

### Sync behavior

1. If an active profile exists, `sync` uses that profile.
2. If no active profile exists, `sync` uses the legacy `config.json` configuration.
3. When the legacy configuration is used, ProxySync prints a console message so the behavior is visible to the user.

### Profile storage

Profiles are stored in `%USERPROFILE%\.proxysync\profiles.json`.

If `profiles.json` is corrupted, ProxySync backs it up to a timestamped file such as:

`profiles.json.corrupt.20260529-103015.bak`

and creates a fresh profile store.

