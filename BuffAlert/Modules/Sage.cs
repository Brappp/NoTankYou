using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public class Sage : ModuleBase<SageConfiguration> {
    public override ModuleName ModuleName => ModuleName.Sage;
    protected override string DefaultWarningText => "Sage Kardion";
    public override uint[] CheckedActionIds => [KardiaActionId];

    private const byte MinimumLevel = 4;
    private const byte SageClassJob = 40;
    private const int KardiaStatusId = 2604;
    private const uint KardiaActionId = 24285;
    
    protected override unsafe bool ShouldEvaluate(IPlayerData playerData) {
        if (Config.DisableWhileSolo && GroupManager.Instance()->MainGroup.MemberCount is 0) return false;
        if (playerData.MissingClassJob(SageClassJob)) return false;
        if (playerData.GetLevel() < MinimumLevel) return false;

        return true;
    }
    
    protected override void EvaluateWarnings(IPlayerData playerData) {
        if (playerData.MissingStatus(KardiaStatusId)) {
            AddActiveWarning(KardiaActionId, playerData);
        }
    }
}

public class SageConfiguration() : ModuleConfigBase(ModuleName.Sage) {
    public bool DisableWhileSolo = true;

    public override bool HasOptions => true;

    protected override void DrawModuleConfig() {
        ConfigChanged |= ImGui.Checkbox("Disable while Solo", ref DisableWhileSolo);
    }
}
