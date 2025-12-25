using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Classes;

public abstract class ConsumableModule<T> : ModuleBase<T> where T : ConsumableConfiguration, new() {
    protected abstract uint IconId { get; }
    protected abstract string IconLabel { get; }
    protected abstract uint StatusId { get; }
    public override bool SelfOnly => true;

    protected override bool ShouldEvaluate(IPlayerData playerData) {
        // Self-only module
        if (Services.ObjectTable.LocalPlayer?.EntityId != playerData.GetEntityId()) return false;
        if (Config.SuppressInCombat && Services.Condition.IsInCombat()) return false;

        return true;
    }

    protected override void EvaluateWarnings(IPlayerData playerData) {
        var statusTimeRemaining = playerData.GetStatusTimeRemaining(StatusId);

        if (statusTimeRemaining < Config.EarlyWarningTime) {
            AddActiveWarning(IconId, IconLabel, playerData);
        }
    }
}

public abstract class ConsumableConfiguration(ModuleName moduleName) : ModuleConfigBase(moduleName) {
    public bool SuppressInCombat = true;
    public int EarlyWarningTime = 600;

    public override bool HasOptions => true;

    protected override void DrawModuleConfig() {
        ConfigChanged |= ImGui.Checkbox("Suppress in Combat", ref SuppressInCombat);

        ImGui.Text("Warn when");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
        ConfigChanged |= ImGui.InputInt("##EarlyWarningTime", ref EarlyWarningTime);
        if (EarlyWarningTime < 0) EarlyWarningTime = 0;
        ImGui.SameLine();
        ImGui.TextColored(new global::System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), "sec remaining");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show warning when buff has this many seconds left");
    }
}
