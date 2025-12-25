using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using BuffAlert.Configuration;
using BuffAlert.Controllers;
using BuffAlert.Windows;

namespace BuffAlert;

public sealed class BuffAlertPlugin : IDalamudPlugin {
    private const string CommandName = "/buffalert";
    private const string CommandNameAlt = "/ba";

    public BuffAlertPlugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();

        System.SystemConfig = new SystemConfig();
        System.BlacklistController = new BlacklistController();
        System.ModuleController = new ModuleController();

        // Create windows
        System.WarningWindow = new WarningWindow();
        System.PartyFrameWindow = new PartyFrameWindow();
        System.PartyOverlayWindow = new PartyOverlayWindow();
        System.ConfigurationWindow = new ConfigurationWindow();

        // Register windows with Dalamud's window system
        System.WindowSystem.AddWindow(System.WarningWindow);
        System.WindowSystem.AddWindow(System.PartyFrameWindow);
        System.WindowSystem.AddWindow(System.PartyOverlayWindow);
        System.WindowSystem.AddWindow(System.ConfigurationWindow);

        // Register UI and commands
        Services.PluginInterface.UiBuilder.Draw += System.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Opens the BuffAlert configuration window"
        });
        Services.CommandManager.AddHandler(CommandNameAlt, new CommandInfo(OnCommand) {
            HelpMessage = "Opens the BuffAlert configuration window"
        });

        if (Services.ClientState.IsLoggedIn) {
            Services.Framework.RunOnFrameworkThread(OnLogin);
        }

        Services.Framework.Update += OnFrameworkUpdate;
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;
        Services.DutyState.DutyWiped += OnDutyReset;
        Services.DutyState.DutyCompleted += OnDutyReset;
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Dispose() {
        System.ModuleController.Dispose();

        Services.Framework.Update -= OnFrameworkUpdate;
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.Logout -= OnLogout;
        Services.DutyState.DutyWiped -= OnDutyReset;
        Services.DutyState.DutyCompleted -= OnDutyReset;
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;

        Services.PluginInterface.UiBuilder.Draw -= System.WindowSystem.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

        Services.CommandManager.RemoveHandler(CommandName);
        Services.CommandManager.RemoveHandler(CommandNameAlt);

        System.WindowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string args) {
        System.ConfigurationWindow.Toggle();
    }

    private void OnOpenConfigUi() {
        System.ConfigurationWindow.Toggle();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Services.ClientState.IsLoggedIn) return;
        if (Services.Condition.IsBetweenAreas()) return;

        // Track combat state changes
        System.OnCombatChanged(Services.Condition.IsInCombat());

        // Process and Collect Warnings
        System.ActiveWarnings = System.ModuleController.EvaluateWarnings();
        System.OnWarningsUpdated();

        // Update suppression based on config
        System.UpdateSuppression();

        // Update the warning windows
        System.WarningWindow.UpdateWarnings(System.ActiveWarnings);
        System.PartyFrameWindow.UpdateWarnings(System.ActiveWarnings);
        System.PartyOverlayWindow.UpdateWarnings(System.ActiveWarnings);
    }

    private void OnLogin() {
        System.SystemConfig = SystemConfig.Load();
        System.BlacklistController.Load();
        System.ModuleController.Load();
    }

    private void OnLogout(int type, int code) {
        System.SystemConfig = null;
        System.SuppressionManager.Clear();
    }

    private void OnDutyReset(object? sender, ushort territoryId) {
        // Clear suppressions on wipe or duty complete
        System.SuppressionManager.Clear();
    }

    private void OnTerritoryChanged(ushort territoryId) {
        // Clear suppressions when changing zones
        System.SuppressionManager.Clear();
    }
}