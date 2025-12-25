using System;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public class Pictomancer : ModuleBase<PictomancerConfiguration> {
    public override ModuleName ModuleName => ModuleName.Pictomancer;
    protected override string DefaultWarningText => "Missing Motif";
    public override uint[] CheckedActionIds => [CreatureActionId, WeaponActionId, LandscapeActionId];

    private const uint PictoClassJobId = 42;
    private const uint MinimumLevel = 30;

    private const uint CreatureMinimumLevel = 30;
    private const uint CreatureActionId = 34664;

    private const uint WeaponMinimumLevel = 50;
    private const uint WeaponActionId = 34668;

    private const uint LandscapeMinimumLevel = 70;
    private const uint LandscapeActionId = 34669;

    private DateTime lastCombatTime = DateTime.UtcNow;
    
    protected override bool ShouldEvaluate(IPlayerData playerData) {
        if (Services.ObjectTable.LocalPlayer?.EntityId != playerData.GetEntityId()) return false;
        if (playerData.GetClassJob() != PictoClassJobId) return false;
        if (playerData.GetLevel() < MinimumLevel) return false;

        return true;
    }

    protected override void EvaluateWarnings(IPlayerData playerData) {
        if (Services.Condition.IsInCombat()) {
            lastCombatTime = DateTime.UtcNow;
        }

        if (DateTime.UtcNow - lastCombatTime > TimeSpan.FromSeconds(Config.WarningDelay)) {
            if (playerData.GetLevel() >= CreatureMinimumLevel && !Services.JobGauges.Get<PCTGauge>().CreatureMotifDrawn) {
                AddActiveWarning(CreatureActionId, playerData);
                return;
            }
        
            if (playerData.GetLevel() >= WeaponMinimumLevel && !Services.JobGauges.Get<PCTGauge>().WeaponMotifDrawn) {
                AddActiveWarning(WeaponActionId, playerData);
                return;
            }
        
            if (playerData.GetLevel() >= LandscapeMinimumLevel && !Services.JobGauges.Get<PCTGauge>().LandscapeMotifDrawn) {
                AddActiveWarning(LandscapeActionId, playerData);
            }
        }
    }
}

public class PictomancerConfiguration() : ModuleConfigBase(ModuleName.Pictomancer) {
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
