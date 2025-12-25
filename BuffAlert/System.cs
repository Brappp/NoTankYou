using System;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using BuffAlert.Classes;
using BuffAlert.Configuration;
using BuffAlert.Controllers;
using BuffAlert.Windows;

namespace BuffAlert;

public static class System {
	public static ModuleController ModuleController { get; set; } = null!;
	public static BlacklistController BlacklistController { get; set; } = null!;
	public static SuppressionManager SuppressionManager { get; } = new();
	public static SystemConfig? SystemConfig { get; set; }
	public static List<WarningState> ActiveWarnings { get; set; } = [];

	// Combat tracking
	public static DateTime CombatStartTime { get; set; } = DateTime.MinValue;
	public static bool IsInCombat { get; set; }

	public static bool ShouldHideSoloForCombat() {
		if (SystemConfig?.SoloHideInCombat != true) return false;
		if (!IsInCombat) return false;

		var secondsInCombat = (DateTime.UtcNow - CombatStartTime).TotalSeconds;
		return secondsInCombat >= SystemConfig.SoloHideInCombatDelay;
	}

	public static bool ShouldHidePartyFrameForCombat() {
		if (SystemConfig?.PartyFrameHideInCombat != true) return false;
		if (!IsInCombat) return false;

		var secondsInCombat = (DateTime.UtcNow - CombatStartTime).TotalSeconds;
		return secondsInCombat >= SystemConfig.PartyFrameHideInCombatDelay;
	}

	public static bool ShouldHidePartyOverlayForCombat() {
		if (SystemConfig?.PartyOverlayHideInCombat != true) return false;
		if (!IsInCombat) return false;

		var secondsInCombat = (DateTime.UtcNow - CombatStartTime).TotalSeconds;
		return secondsInCombat >= SystemConfig.PartyOverlayHideInCombatDelay;
	}

	// Dalamud's built-in window system
	public static WindowSystem WindowSystem { get; } = new("BuffAlert");
	public static WarningWindow WarningWindow { get; set; } = null!;
	public static PartyFrameWindow PartyFrameWindow { get; set; } = null!;
	public static PartyOverlayWindow PartyOverlayWindow { get; set; } = null!;
	public static ConfigurationWindow ConfigurationWindow { get; set; } = null!;
}