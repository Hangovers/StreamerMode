# Streamer Mode

Dalamud plugin that hides every plugin and Dalamud UI element with a single chat command — nothing on screen reveals you are running plugins.

## What it does

- Hides **all plugin windows** (every window registered through `UiBuilder.Draw` / the global ImGui `Draw` event).
- Hides **all Dalamud windows** — settings window, plugin installer, `/xllog` log window, console, dev menu, and title-screen badges.
- Hides the **"Dalamud Plugins" / "Dalamud Settings"** entries from the Escape (system) menu.
- Hides **DTR (Server Info Bar)** entries injected by plugins (the top bar, e.g. vnavmesh "AI: Off / Mesh: Ready" — a native `_DTR` addon not covered by the ImGui `IsDispatchingEvents` flag).
- Hides **toast notifications** (`NotificationManager`).
- Controlled **only via chat** — there is no clickable UI toggle, so nothing on screen betrays that plugins are present.
- While active, the per-frame ImGui UI build is skipped entirely, which **reduces CPU usage**.

## Usage

| Command | Effect |
|---|---|
| `/streamer` | Toggle streamer mode on/off |
| `/streamer on` | Enable streamer mode (hide everything) |
| `/streamer off` | Disable streamer mode (restore everything) |
| `/streamer status` | Print whether streamer mode is currently enabled |

`on`/`enable` and `off`/`disable` are aliases. With no argument `/streamer` toggles. Feedback is printed to the Dalamud log (`/xllog`).

## How it works

There is a single choke point: `Dalamud.Interface.Internal.InterfaceManager.IsDispatchingEvents` (`public bool { get; set; } = true`).

- `InterfaceManager.Display()` only fires the global ImGui `Draw` event and calls `NotificationManager.Draw()` when `IsDispatchingEvents` is `true`. Every plugin window and every Dalamud window (`DalamudInterface.OnDraw`) is chained to that event, so setting the flag to `false` hides them all at once and also skips the per-frame ImGui construction (the CPU saving noted above).
- `SystemMenuIntegration.AgentHudOpenSystemMenuDetour` only injects the "Dalamud Plugins" / "Dalamud Settings" entries into the Escape menu when `IsDispatchingEvents` is `true`, so they disappear as well.

`InterfaceManager` is an `internal` type, so the plugin reaches the flag via reflection:

1. `typeof(IPluginLog).Assembly` to get the Dalamud assembly.
2. `GetType("Dalamud.Interface.Internal.InterfaceManager")`.
3. `GetType("Dalamud.Service`1")` → `MakeGenericType(interfaceManagerType)` → `GetMethod("Get", Public | Static, [], null)` → `Invoke(null, null)` to obtain the `InterfaceManager` instance from Dalamud's service locator.
4. `GetProperty("IsDispatchingEvents", Public | Instance)` → `SetValue(instance, value)`.

This single flag therefore governs the global ImGui draw, the notification manager, and the system menu injection.

## Building

```bash
# Requires the .NET 10 SDK (add its folder to PATH if `dotnet` is not already available)
wget -q https://goatcorp.github.io/dalamud-distrib/latest.zip -O /tmp/dalamud.zip
rm -rf /tmp/dalamud && mkdir -p /tmp/dalamud && unzip -q /tmp/dalamud.zip -d /tmp/dalamud
export DALAMUD_HOME=/tmp/dalamud
dotnet build StreamerMode.sln -c Release
```

Output is in `StreamerMode/bin/Release/StreamerMode/` (`StreamerMode.dll` + `StreamerMode.json` manifest).

## Installing (dev)

1. Copy the built `StreamerMode/bin/Release/StreamerMode/` folder to your gaming PC.
2. In-game run `/xldev` → *Experimental* → add the folder under **Dev Plugin Locations**.
3. Reload/re-enable the plugin from the plugin installer (or restart the game).
4. Run `/streamer` in chat to toggle.

## Notes / limitations

- Targets **Dalamud API 15** on **.NET 10** (`Dalamud.NET.Sdk/15.0.0`, `net10.0-windows`).
- Relies on **reflection over an `internal` Dalamud type** (`InterfaceManager` / `Service<T>` / `IsDispatchingEvents`). A future Dalamud update may rename or move these internals and break the plugin — in that case the plugin **logs an error to `/xllog` instead of crashing** and leaves the state unchanged.
- The DTR hiding is **reinforced every frame** while active (via `IFramework.Update`), because some plugins (e.g. vnavmesh) re-set `Shown = true` on every tick.
- `Dispose()` **always restores** `IsDispatchingEvents` to `true`, so the Dalamud UI is never left permanently hidden if the plugin is unloaded or disabled.
- Intended for **personal use**. The official Dalamud plugin repository (D17) would reject reflection over Dalamud internals, so this plugin is not suitable for submission there.

## License

MIT — see [LICENSE](LICENSE).
