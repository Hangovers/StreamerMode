namespace StreamerMode;

public sealed class Commands : IDisposable
{
    private const string CommandName = "/streamer";

    private readonly Plugin plugin;

    public Commands(Plugin plugin)
    {
        this.plugin = plugin;

        Service.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle streamer mode. Usage: /streamer [on|off|status]"
        });
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "":
            case "toggle":
                plugin.SetEnabled(!plugin.IsEnabled);
                break;

            case "on":
            case "enable":
                plugin.SetEnabled(true);
                break;

            case "off":
            case "disable":
                plugin.SetEnabled(false);
                break;

            case "status":
                Service.PluginLog.Information("Streamer Mode is currently {0}.", plugin.IsEnabled ? "ENABLED" : "disabled");
                break;

            default:
                Service.PluginLog.Error("Unknown argument '{0}'. Usage: /streamer [on|off|status]", args);
                break;
        }
    }

    public void Dispose()
    {
        Service.Commands.RemoveHandler(CommandName);
    }
}
