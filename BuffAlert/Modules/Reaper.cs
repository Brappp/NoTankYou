using System;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public class Reaper : ModuleBase<ReaperConfiguration> {
    public override ModuleName ModuleName => ModuleName.Reaper;
    protected override string DefaultWarningText => "Status Missing";
    public override uint[] CheckedActionIds => [SoulsowActionId];

    private const uint ReaperClassJobId = 39;
    private const uint MinimumLevel = 82;

    private const uint SoulsowActionId = 24387;
    private const uint SoulsowStatusId = 2594;
    
    private DateTime lastCombatTime = DateTime.UtcNow;
    
    protected override bool ShouldEvaluate(IPlayerData playerData) {
        if (Services.ObjectTable.LocalPlayer?.EntityId != playerData.GetEntityId()) return false;
        if (playerData.GetClassJob() != ReaperClassJobId) return false;
        if (playerData.GetLevel() < MinimumLevel) return false;

        return true;
    }

    protected override void EvaluateWarnings(IPlayerData playerData) {
        if (Services.Condition.IsInCombat()) {
            lastCombatTime = DateTime.UtcNow;
        }

        if (DateTime.UtcNow - lastCombatTime > TimeSpan.FromSeconds(Config.WarningDelay)) {
            if (playerData.MissingStatus(SoulsowStatusId)) {
                AddActiveWarning(SoulsowActionId, playerData);
            }
        }
    }
}

public class ReaperConfiguration() : ModuleConfigBase(ModuleName.Reaper) {
    public int WarningDelay = 5;

    public override bool HasOptions => true;

    protected override void DrawModuleConfig() {
        ImGui.Text("Delay:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(40.0f * ImGuiHelpers.GlobalScale);
        ConfigChanged |= ImGui.InputInt("##WarningDelay", ref WarningDelay);
        if (WarningDelay < 0) WarningDelay = 0;
        ImGui.SameLine();
        ImGui.TextColored(new global::System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), "sec after combat");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Seconds after leaving combat before showing warning");
    }
}