using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Interop;
using Dalamud.Bindings.ImGui;
using BuffAlert.PlayerDataInterface;
using Action = Lumina.Excel.Sheets.Action;
using Vector2 = System.Numerics.Vector2;

namespace BuffAlert.Classes;

public abstract class ModuleBase {
    public abstract ModuleName ModuleName { get; }
    public abstract void Load();
    public abstract void DrawConfigUi();
    public abstract void EvaluateWarnings();
    public List<WarningState> ActiveWarningStates { get; } = [];
    public bool HasWarnings => ActiveWarningStates.Count != 0;

    public abstract bool IsEnabled { get; set; }
    public abstract bool HasConfigOptions { get; }

    /// <summary>
    /// Action IDs that this module checks for. Used to display skill icons in config.
    /// </summary>
    public virtual uint[] CheckedActionIds => [];

    /// <summary>
    /// If true, this module only checks the local player (not party members).
    /// Used by the UI to hide party-related display options.
    /// </summary>
    public virtual bool SelfOnly => false;

    /// <summary>
    /// If true, this module requires being in a party to show warnings.
    /// Used by the UI to indicate party requirement.
    /// </summary>
    public virtual bool RequiresParty => false;

    /// <summary>
    /// Gets the display settings for a specific action.
    /// </summary>
    public abstract ActionDisplaySettings GetActionDisplaySettings(uint actionId);

    /// <summary>
    /// Saves the module configuration.
    /// </summary>
    public abstract void SaveConfig();
}

public abstract unsafe class ModuleBase<T> : ModuleBase, IDisposable where T : ModuleConfigBase, new() {
    protected T Config { get; private set; } = new();

    public override bool IsEnabled {
        get => Config.Enabled;
        set {
            Config.Enabled = value;
            Config.Save();
        }
    }

    public override bool HasConfigOptions => Config.HasOptions;

    public override ActionDisplaySettings GetActionDisplaySettings(uint actionId)
        => Config.GetActionSettings(actionId);

    public override void SaveConfig() => Config.Save();

    protected abstract string DefaultWarningText { get; }

    private readonly Dictionary<ulong, Stopwatch> suppressionTimer = new();

    private readonly DeathTracker deathTracker = new();

    public virtual void Dispose() { }

    protected abstract bool ShouldEvaluate(IPlayerData playerData);

    protected abstract void EvaluateWarnings(IPlayerData playerData);

    public override void EvaluateWarnings() {
        ActiveWarningStates.Clear();

        if (System.SystemConfig is null) return;
        if (!Config.Enabled) return;
        if (Services.ClientState.IsPvPExcludingDen) return;
        if (System.SystemConfig.OnlyInDuties && !Services.Condition.IsBoundByDuty()) return;
        if (System.BlacklistController.IsZoneBlacklisted(Services.ClientState.TerritoryType)) return;
        if (System.SystemConfig.OnlyInDuties && !Services.DutyState.IsDutyStarted) return;
        if (Services.Condition.IsCrossWorld()) return;
        if (System.SystemConfig.HideInQuestEvent && Services.Condition.IsInCutsceneOrQuestEvent()) return;

        var groupManager = GroupManager.Instance();

        if (groupManager->MainGroup.MemberCount is 0) {
            if (Services.ObjectTable.LocalPlayer is not { } player) return;

            var localPlayer = (Character*)player.Address;
            if (localPlayer is null) return;

            ProcessPlayer(new CharacterPlayerData(localPlayer));
        }
        else {
            foreach (var partyMember in PartyMemberSpan.PointerEnumerator()) {
                ProcessPlayer(new PartyMemberPlayerData(partyMember));
            }
        }
    }

    private void ProcessPlayer(IPlayerData player) {
        if (player.GetEntityId() is 0xE0000000 or 0) return;
        if (HasDisallowedCondition()) return;
        if (HasDisallowedStatus(player)) return;
        if (deathTracker.IsDead(player)) return;
        if (!ShouldEvaluate(player)) return;

        // Skip if this player is already auto-suppressed for this module
        if (System.SuppressionManager.IsPlayerSuppressed(ModuleName, player.GetEntityId())) return;

        EvaluateWarnings(player);
        EvaluateAutoSuppression(player);
    }

    private void EvaluateAutoSuppression(IPlayerData player) {
        if (System.SystemConfig?.AutoSuppress != true) return;

        if (Services.ObjectTable.LocalPlayer is { EntityId: var playerEntityId } && playerEntityId == player.GetEntityId()) {
            return; // Do not allow auto suppression for the user.
        }

        suppressionTimer.TryAdd(player.GetEntityId(), Stopwatch.StartNew());
        if (suppressionTimer.TryGetValue(player.GetEntityId(), out var timer)) {
            if (HasWarnings) {
                if (timer.Elapsed.TotalSeconds >= System.SystemConfig.AutoSuppressTime) {
                    System.SuppressionManager.SuppressPlayer(ModuleName, player.GetEntityId());
                    Services.PluginLog.Warning($"[{ModuleName}]: Adding {player.GetName()} to auto-suppression list");
                }
            }
            else {
                timer.Restart();
            }
        }
    }

    private bool HasDisallowedStatus(IPlayerData player)
        => player.HasStatus(1534);

    private static bool HasDisallowedCondition()
        => Services.Condition.Any(ConditionFlag.Jumping61,
            ConditionFlag.Transformed,
            ConditionFlag.InThisState89);

    public override void DrawConfigUi() {
        Config.DrawConfigUi();
    }

    public override void Load() {
        Services.PluginLog.Debug($"[{ModuleName}] Loading Module");
        Config = ModuleConfigBase.Load<T>(ModuleName);
    }

    protected static Span<PartyMember> PartyMemberSpan
        => GroupManager.Instance()->MainGroup.PartyMembers[..GroupManager.Instance()->MainGroup.MemberCount];

    protected void AddActiveWarning(uint actionId, IPlayerData playerData) {
        var action = Services.DataManager.GetExcelSheet<Action>().GetRowOrDefault(actionId);
        if (action is null) return;

        ActiveWarningStates.Add(new WarningState {
            IconId = action.Value.Icon,
            ActionId = actionId,
            IconLabel = action.Value.Name.ToString(),
            Message = DefaultWarningText,
            SourcePlayerName = playerData.GetName(),
            SourceEntityId = playerData.GetEntityId(),
            SourceModule = ModuleName,
        });
    }

    protected void AddActiveWarning(uint iconId, string iconLabel, IPlayerData playerData) => ActiveWarningStates.Add(new WarningState {
        IconId = iconId,
        ActionId = 0,
        IconLabel = iconLabel,
        Message = DefaultWarningText,
        SourcePlayerName = playerData.GetName(),
        SourceEntityId = playerData.GetEntityId(),
        SourceModule = ModuleName,
    });
}
