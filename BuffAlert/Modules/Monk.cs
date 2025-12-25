using System;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using BuffAlert.Classes;
using BuffAlert.PlayerDataInterface;

namespace BuffAlert.Modules;

public class Monk : ModuleBase<MonkConfiguration> {
	public override ModuleName ModuleName => ModuleName.Monk;
	protected override string DefaultWarningText => "Monk Warning";
	public override uint[] CheckedActionIds => [MantraActionId, FormlessFistActionId];
	public override bool SelfOnly => true;

	private const byte MinimumLevel = 40;
	private const byte MonkClassJob = 20;

	private const uint MantraActionId = 36943;
	private const uint MantraMinimumLevel = 54;
	
	private const uint FormlessFistActionId = 4262;
	private const uint FormlessFistStatusEffect = 2513;
	private const uint FormlessFistMinimumLevel = 52;

	private DateTime lastCombatTime = DateTime.UtcNow;
	
	protected override bool ShouldEvaluate(IPlayerData playerData) {
		// Self-only module (requires job gauge)
		if (Services.ObjectTable.LocalPlayer?.EntityId != playerData.GetEntityId()) return false;
		if (!playerData.HasClassJob(MonkClassJob)) return false;
		if (playerData.GetLevel() < MinimumLevel) return false;

		return true;
	}

	protected override void EvaluateWarnings(IPlayerData playerData) {
		if (Services.Condition.IsInCombat()) {
			lastCombatTime = DateTime.UtcNow;
		}

		if (DateTime.UtcNow - lastCombatTime > TimeSpan.FromSeconds(Config.WarningDelay)) {
			// Mantra (Chakra gauge)
			if (playerData.GetLevel() >= MantraMinimumLevel) {
				if (Services.JobGauges.Get<MNKGauge>().Chakra < 5) {
					AddActiveWarning(MantraActionId, playerData);
					return;
				}
			}

			// Formless Fist (status effect)
			if (playerData.GetLevel() >= FormlessFistMinimumLevel) {
				if (Config.FormlessFist && playerData.MissingStatus(FormlessFistStatusEffect)) {
					AddActiveWarning(FormlessFistActionId, playerData);
				}
			}
		}
	}
}

public class MonkConfiguration() : ModuleConfigBase(ModuleName.Monk) {
	public int WarningDelay = 5;

	public bool FormlessFist;

	public override bool HasOptions => true;

	protected override void DrawModuleConfig() {
		ConfigChanged |= ImGui.Checkbox("Formless Fist Warning", ref FormlessFist);

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
