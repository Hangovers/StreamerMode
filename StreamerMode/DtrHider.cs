using Dalamud.Game.Gui.Dtr;

namespace StreamerMode;

public sealed class DtrHider
{
    private readonly Dictionary<string, bool> _saved = new();

    public void HideAll()
    {
        _saved.Clear();
        foreach (var entry in Service.DtrBar.Entries)
        {
            _saved[entry.Title] = entry.Shown;
            if (entry is IDtrBarEntry e && e.Shown)
                e.Shown = false;
        }
    }

    public void EnforceHidden()
    {
        foreach (var entry in Service.DtrBar.Entries)
            if (entry is IDtrBarEntry e && e.Shown)
                e.Shown = false;
    }

    public void RestoreAll()
    {
        foreach (var entry in Service.DtrBar.Entries)
            if (entry is IDtrBarEntry e && _saved.TryGetValue(entry.Title, out var wasShown))
                e.Shown = wasShown;
        _saved.Clear();
    }
}
