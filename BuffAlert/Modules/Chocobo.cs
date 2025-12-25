using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public unsafe class Chocobo : ModuleBase<ChocoboConfiguration> {
    public override ModuleName ModuleName => ModuleName.Chocobo;
    protected override string DefaultWarningText => "Chocobo Missing";
    public override bool SelfOnly => true;

    private const uint GyshalGreensItemId = 4868;
    private uint? _gyshalGreensIconId;
    private string? _gyshalGreensActionName;

    private uint GyshalGreensIconId => _gyshalGreensIconId ??= Services.DataManager.GetExcelSheet<Item>().GetRowOrDefault(GyshalGreensItemId)?.Icon ?? 0;
    private string GyshalGreensActionName => _gyshalGreensActionName ??= Services.DataManager.GetExcelSheet<Item>().GetRowOrDefault(GyshalGreensItemId)?.Name.ToString() ?? "Gyshal Greens";

    protected override bool ShouldEvaluate(IPlayerData playerData) {
        if (TerritoryInfo.Instance()->InSanctuary) return false;
        if (Config.DisableInCombat && Services.Condition.IsInCombat()) return false;
        if (playerData.GetEntityId() != Services.ObjectTable.LocalPlayer?.EntityId) return false;

        return true;
    }

    protected override void EvaluateWarnings(IPlayerData playerData) {
        var warningTime = Config.EarlyWarning ? Config.EarlyWarningTime : 0;

        if (UIState.Instance()->Buddy.CompanionInfo.TimeLeft <= warningTime) {
            AddActiveWarning(GyshalGreensIconId, GyshalGreensActionName, playerData);
        }
    }
}

public class ChocoboConfiguration() : ModuleConfigBase(ModuleName.Chocobo) {
    public bool DisableInCombat = true;
    public bool EarlyWarning = true;
    public int EarlyWarningTime = 300;

    public override bool HasOptions => true;

    protected override void DrawModuleConfig() {
        ConfigChanged |= ImGui.Checkbox("Suppress in Combat", ref DisableInCombat);
        ConfigChanged |= ImGui.Checkbox("Early Warning", ref EarlyWarning);

        ImGui.SetNextItemWidth(50.0f * ImGuiHelpers.GlobalScale);
        ConfigChanged |= ImGui.InputInt("Early Warning Time (sec)", ref EarlyWarningTime);
        if (EarlyWarningTime < 0) EarlyWarningTime = 0;
    }
}