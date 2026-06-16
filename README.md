## ProxySync

ProxySync manages proxy configuration for the current user.

### Commands

- `proxysync set` - create or update the legacy `config.json` configuration.
- `proxysync status` - show the active profile and legacy configuration summary.
- `proxysync sync` - applies the active profile when one exists, otherwise falls back to the legacy `config.json` workflow.
- `proxysync disable` - clears proxy settings.
- `proxysync on` - enables proxy settings from the active profile.
- `proxysync off` - disables proxy settings without deleting profiles.
- `proxysync profile add <name>` - create a named profile.
- `proxysync profile list` - show all profiles and mark the active profile.
- `proxysync profile show [name]` - show the selected profile, or the active profile when no name is provided.
- `proxysync profile switch <name>` - make a profile active.
- `proxysync detect` - detect the current network and suggest a matching profile.
- `proxysync auto-switch` - detect the current network, switch profiles automatically, and synchronize proxy settings.

### Sync behavior

1. If an active profile exists, `sync` uses that profile.
2. If no active profile exists, `sync` uses the legacy `config.json` configuration.
3. When the legacy configuration is used, ProxySync prints a console message so the behavior is visible to the user.

### Automation behavior

1. `on` requires an active profile and applies proxy settings from that profile.
2. `off` disables proxy settings and preserves the active profile selection.
3. `detect` reads the current network signature and suggests a matching profile if one exists.
4. `auto-switch` detects a matching profile, switches it active, and runs the normal sync workflow.

### Profile storage

Profiles are stored in `%USERPROFILE%\.proxysync\profiles.json`.

Each profile may optionally include a `networkIdentifier` value such as a gateway prefix or subnet prefix. The detection logic uses this identifier to find network matches.

Use `proxysync profile show [name]` to inspect a profile from the CLI. Use `proxysync status` to see the active profile and whether a legacy `config.json` exists.

If `profiles.json` is corrupted, ProxySync backs it up to a timestamped file such as:

`profiles.json.corrupt.20260529-103015.bak`

and creates a fresh profile store.

### Backward compatibility

Existing users can continue using `set` followed by `sync`. If no active profile is configured, `sync` falls back to the legacy `config.json` workflow and prints a message indicating that legacy configuration is in use.

