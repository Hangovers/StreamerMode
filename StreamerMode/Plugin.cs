namespace StreamerMode;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Commands _commands;
    private readonly DtrHider _dtrHider = new();

    private bool _enabled;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        _pluginInterface.Create<Service>();

        _commands = new Commands(this);
        Service.Framework.Update += OnUpdate;
        _pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        _pluginInterface.UiBuilder.OpenMainUi += OnOpenMainUi;

        Service.PluginLog.Information("Streamer Mode loaded. Type /streamer to toggle.");
    }

    public string Name => "StreamerMode";

    public bool IsEnabled => _enabled;

    public void Dispose()
    {
        _commands.Dispose();
        Service.Framework.Update -= OnUpdate;
        _pluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        _pluginInterface.UiBuilder.OpenMainUi -= OnOpenMainUi;
        _dtrHider.RestoreAll();
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

        if (enabled)
            _dtrHider.HideAll();
        else
            _dtrHider.RestoreAll();

        _enabled = enabled;
        Service.PluginLog.Information("Streamer Mode {0}.", enabled
            ? "ENABLED — all plugin and Dalamud UI hidden"
            : "disabled — UI restored");
    }

    private void OnUpdate(IFramework _)
    {
        if (_enabled) _dtrHider.EnforceHidden();
    }

    private void OnOpenConfigUi()
        => Service.ChatGui.Print("Streamer Mode has no settings window — use /streamer on|off|status.");

    private void OnOpenMainUi()
        => Service.ChatGui.Print("Streamer Mode has no main window — use /streamer on|off|status.");
}
