namespace BuffAlert.Configuration;

public enum SuppressMode {
	Never,          // Never auto-suppress
	AfterTime,      // Suppress after X seconds
	OnCombatStart,  // Suppress when combat starts
}

public class SystemConfig {
	public bool Enabled = true;
	public bool TestMode;
	public bool OnlyInDuties = true;
	public bool HideInQuestEvent = true;

	// Legacy auto-suppress (kept for migration, use per-display settings instead)
	public bool AutoSuppress;
	public int AutoSuppressTime = 60;

	// Solo display settings
	public bool SoloMode = true;
	public float SoloIconSize = 32f;
	public float SoloBgColor_R = 0.1f;
	public float SoloBgColor_G = 0.1f;
	public float SoloBgColor_B = 0.3f;
	public float SoloBgColor_A = 0.8f;
	public SuppressMode SoloSuppressMode = SuppressMode.Never;
	public int SoloSuppressDelay = 5;

	// Party Frame display settings
	public bool PartyFrameMode = true;
	public float PartyFrameIconSize = 32f;
	public float PartyFrameBgColor_R = 0.3f;
	public float PartyFrameBgColor_G = 0.1f;
	public float PartyFrameBgColor_B = 0.1f;
	public float PartyFrameBgColor_A = 0.8f;
	public SuppressMode PartyFrameSuppressMode = SuppressMode.Never;
	public int PartyFrameSuppressDelay = 5;

	// Party Overlay display settings
	public bool PartyOverlayMode = true;
	public float PartyOverlayIconSize = 32f;
	public float PartyOverlayHeightOffset = 2.5f;
	public SuppressMode PartyOverlaySuppressMode = SuppressMode.Never;
	public int PartyOverlaySuppressDelay = 5;

	public static SystemConfig Load()
		=> Utilities.Config.LoadCharacterConfig<SystemConfig>("System.config.json");

	public void Save()
		=> Utilities.Config.SaveCharacterConfig(this, "System.config.json");
}