using System.Collections.Generic;

namespace BuffAlert.Classes;

public class SuppressionManager {
    // Suppresses a specific module for ALL players (when clicking on party frame)
    private readonly HashSet<ModuleName> suppressedModules = [];

    // Suppresses a specific player for a specific module (auto-suppress)
    private readonly HashSet<(ModuleName, ulong)> suppressedPlayerWarnings = [];

    public bool IsSuppressed(WarningState warning) {
        // Check if entire module is muted
        if (suppressedModules.Contains(warning.SourceModule))
            return true;

        // Check if specific player+module is muted
        return suppressedPlayerWarnings.Contains((warning.SourceModule, warning.SourceEntityId));
    }

    public bool IsModuleSuppressed(ModuleName module)
        => suppressedModules.Contains(module);

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

    public void Clear() {
        suppressedModules.Clear();
        suppressedPlayerWarnings.Clear();
    }
}
