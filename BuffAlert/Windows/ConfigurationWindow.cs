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
using BuffAlert.Configuration;
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

    public ConfigurationWindow() : base("BuffAlert - Configuration", ImGuiWindowFlags.None) {
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

        // Status indicator
        DrawStatusIndicator();

        ImGui.Spacing();

        // Legend for S/P/O columns
        DrawColumnLegend();

        ImGui.Spacing();
        ImGui.Separator();

        // Modules grouped by category
        var modulesByCategory = System.ModuleController.Modules
            .GroupBy(m => m.ModuleName.GetAttribute<ModuleCategoryAttribute>()?.Category ?? ModuleCategory.General)
            .OrderBy(g => g.Key);

        foreach (var categoryGroup in modulesByCategory) {
            DrawCategorySection(categoryGroup.Key, categoryGroup.ToList());
        }
    }

    private void DrawStatusIndicator() {
        var warningCount = System.ActiveWarnings.Count;
        var soloSuppressed = System.SuppressionManager.IsDisplayModeSuppressed(DisplayMode.Solo);
        var partyFrameSuppressed = System.SuppressionManager.IsDisplayModeSuppressed(DisplayMode.PartyFrame);
        var overlaySuppressed = System.SuppressionManager.IsDisplayModeSuppressed(DisplayMode.PartyOverlay);

        // Status text
        if (warningCount == 0) {
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 0.5f, 1f), "No active warnings");
        }
        else {
            var warningText = warningCount == 1 ? "1 warning active" : $"{warningCount} warnings active";
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), warningText);
        }

        // Suppression status
        if (soloSuppressed || partyFrameSuppressed || overlaySuppressed) {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), " | Hidden:");

            if (soloSuppressed) {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.4f, 0.7f, 1f, 0.7f), "S");
            }
            if (partyFrameSuppressed) {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1f, 0.7f, 0.4f, 0.7f), "P");
            }
            if (overlaySuppressed) {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.7f, 1f, 0.4f, 0.7f), "O");
            }
        }
    }

    private void DrawColumnLegend() {
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Display toggles:");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1f, 1f), "S");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Solo");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.4f, 1f), "P");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Party Frame");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 1f, 0.4f, 1f), "O");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "Overlay");
    }

    private void DrawCategorySection(ModuleCategory category, List<ModuleBase> modules) {
        var (categoryName, iconId, color) = CategoryInfo[category];

        // Collapsible header for each category
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(color.X * 0.3f, color.Y * 0.3f, color.Z * 0.3f, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(color.X * 0.4f, color.Y * 0.4f, color.Z * 0.4f, 0.7f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(color.X * 0.5f, color.Y * 0.5f, color.Z * 0.5f, 0.9f));

        var isOpen = ImGui.CollapsingHeader($"{categoryName} ({modules.Count})###{category}", ImGuiTreeNodeFlags.DefaultOpen);

        ImGui.PopStyleColor(3);

        if (isOpen) {
            using (ImRaii.PushIndent(8f)) {
                foreach (var module in modules) {
                    DrawModuleEntry(module);
                }
            }
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

        // Show module requirement indicators
        if (module.SelfOnly) {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "(self only)");
        }
        else if (module.RequiresParty) {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.5f, 0.9f, 1f), "(party 2+)");
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("This module only activates when in a party of 2 or more");
            }
        }

        // Per-action display settings (when module is enabled)
        if (module.IsEnabled && module.CheckedActionIds.Length > 0) {
            var actionSheet = Services.DataManager.GetExcelSheet<Action>();
            var configChanged = false;
            var isSelfOnly = module.SelfOnly;

            // Colors matching the section headers
            var soloColor = new Vector4(0.4f, 0.7f, 1f, 1f);      // Blue
            var partyFrameColor = new Vector4(1f, 0.7f, 0.4f, 1f); // Orange
            var overlayColor = new Vector4(0.7f, 1f, 0.4f, 1f);    // Green

            using (ImRaii.PushIndent(28f)) {
                // Table for action settings - only show S column for self-only modules
                var columnCount = isSelfOnly ? 2 : 4;
                using (var table = ImRaii.Table($"Actions_{module.ModuleName}", columnCount, ImGuiTableFlags.BordersInnerH)) {
                    if (table) {
                        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("S", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                        if (!isSelfOnly) {
                            ImGui.TableSetupColumn("P", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                            ImGui.TableSetupColumn("O", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                        }

                        // Custom header row with colored text
                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                        ImGui.TableNextColumn();
                        ImGui.Text("Action");

                        ImGui.TableNextColumn();
                        ImGui.TextColored(soloColor, "S");
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Solo Warnings - your own warning bar");

                        if (!isSelfOnly) {
                            ImGui.TableNextColumn();
                            ImGui.TextColored(partyFrameColor, "P");
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Party Frame - party members' warning bar");

                            ImGui.TableNextColumn();
                            ImGui.TextColored(overlayColor, "O");
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Party Overlay - icons above party members' heads");
                        }

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

                            // S column (Solo - always shown)
                            ImGui.TableNextColumn();
                            configChanged |= ImGui.Checkbox("##S", ref settings.ShowInSolo);
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show in Solo warnings bar");

                            // P and O columns only for party-capable modules
                            if (!isSelfOnly) {
                                // P column (Party Frame)
                                ImGui.TableNextColumn();
                                configChanged |= ImGui.Checkbox("##P", ref settings.ShowInPartyFrame);
                                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show in Party Frame bar");

                                // O column (Party Overlay)
                                ImGui.TableNextColumn();
                                configChanged |= ImGui.Checkbox("##O", ref settings.ShowInPartyOverlay);
                                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show above party members' heads");
                            }
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
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Show test warnings to position the UI elements");

        ImGui.Spacing();
        ImGui.Separator();

        // ===== SOLO WARNINGS (S) =====
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1f, 1f), "Solo Warnings (S)");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Warning bar for your own missing buffs");

        configChanged |= ImGui.Checkbox("Enabled##Solo", ref config.SoloMode);
        if (config.SoloMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("Size##Solo", ref config.SoloIconSize, 1f, 16f, 64f, "%.0f");

            ImGui.SameLine();
            var soloBgColor = new Vector4(config.SoloBgColor_R, config.SoloBgColor_G, config.SoloBgColor_B, config.SoloBgColor_A);
            if (ImGui.ColorEdit4("BG##Solo", ref soloBgColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar)) {
                config.SoloBgColor_R = soloBgColor.X;
                config.SoloBgColor_G = soloBgColor.Y;
                config.SoloBgColor_B = soloBgColor.Z;
                config.SoloBgColor_A = soloBgColor.W;
                configChanged = true;
            }

            // Suppress mode
            using (ImRaii.PushIndent()) {
                ImGui.Text("Hide:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
                var soloSuppressMode = (int)config.SoloSuppressMode;
                if (ImGui.Combo("##SoloSuppress", ref soloSuppressMode, "Never\0After Time\0On Combat\0")) {
                    config.SoloSuppressMode = (SuppressMode)soloSuppressMode;
                    configChanged = true;
                }
                if (config.SoloSuppressMode != SuppressMode.Never) {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                    configChanged |= ImGui.InputInt("sec##SoloDelay", ref config.SoloSuppressDelay);
                    if (config.SoloSuppressDelay < 0) config.SoloSuppressDelay = 0;
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // ===== PARTY FRAME (P) =====
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.4f, 1f), "Party Frame (P)");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Warning bar for party members' missing buffs");

        configChanged |= ImGui.Checkbox("Enabled##PartyFrame", ref config.PartyFrameMode);
        if (config.PartyFrameMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("Size##PartyFrame", ref config.PartyFrameIconSize, 1f, 16f, 64f, "%.0f");

            ImGui.SameLine();
            var partyBgColor = new Vector4(config.PartyFrameBgColor_R, config.PartyFrameBgColor_G, config.PartyFrameBgColor_B, config.PartyFrameBgColor_A);
            if (ImGui.ColorEdit4("BG##PartyFrame", ref partyBgColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar)) {
                config.PartyFrameBgColor_R = partyBgColor.X;
                config.PartyFrameBgColor_G = partyBgColor.Y;
                config.PartyFrameBgColor_B = partyBgColor.Z;
                config.PartyFrameBgColor_A = partyBgColor.W;
                configChanged = true;
            }

            // Suppress mode
            using (ImRaii.PushIndent()) {
                ImGui.Text("Hide:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
                var partyFrameSuppressMode = (int)config.PartyFrameSuppressMode;
                if (ImGui.Combo("##PartyFrameSuppress", ref partyFrameSuppressMode, "Never\0After Time\0On Combat\0")) {
                    config.PartyFrameSuppressMode = (SuppressMode)partyFrameSuppressMode;
                    configChanged = true;
                }
                if (config.PartyFrameSuppressMode != SuppressMode.Never) {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                    configChanged |= ImGui.InputInt("sec##PartyFrameDelay", ref config.PartyFrameSuppressDelay);
                    if (config.PartyFrameSuppressDelay < 0) config.PartyFrameSuppressDelay = 0;
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // ===== PARTY OVERLAY (O) =====
        ImGui.TextColored(new Vector4(0.7f, 1f, 0.4f, 1f), "Party Overlay (O)");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), "Icons floating above party members' heads");

        configChanged |= ImGui.Checkbox("Enabled##PartyOverlay", ref config.PartyOverlayMode);
        if (config.PartyOverlayMode) {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("Size##PartyOverlay", ref config.PartyOverlayIconSize, 1f, 16f, 64f, "%.0f");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ImGuiHelpers.GlobalScale);
            configChanged |= ImGui.DragFloat("Height##PartyOverlay", ref config.PartyOverlayHeightOffset, 0.1f, 0f, 5f, "%.1f");

            // Suppress mode
            using (ImRaii.PushIndent()) {
                ImGui.Text("Hide:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(120 * ImGuiHelpers.GlobalScale);
                var partyOverlaySuppressMode = (int)config.PartyOverlaySuppressMode;
                if (ImGui.Combo("##PartyOverlaySuppress", ref partyOverlaySuppressMode, "Never\0After Time\0On Combat\0")) {
                    config.PartyOverlaySuppressMode = (SuppressMode)partyOverlaySuppressMode;
                    configChanged = true;
                }
                if (config.PartyOverlaySuppressMode != SuppressMode.Never) {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
                    configChanged |= ImGui.InputInt("sec##PartyOverlayDelay", ref config.PartyOverlaySuppressDelay);
                    if (config.PartyOverlaySuppressDelay < 0) config.PartyOverlaySuppressDelay = 0;
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // ===== CONDITIONS =====
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Conditions");

        configChanged |= ImGui.Checkbox("Only in Duties", ref config.OnlyInDuties);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Only show warnings while in a duty (after countdown finishes)");

        configChanged |= ImGui.Checkbox("Hide in Quest Events", ref config.HideInQuestEvent);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hide warnings during cutscenes and quest events");

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
