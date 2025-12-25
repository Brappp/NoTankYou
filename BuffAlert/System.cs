using System;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using BuffAlert.Classes;
using BuffAlert.Configuration;
using BuffAlert.Controllers;
using BuffAlert.Windows;

namespace BuffAlert;

public enum DisplayMode {
	Solo,
	PartyFrame,
	PartyOverlay
}

public static class System {
	public static ModuleController ModuleController { get; set; } = null!;
	public static BlacklistController BlacklistController { get; set; } = null!;
	public static SuppressionManager SuppressionManager { get; } = new();
	public static SystemConfig? SystemConfig { get; set; }
	public static List<WarningState> ActiveWarnings { get; set; } = [];

	// Combat tracking
	public static DateTime CombatStartTime { get; set; } = DateTime.MinValue;
	public static DateTime WarningsFirstSeenTime { get; set; } = DateTime.MinValue;
	public static bool IsInCombat { get; set; }
	public static bool HadWarningsBeforeCombat { get; set; }

	/// <summary>
	/// Call this every frame to update suppression states based on config
	/// </summary>
	public static void UpdateSuppression() {
		if (SystemConfig is null) return;

		UpdateDisplayModeSuppression(DisplayMode.Solo, SystemConfig.SoloSuppressMode, SystemConfig.SoloSuppressDelay);
		UpdateDisplayModeSuppression(DisplayMode.PartyFrame, SystemConfig.PartyFrameSuppressMode, SystemConfig.PartyFrameSuppressDelay);
		UpdateDisplayModeSuppression(DisplayMode.PartyOverlay, SystemConfig.PartyOverlaySuppressMode, SystemConfig.PartyOverlaySuppressDelay);
	}

	private static void UpdateDisplayModeSuppression(DisplayMode mode, SuppressMode suppressMode, int delay) {
		switch (suppressMode) {
			case SuppressMode.Never:
				SuppressionManager.UnsuppressDisplayMode(mode);
				break;

			case SuppressMode.AfterTime:
				// Suppress after X seconds of warnings being shown
				if (ActiveWarnings.Count > 0 && WarningsFirstSeenTime != DateTime.MinValue) {
					var secondsSinceWarnings = (DateTime.UtcNow - WarningsFirstSeenTime).TotalSeconds;
					if (secondsSinceWarnings >= delay) {
						SuppressionManager.SuppressDisplayMode(mode);
					}
				}
				else {
					// Clear suppression when warnings are gone
					SuppressionManager.UnsuppressDisplayMode(mode);
				}
				break;

			case SuppressMode.OnCombatStart:
				// Suppress when combat starts (after optional delay)
				// Note: unsuppression happens in OnCombatChanged when leaving combat
				if (IsInCombat) {
					var secondsInCombat = (DateTime.UtcNow - CombatStartTime).TotalSeconds;
					if (secondsInCombat >= delay) {
						SuppressionManager.SuppressDisplayMode(mode);
					}
				}
				break;
		}
	}

	/// <summary>
	/// Call when warnings change to track when they first appeared
	/// </summary>
	public static void OnWarningsUpdated() {
		if (ActiveWarnings.Count > 0 && WarningsFirstSeenTime == DateTime.MinValue) {
			WarningsFirstSeenTime = DateTime.UtcNow;
		}
		else if (ActiveWarnings.Count == 0) {
			WarningsFirstSeenTime = DateTime.MinValue;
		}
	}

	/// <summary>
	/// Call when combat state changes
	/// </summary>
	public static void OnCombatChanged(bool inCombat) {
		var wasInCombat = IsInCombat;
		IsInCombat = inCombat;

		if (inCombat && !wasInCombat) {
			// Entering combat
			CombatStartTime = DateTime.UtcNow;
			HadWarningsBeforeCombat = ActiveWarnings.Count > 0;
		}
		else if (!inCombat && wasInCombat) {
			// Leaving combat - clear suppressions
			SuppressionManager.UnsuppressDisplayMode(DisplayMode.Solo);
			SuppressionManager.UnsuppressDisplayMode(DisplayMode.PartyFrame);
			SuppressionManager.UnsuppressDisplayMode(DisplayMode.PartyOverlay);
			WarningsFirstSeenTime = DateTime.MinValue;
		}
	}

	/// <summary>
	/// Check if a display mode should be hidden (for DrawConditions)
	/// </summary>
	public static bool ShouldHideDisplayMode(DisplayMode mode)
		=> SuppressionManager.IsDisplayModeSuppressed(mode);

	// Dalamud's built-in window system
	public static WindowSystem WindowSystem { get; } = new("BuffAlert");
	public static WarningWindow WarningWindow { get; set; } = null!;
	public static PartyFrameWindow PartyFrameWindow { get; set; } = null!;
	public static PartyOverlayWindow PartyOverlayWindow { get; set; } = null!;
	public static ConfigurationWindow ConfigurationWindow { get; set; } = null!;
}