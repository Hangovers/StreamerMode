using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace StreamerMode;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/streamer";

    private bool _enabled;

    public string Name => "Streamer Mode";

    public Plugin()
    {
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle streamer mode. Usage: /streamer [on|off|status]"
        });

        Log.Information("Streamer Mode loaded. Type /streamer to toggle.");
    }

    public void Dispose()
    {
        // Never leave Dalamud's UI hidden if this plugin is unloaded.
        DalamudInternals.SetDispatchingEvents(true);
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "":
            case "toggle":
                SetEnabled(!_enabled);
                break;

            case "on":
            case "enable":
                SetEnabled(true);
                break;

            case "off":
            case "disable":
                SetEnabled(false);
                break;

            case "status":
                Log.Information("Streamer Mode is currently {0}.", _enabled ? "ENABLED" : "disabled");
                break;

            default:
                Log.Error("Unknown argument '{0}'. Usage: /streamer [on|off|status]", args);
                break;
        }
    }

    private void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
        {
            Log.Information("Streamer Mode is already {0}.", enabled ? "enabled" : "disabled");
            return;
        }

        // Streamer mode ON  -> stop dispatching ImGui draw events (hide everything)
        // Streamer mode OFF -> resume dispatching (restore everything)
        if (!DalamudInternals.SetDispatchingEvents(!enabled))
        {
            Log.Error("Failed to toggle Streamer Mode (incompatible Dalamud version?). State unchanged.");
            return;
        }

        _enabled = enabled;
        Log.Information("Streamer Mode {0}.", enabled
            ? "ENABLED — all plugin and Dalamud UI hidden"
            : "disabled — UI restored");
    }
}
