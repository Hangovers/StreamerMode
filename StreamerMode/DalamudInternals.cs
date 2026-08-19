namespace StreamerMode;

internal static class DalamudInternals
{
    private static object? _interfaceManager;
    private static PropertyInfo? _isDispatchingEvents;
    private static FieldInfo? _lastWantCapture;
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

            // Fixes alt-tab blocked-click SE: InterfaceManager.lastWantCapture is a private bool
            // set in Display() before the IsDispatchingEvents check. When we hide, it stays true
            // for one frame, causing SetCursorDetour/ProcessWndProcW to swallow the stray mouse
            // Wine/KDE synthesizes on focus restore, which FFXIV plays as blocked-click. Clear it
            // immediately when toggling so input falls through right away.
            _lastWantCapture = interfaceManagerType.GetField("lastWantCapture", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_lastWantCapture == null)
            {
                Service.PluginLog?.Warning("Streamer Mode: lastWantCapture field not found — alt-tab SE fix disabled.");
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
            if (!value)
            {
                try
                {
                    _lastWantCapture?.SetValue(_interfaceManager, false);
                }
                catch (Exception ex2)
                {
                    Service.PluginLog?.Warning(ex2, "Streamer Mode: failed to clear lastWantCapture (non-fatal).");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Service.PluginLog?.Error(ex, "Streamer Mode: failed to set IsDispatchingEvents.");
            return false;
        }
    }
}
