using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BuffAlert.Classes;

public class ActionDisplaySettings {
	public bool ShowInSolo = true;
	public bool ShowInPartyFrame = true;
	public bool ShowInPartyOverlay = true;
}

public abstract class ModuleConfigBase(ModuleName moduleName) {
	public bool Enabled;

	/// <summary>
	/// Per-action display settings. Key is the action ID.
	/// </summary>
	public Dictionary<uint, ActionDisplaySettings> ActionSettings { get; set; } = new();

	[JsonIgnore] protected bool ConfigChanged { get; set; }
	[JsonIgnore] public virtual bool HasOptions => false;

	public ActionDisplaySettings GetActionSettings(uint actionId) {
		if (!ActionSettings.TryGetValue(actionId, out var settings)) {
			settings = new ActionDisplaySettings();
			ActionSettings[actionId] = settings;
		}
		return settings;
	}

	public void DrawConfigUi() {
		DrawModuleConfig();

		if (ConfigChanged) {
			Services.PluginLog.Verbose($"Saving config for {moduleName}");
			Save();
			ConfigChanged = false;
		}
	}

	protected virtual void DrawModuleConfig() { }

	public static T Load<T>(ModuleName moduleName) where T : new()
		=> Utilities.Config.LoadCharacterConfig<T>($"{moduleName}.config.json");

	public void Save()
		=> Utilities.Config.SaveCharacterConfig(this, $"{moduleName}.config.json");
}
