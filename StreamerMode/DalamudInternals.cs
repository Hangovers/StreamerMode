namespace StreamerMode;

internal static class DalamudInternals
{
    private static object? _interfaceManager;
    private static PropertyInfo? _isDispatchingEvents;
    private static bool _initialized;

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            var dalamudAssembly = typeof(IPluginLog).Assembly;

            var interfaceManagerType = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
            var serviceOpenGeneric = dalamudAssembly.GetType("Dalamud.Service`1");

            if (interfaceManagerType == null || serviceOpenGeneric == null)
            {
                Service.PluginLog?.Error("Streamer Mode: could not locate internal Dalamud types. Incompatible Dalamud version?");
                return;
            }

            var serviceClosed = serviceOpenGeneric.MakeGenericType(interfaceManagerType);
            var getMethod = serviceClosed.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (getMethod == null)
            {
                Service.PluginLog?.Error("Streamer Mode: could not locate Service<InterfaceManager>.Get().");
                return;
            }

            _interfaceManager = getMethod.Invoke(null, null);
            _isDispatchingEvents = interfaceManagerType.GetProperty("IsDispatchingEvents", BindingFlags.Public | BindingFlags.Instance);

            if (_isDispatchingEvents == null)
            {
                Service.PluginLog?.Error("Streamer Mode: IsDispatchingEvents property not found. Incompatible Dalamud version?");
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog?.Error(ex, "Streamer Mode: failed to initialize reflection bridge.");
        }
    }

    public static bool SetDispatchingEvents(bool value)
    {
        EnsureInitialized();

        if (_isDispatchingEvents == null || _interfaceManager == null)
        {
            Service.PluginLog?.Error("Streamer Mode: reflection bridge is not available.");
            return false;
        }

        try
        {
            _isDispatchingEvents.SetValue(_interfaceManager, value);
            return true;
        }
        catch (Exception ex)
        {
            Service.PluginLog?.Error(ex, "Streamer Mode: failed to set IsDispatchingEvents.");
            return false;
        }
    }
}
