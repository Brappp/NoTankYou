namespace BuffAlert.Configuration;

public class SystemConfig {
	public bool Enabled = true;
	public bool SoloMode = true;
	public bool PartyFrameMode = true;
	public bool PartyOverlayMode = true;
	public bool OnlyInDuties = true;
	public bool HideInQuestEvent = true;
	public bool AutoSuppress;
	public int AutoSuppressTime = 60;
	public bool TestMode;

	// Solo display settings
	public float SoloIconSize = 32f;
	public float SoloBgColor_R = 0.1f;
	public float SoloBgColor_G = 0.1f;
	public float SoloBgColor_B = 0.3f;
	public float SoloBgColor_A = 0.8f;
	public bool SoloHideInCombat;
	public int SoloHideInCombatDelay = 5;

	// Party Frame display settings
	public float PartyFrameIconSize = 32f;
	public float PartyFrameBgColor_R = 0.3f;
	public float PartyFrameBgColor_G = 0.1f;
	public float PartyFrameBgColor_B = 0.1f;
	public float PartyFrameBgColor_A = 0.8f;
	public bool PartyFrameHideInCombat;
	public int PartyFrameHideInCombatDelay = 5;

	// Party Overlay display settings
	public float PartyOverlayIconSize = 32f;
	public float PartyOverlayHeightOffset = 2.5f;
	public bool PartyOverlayHideInCombat;
	public int PartyOverlayHideInCombatDelay = 5;

	public static SystemConfig Load()
		=> Utilities.Config.LoadCharacterConfig<SystemConfig>("System.config.json");

	public void Save()
		=> Utilities.Config.SaveCharacterConfig(this, "System.config.json");
}