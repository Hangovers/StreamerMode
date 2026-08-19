using System;
using System.Reflection;
using Dalamud.Plugin.Services;

namespace StreamerMode;

/// <summary>
/// Reflection bridge to Dalamud's internal <c>InterfaceManager.IsDispatchingEvents</c> flag.
///
/// This single flag is the choke point for the entire Dalamud UI:
///   - <c>InterfaceManager.Display()</c> only fires the global ImGui <c>Draw</c> event
///     (which every plugin window and every Dalamud window is chained to) when it is true.
///   - <c>NotificationManager.Draw()</c> is gated by it as well (toast notifications).
///   - <c>SystemMenuIntegration</c> only injects the "Dalamud Plugins"/"Dalamud Settings"
///     entries into the Escape menu when it is true.
///
/// Setting it to <c>false</c> therefore hides everything in one shot, and also skips the
/// per-frame ImGui window construction that otherwise costs CPU.
/// </summary>
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
            // Dalamud.dll
            var dalamudAssembly = typeof(IPluginLog).Assembly;

            var interfaceManagerType = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
            var serviceOpenGeneric = dalamudAssembly.GetType("Dalamud.Service`1");

            if (interfaceManagerType == null || serviceOpenGeneric == null)
            {
                Plugin.Log?.Error("Streamer Mode: could not locate internal Dalamud types. Incompatible Dalamud version?");
                return;
            }

            var serviceClosed = serviceOpenGeneric.MakeGenericType(interfaceManagerType);
            var getMethod = serviceClosed.GetMethod("Get", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (getMethod == null)
            {
                Plugin.Log?.Error("Streamer Mode: could not locate Service<InterfaceManager>.Get().");
                return;
            }

            _interfaceManager = getMethod.Invoke(null, null);
            _isDispatchingEvents = interfaceManagerType.GetProperty("IsDispatchingEvents", BindingFlags.Public | BindingFlags.Instance);

            if (_isDispatchingEvents == null)
            {
                Plugin.Log?.Error("Streamer Mode: IsDispatchingEvents property not found. Incompatible Dalamud version?");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.Error(ex, "Streamer Mode: failed to initialize reflection bridge.");
        }
    }

    /// <summary>Sets <c>InterfaceManager.IsDispatchingEvents</c>. Returns false on any failure.</summary>
    public static bool SetDispatchingEvents(bool value)
    {
        EnsureInitialized();

        if (_isDispatchingEvents == null || _interfaceManager == null)
        {
            Plugin.Log?.Error("Streamer Mode: reflection bridge is not available.");
            return false;
        }

        try
        {
            _isDispatchingEvents.SetValue(_interfaceManager, value);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log?.Error(ex, "Streamer Mode: failed to set IsDispatchingEvents.");
            return false;
        }
    }
}
