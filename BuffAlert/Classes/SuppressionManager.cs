using System.Collections.Generic;

namespace BuffAlert.Classes;

public class SuppressionManager {
    // Suppresses a specific module for ALL players (when clicking on icon)
    private readonly HashSet<ModuleName> suppressedModules = [];

    // Suppresses a specific player for a specific module (auto-suppress)
    private readonly HashSet<(ModuleName, ulong)> suppressedPlayerWarnings = [];

    // Per-display mode suppression (for combat/time-based auto-suppress)
    private readonly HashSet<DisplayMode> suppressedDisplayModes = [];

    public bool IsSuppressed(WarningState warning) {
        // Check if entire module is muted
        if (suppressedModules.Contains(warning.SourceModule))
            return true;

        // Check if specific player+module is muted
        return suppressedPlayerWarnings.Contains((warning.SourceModule, warning.SourceEntityId));
    }

    public bool IsSuppressed(WarningState warning, DisplayMode displayMode) {
        // Check display mode suppression first
        if (suppressedDisplayModes.Contains(displayMode))
            return true;

        return IsSuppressed(warning);
    }

    public bool IsModuleSuppressed(ModuleName module)
        => suppressedModules.Contains(module);

    public bool IsPlayerSuppressed(ModuleName module, ulong entityId)
        => suppressedPlayerWarnings.Contains((module, entityId));

    public bool IsDisplayModeSuppressed(DisplayMode mode)
        => suppressedDisplayModes.Contains(mode);

    /// <summary>
    /// Toggle suppression for an entire module (all players)
    /// </summary>
    public void ToggleModuleSuppression(ModuleName module) {
        if (!suppressedModules.Remove(module)) {
            suppressedModules.Add(module);
        }
    }

    /// <summary>
    /// Suppress a specific player for a specific module (used by auto-suppress)
    /// </summary>
    public void SuppressPlayer(ModuleName module, ulong entityId)
        => suppressedPlayerWarnings.Add((module, entityId));

    public void UnsuppressPlayer(ModuleName module, ulong entityId)
        => suppressedPlayerWarnings.Remove((module, entityId));

    /// <summary>
    /// Suppress an entire display mode (for combat-based suppression)
    /// </summary>
    public void SuppressDisplayMode(DisplayMode mode)
        => suppressedDisplayModes.Add(mode);

    public void UnsuppressDisplayMode(DisplayMode mode)
        => suppressedDisplayModes.Remove(mode);

    public void Clear() {
        suppressedModules.Clear();
        suppressedPlayerWarnings.Clear();
        suppressedDisplayModes.Clear();
    }
}
