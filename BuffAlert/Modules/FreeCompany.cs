using System.Linq;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public class FreeCompany : ModuleBase<FreeCompanyConfiguration> {
    public override ModuleName ModuleName => ModuleName.FreeCompany;
    protected override string DefaultWarningText => "Free Company Buff";

    private const uint FreeCompanyActionId = 43;
    private int? _freeCompanyIconId;
    private uint[]? _statusList;

    private int FreeCompanyIconId => _freeCompanyIconId ??= Services.DataManager.GetExcelSheet<CompanyAction>().GetRowOrDefault(FreeCompanyActionId)?.Icon ?? 0;

    private uint[] StatusList => _statusList ??= Services.DataManager.GetExcelSheet<Status>()
        .Where(status => status.IsFcBuff)
        .Select(status => status.RowId)
        .ToArray();

    protected override bool ShouldEvaluate(IPlayerData playerData) {
        if (Services.ObjectTable.LocalPlayer?.EntityId != playerData.GetEntityId()) return false;
        var localPlayer = Services.ObjectTable.LocalPlayer;
        if (localPlayer?.HomeWorld.RowId != localPlayer?.CurrentWorld.RowId) return false;

        return true;
    }

    protected override void EvaluateWarnings(IPlayerData playerData) {
        if (playerData.MissingStatus(StatusList)) {
            AddActiveWarning((uint)FreeCompanyIconId, "FC Buff", playerData);
        }
    }
}

public class FreeCompanyConfiguration() : ModuleConfigBase(ModuleName.FreeCompany) {
    protected override void DrawModuleConfig() {
        ImGui.TextWrapped("Warns when you don't have any Free Company buff active.");
    }
}
