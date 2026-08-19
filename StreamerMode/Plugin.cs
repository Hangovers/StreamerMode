namespace StreamerMode;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Commands _commands;

    private bool _enabled;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        _pluginInterface.Create<Service>();

        _commands = new Commands(this);

        Service.PluginLog.Information("Streamer Mode loaded. Type /streamer to toggle.");
    }

    public string Name => "StreamerMode";

    public bool IsEnabled => _enabled;

    public void Dispose()
    {
        _commands.Dispose();
        DalamudInternals.SetDispatchingEvents(true);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
        {
            Service.PluginLog.Information("Streamer Mode is already {0}.", enabled ? "enabled" : "disabled");
            return;
        }

        if (!DalamudInternals.SetDispatchingEvents(!enabled))
        {
            Service.PluginLog.Error("Failed to toggle Streamer Mode (incompatible Dalamud version?). State unchanged.");
            return;
        }

        _enabled = enabled;
        Service.PluginLog.Information("Streamer Mode {0}.", enabled
            ? "ENABLED — all plugin and Dalamud UI hidden"
            : "disabled — UI restored");
    }
}
