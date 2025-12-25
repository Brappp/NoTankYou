using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using BuffAlert.Classes;
using Vector4 = System.Numerics.Vector4;

namespace BuffAlert.Windows;

public class ConfigurationWindow : Window {
    // Category display info
    private static readonly Dictionary<ModuleCategory, (string Name, uint Icon, Vector4 Color)> CategoryInfo = new() {
        { ModuleCategory.Tank, ("Tank", 62581, new Vector4(0.2f, 0.4f, 0.8f, 1f)) },
        { ModuleCategory.Healer, ("Healer", 62582, new Vector4(0.2f, 0.7f, 0.3f, 1f)) },
        { ModuleCategory.DPS, ("DPS", 62584, new Vector4(0.8f, 0.3f, 0.3f, 1f)) },
        { ModuleCategory.Gatherer, ("Gatherer", 62586, new Vector4(0.6f, 0.5f, 0.2f, 1f)) },
        { ModuleCategory.General, ("General", 62574, new Vector4(0.7f, 0.7f, 0.7f, 1f)) },
    };

    public ConfigurationWindow() : base("NoTankYou - Configuration", ImGuiWindowFlags.None) {
        Size = new Vector2(500, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw() {
        // Tab bar at the top
        using (var tabBar = ImRaii.TabBar("ConfigTabs")) {
            if (tabBar) {
                if (ImGui.BeginTabItem("Config")) {
                    DrawConfigTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Blacklist")) {
                    DrawBlacklistTab();
                    ImGui.EndTabItem();
                }
            }
        }
    }

    private void DrawConfigTab() {
        using var child = ImRaii.Child("ConfigList", new Vector2(0, 0), false);
        if (!child) return;

        // General settings at top
        DrawGeneralSettings();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Modules grouped by category
        var modulesByCategory = System.ModuleController.Modules
            .GroupBy(m => m.ModuleName.GetAttribute<ModuleCategoryAttribute>()?.Category ?? ModuleCategory.General)
            .OrderBy(g => g.Key);

        foreach (var categoryGroup in modulesByCategory) {
            DrawCategorySection(categoryGroup.Key, categoryGroup.ToList());
        }
    }

    private void DrawCategorySection(ModuleCategory category, List<ModuleBase> modules) {
        var (categoryName, iconId, color) = CategoryInfo[category];

        ImGui.Spacing();

        // Category header with colored text
        ImGui.TextColored(color, categoryName);
        ImGui.Separator();

        // Single column layout for cleaner display
        foreach (var module in modules) {
            DrawModuleEntry(module);
        }

        ImGui.Spacing();
    }

    private void DrawModuleEntry(ModuleBase module) {
        using var id = ImRaii.PushId(module.ModuleName.ToString());

        // Light separator line between modules
        ImGui.Separator();

        var moduleName = module.ModuleName.GetDescription();
        var iconAttr = module.ModuleName.GetAttribute<ModuleIconAttribute>();

        // Draw module icon
        if (iconAttr != null) {
            var texture = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(iconAttr.ModuleIcon));
            var wrap = texture.GetWrapOrEmpty();
            if (wrap.Handle != nint.Zero) {
                ImGui.Image(wrap.Handle, ImGuiHelpers.ScaledVector2(20, 20));
                ImGui.SameLine();
            }
        }

        // Enable checkbox with module name
        var enabled = module.IsEnabled;
        if (ImGui.Checkbox(moduleName, ref enabled)) {
            module.IsEnabled = enabled;
        }

        // Per-action display settings (when module is enabled)
        if (module.IsEnabled && module.CheckedActionIds.Length > 0) {
            var actionSheet = Services.DataManager.GetExcelSheet<Action>();
            var configChanged = false;

            using (ImRaii.PushIndent(28f)) {
                // Table for action settings
                using (var table = ImRaii.Table($"Actions_{module.ModuleName}", 4, ImGuiTableFlags.BordersInnerH)) {
                    if (table) {
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("S", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("P", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("O", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                        ImGui.TableHeadersRow();

                        foreach (var actionId in module.CheckedActionIds) {
                            var action = actionSheet.GetRowOrDefault(actionId);
                            if (action is null) continue;

                            using var actionPushId = ImRaii.PushId((int)actionId);
                            var settings = module.GetActionDisplaySettings(actionId);

                            ImGui.TableNextRow();

                            // Action column (icon + name)
                            ImGui.TableNextColumn();
                            var actionTexture = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(action.Value.Icon));
                            var actionWrap = actionTexture.GetWrapOrEmpty();
                            if (actionWrap.Handle != nint.Zero) {
                                ImGui.Image(actionWrap.Handle, ImGuiHelpers.ScaledVector2(16, 16));
                                ImGui.SameLine();
                            }
                            ImGui.Text(action.Value.Name.ToString());

                            // S column
                            ImGui.TableNextColumn();
                            configChanged |= ImGui.Checkbox("##S", ref settings.ShowInSolo);
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Solo");

                            // P column
                            ImGui.TableNextColumn();
                            configChanged |= ImGui.Checkbox("##P", ref settings.ShowInPartyFrame);
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Party Frame");

                            // O column
                            ImGui.TableNextColumn();
                            configChanged |= ImGui.Checkbox("##O", ref settings.ShowInPartyOverlay);
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Party Overlay");
                        }
                    }
                }

                // Module-specific options (outside the table)
                if (module.HasConfigOptions) {
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.85f, 0.85f, 0.85f, 1f))) {
                        module.DrawConfigUi();
                    }
                }
            }

            if (configChanged) {
                module.SaveConfig();
            }

            ImGui.Spacing();
        }
        else if (module.IsEnabled && module.HasConfigOptions) {
            // Module has no actions but has config options
            using (ImRaii.PushIndent(28f)) {
                using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.85f, 0.85f, 0.85f, 1f))) {
                    module.DrawConfigUi();
                }
            }
            ImGui.Spacing();
        }
    }

    private void DrawGeneralSettings() {
        var config = System.SystemConfig;
        if (config is null) {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0f, 1f), "Configuration not loaded. Please log in.");
            return;
        }

        var configChanged = false;

        configChanged |= ImGui.Checkbox("Enabled", ref config.Enabled);

        ImGui.SameLine();
        ImGui.Checkbox("Test Mode", ref config.TestMode);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Show test warnings to position the UI elements");
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Display Modes:");

        configChanged |= ImGui.Checkbox("Solo Warnings", ref config.SoloMode);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Show a warning bar for your own missing buffs");
        }
        if (config.SoloMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("##SoloSize", ref config.SoloIconSize, 1f, 16f, 64f, "%.0f");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Icon size");

            ImGui.SameLine();
            var soloBgColor = new Vector4(config.SoloBgColor_R, config.SoloBgColor_G, config.SoloBgColor_B, config.SoloBgColor_A);
            if (ImGui.ColorEdit4("##SoloBg", ref soloBgColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar)) {
                config.SoloBgColor_R = soloBgColor.X;
                config.SoloBgColor_G = soloBgColor.Y;
                config.SoloBgColor_B = soloBgColor.Z;
                config.SoloBgColor_A = soloBgColor.W;
                configChanged = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Background color");

            ImGui.SameLine();
            configChanged |= ImGui.Checkbox("Hide##SoloCombat", ref config.SoloHideInCombat);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hide in combat");
            if (config.SoloHideInCombat) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                configChanged |= ImGui.InputInt("##SoloCombatDelay", ref config.SoloHideInCombatDelay);
                if (config.SoloHideInCombatDelay < 0) config.SoloHideInCombatDelay = 0;
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Seconds in combat before hiding");
            }
        }

        configChanged |= ImGui.Checkbox("Party Frame", ref config.PartyFrameMode);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Show a warning bar for party members' missing buffs");
        }
        if (config.PartyFrameMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("##PartyFrameSize", ref config.PartyFrameIconSize, 1f, 16f, 64f, "%.0f");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Icon size");

            ImGui.SameLine();
            var partyBgColor = new Vector4(config.PartyFrameBgColor_R, config.PartyFrameBgColor_G, config.PartyFrameBgColor_B, config.PartyFrameBgColor_A);
            if (ImGui.ColorEdit4("##PartyFrameBg", ref partyBgColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar)) {
                config.PartyFrameBgColor_R = partyBgColor.X;
                config.PartyFrameBgColor_G = partyBgColor.Y;
                config.PartyFrameBgColor_B = partyBgColor.Z;
                config.PartyFrameBgColor_A = partyBgColor.W;
                configChanged = true;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Background color");

            ImGui.SameLine();
            configChanged |= ImGui.Checkbox("Hide##PartyFrameCombat", ref config.PartyFrameHideInCombat);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hide in combat");
            if (config.PartyFrameHideInCombat) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                configChanged |= ImGui.InputInt("##PartyFrameCombatDelay", ref config.PartyFrameHideInCombatDelay);
                if (config.PartyFrameHideInCombatDelay < 0) config.PartyFrameHideInCombatDelay = 0;
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Seconds in combat before hiding");
            }
        }

        configChanged |= ImGui.Checkbox("Party Overlay", ref config.PartyOverlayMode);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Show warning icons above party members' heads in the world");
        }
        if (config.PartyOverlayMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("##PartyOverlaySize", ref config.PartyOverlayIconSize, 1f, 16f, 64f, "%.0f");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Icon size");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(60 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("##PartyOverlayHeight", ref config.PartyOverlayHeightOffset, 0.1f, 0f, 5f, "%.1f");
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Height above player");

            ImGui.SameLine();
            configChanged |= ImGui.Checkbox("Hide##PartyOverlayCombat", ref config.PartyOverlayHideInCombat);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hide in combat");
            if (config.PartyOverlayHideInCombat) {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                configChanged |= ImGui.InputInt("##PartyOverlayCombatDelay", ref config.PartyOverlayHideInCombatDelay);
                if (config.PartyOverlayHideInCombatDelay < 0) config.PartyOverlayHideInCombatDelay = 0;
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Seconds in combat before hiding");
            }
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Conditions:");

        configChanged |= ImGui.Checkbox("Only in Duties", ref config.OnlyInDuties);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Only show warnings while in a duty (after countdown finishes)");
        }

        configChanged |= ImGui.Checkbox("Hide in Quest Events", ref config.HideInQuestEvent);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Hide warnings during cutscenes and quest events");
        }

        configChanged |= ImGui.Checkbox("Auto Suppress Warnings", ref config.AutoSuppress);
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Automatically suppress warnings for other players after a set time");
        }

        if (config.AutoSuppress) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.InputInt("seconds", ref config.AutoSuppressTime);
            if (config.AutoSuppressTime < 1) config.AutoSuppressTime = 1;
        }

        if (configChanged) {
            config.Save();
        }
    }

    private void DrawBlacklistTab() {
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), "Zone Blacklist");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("Warnings will not be shown in blacklisted zones.");
        ImGui.Spacing();

        // Current zone
        var currentZone = Services.ClientState.TerritoryType;
        var currentZoneName = GetZoneName(currentZone);
        var isBlacklisted = System.BlacklistController.Config.BlacklistedZones.Contains(currentZone);

        ImGui.Text($"Current Zone: {currentZoneName}");

        if (isBlacklisted) {
            if (ImGui.Button("Remove Current Zone from Blacklist")) {
                System.BlacklistController.Config.BlacklistedZones.Remove(currentZone);
                System.BlacklistController.Config.Save();
            }
        }
        else {
            if (ImGui.Button("Add Current Zone to Blacklist")) {
                System.BlacklistController.Config.BlacklistedZones.Add(currentZone);
                System.BlacklistController.Config.Save();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // List of blacklisted zones
        ImGui.Text("Blacklisted Zones:");

        using var child = ImRaii.Child("BlacklistList", Vector2.Zero, true);
        if (!child) return;

        var zonesToRemove = System.BlacklistController.Config.BlacklistedZones.ToList();
        foreach (var zoneId in zonesToRemove) {
            using var id = ImRaii.PushId((int)zoneId);

            using (ImRaii.PushFont(UiBuilder.IconFont)) {
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString())) {
                    System.BlacklistController.Config.BlacklistedZones.Remove(zoneId);
                    System.BlacklistController.Config.Save();
                }
            }

            ImGui.SameLine();
            ImGui.Text(GetZoneName(zoneId));
        }

        if (System.BlacklistController.Config.BlacklistedZones.Count == 0) {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No zones blacklisted");
        }
    }

    private static string GetZoneName(uint territoryId) {
        var territory = Services.DataManager.GetExcelSheet<TerritoryType>().GetRowOrDefault(territoryId);
        if (territory is null) return $"Unknown ({territoryId})";

        var placeName = territory.Value.PlaceName.ValueNullable?.Name.ToString();
        return string.IsNullOrEmpty(placeName) ? $"Zone {territoryId}" : placeName;
    }

}
