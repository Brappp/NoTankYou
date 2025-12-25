using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using BuffAlert.Classes;
using Vector4 = System.Numerics.Vector4;

namespace BuffAlert.Windows;

public class PartyFrameWindow : Window {
    private const float IconSpacing = 4f;

    private float IconSize => System.SystemConfig?.PartyFrameIconSize ?? 32f;

    // Test mode action IDs
    private const uint TestActionId1 = 24285;  // Kardia (sage)
    private const uint TestActionId2 = 7396;   // Summon Eos (scholar)

    private List<(WarningState Warning, int Count)> partyWarnings = [];

    public PartyFrameWindow() : base("##BuffAlertPartyFrame",
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoFocusOnAppearing |
        ImGuiWindowFlags.NoNav) {

        RespectCloseHotkey = false;
        IsOpen = true;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(0, 0),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void UpdateWarnings(List<WarningState> warnings) {
        // Only keep warnings for party members (not self)
        var localPlayerId = Services.ObjectTable.LocalPlayer?.EntityId ?? 0;

        // Group by module+action to aggregate (one Food icon, not 10)
        // Use IconId as fallback key when ActionId is 0 (consumables)
        // Count how many unique players have each warning type
        partyWarnings = warnings
            .Where(w => w.SourceEntityId != localPlayerId && w.SourceEntityId != 0)
            .Where(w => System.ModuleController.GetModule(w.SourceModule)?.GetActionDisplaySettings(w.ActionId)?.ShowInPartyFrame != false)
            .GroupBy(w => (w.SourceModule, Key: w.ActionId != 0 ? w.ActionId : w.IconId))
            .Select(g => (Warning: g.First(), Count: g.Select(w => w.SourceEntityId).Distinct().Count()))
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Warning.Priority)
            .ToList();
    }

    public override bool DrawConditions() {
        if (!Services.ClientState.IsLoggedIn) return false;
        if (System.SystemConfig?.Enabled != true) return false;
        if (System.SystemConfig?.PartyFrameMode != true) return false;

        // In test mode, always show
        if (System.SystemConfig.TestMode) return true;

        if (System.ShouldHideDisplayMode(DisplayMode.PartyFrame)) return false;
        if (partyWarnings.Count == 0) return false;

        return true;
    }

    public override void PreDraw() {
        // Always allow mouse input so icons can be clicked
        Flags &= ~ImGuiWindowFlags.NoMouseInputs;

        // Configurable background color
        var config = System.SystemConfig;
        if (config != null) {
            var bgColor = new Vector4(config.PartyFrameBgColor_R, config.PartyFrameBgColor_G, config.PartyFrameBgColor_B, config.PartyFrameBgColor_A);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);
        }

        // Minimal padding for compact look
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f, 4f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(IconSpacing, 0f));
    }

    public override void PostDraw() {
        ImGui.PopStyleVar(2);
        if (System.SystemConfig != null) {
            ImGui.PopStyleColor();
        }
    }

    public override void Draw() {
        var warnings = System.SystemConfig?.TestMode == true ? CreateTestWarnings() : partyWarnings;
        if (warnings.Count == 0) return;

        for (var i = 0; i < warnings.Count; i++) {
            DrawWarningIcon(warnings[i].Warning, warnings[i].Count);

            // Same line unless this is the last item
            if (i < warnings.Count - 1) {
                ImGui.SameLine();
            }
        }
    }

    private List<(WarningState Warning, int Count)> CreateTestWarnings() {
        var actionSheet = Services.DataManager.GetExcelSheet<Action>();
        var warnings = new List<(WarningState, int)>();

        var action1 = actionSheet.GetRowOrDefault(TestActionId1);
        if (action1 is not null) {
            warnings.Add((new WarningState {
                IconId = action1.Value.Icon,
                Message = "Test Party Warning 1",
                IconLabel = action1.Value.Name.ToString(),
                SourcePlayerName = "3 players",
                Priority = 10,
                ActionId = TestActionId1,
                SourceEntityId = 1,
                SourceModule = ModuleName.Sage,
            }, 3));
        }

        var action2 = actionSheet.GetRowOrDefault(TestActionId2);
        if (action2 is not null) {
            warnings.Add((new WarningState {
                IconId = action2.Value.Icon,
                Message = "Test Party Warning 2",
                IconLabel = action2.Value.Name.ToString(),
                SourcePlayerName = "1 player",
                Priority = 5,
                ActionId = TestActionId2,
                SourceEntityId = 2,
                SourceModule = ModuleName.Scholar,
            }, 1));
        }

        return warnings;
    }

    private void DrawWarningIcon(WarningState warning, int count) {
        var texture = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(warning.IconId));
        var wrap = texture.GetWrapOrEmpty();

        if (wrap.Handle == nint.Zero) return;

        var scaledSize = ImGuiHelpers.ScaledVector2(IconSize, IconSize);
        var isSuppressed = System.SuppressionManager.IsModuleSuppressed(warning.SourceModule);

        // Dim the icon if suppressed
        var tint = isSuppressed ? new Vector4(0.4f, 0.4f, 0.4f, 0.7f) : new Vector4(1f, 1f, 1f, 1f);

        var cursorPos = ImGui.GetCursorScreenPos();

        ImGui.Image(wrap.Handle, scaledSize, Vector2.Zero, Vector2.One, tint);

        var drawList = ImGui.GetWindowDrawList();

        // Draw count badge in bottom-right corner
        if (count > 1) {
            var countText = count.ToString();
            var fontSize = scaledSize.X * 0.4f;
            var badgeRadius = scaledSize.X * 0.3f;
            var badgeCenter = cursorPos + new Vector2(scaledSize.X - badgeRadius * 0.7f, scaledSize.Y - badgeRadius * 0.7f);

            // Badge background
            var badgeColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 1f));
            drawList.AddCircleFilled(badgeCenter, badgeRadius, badgeColor);

            // Badge text
            var textSize = ImGui.CalcTextSize(countText);
            var textPos = badgeCenter - textSize / 2f;
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)), countText);
        }

        // Draw mute icon overlay if suppressed
        if (isSuppressed) {
            var iconCenter = cursorPos + scaledSize / 2f;
            var muteSize = scaledSize.X * 0.5f;

            // Draw a "no" symbol (circle with line through it)
            var red = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.2f, 0.2f, 0.9f));
            drawList.AddCircle(iconCenter, muteSize, red, 0, 3f);
            drawList.AddLine(
                iconCenter + new Vector2(-muteSize * 0.7f, -muteSize * 0.7f),
                iconCenter + new Vector2(muteSize * 0.7f, muteSize * 0.7f),
                red, 3f);
        }

        // Click to toggle suppression for entire module (all players)
        if (ImGui.IsItemClicked()) {
            System.SuppressionManager.ToggleModuleSuppression(warning.SourceModule);
        }

        if (ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();

            if (isSuppressed) {
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "[MUTED] Click to unmute");
            }
            else {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Click to mute");
            }

            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), warning.Message);

            if (!string.IsNullOrEmpty(warning.IconLabel)) {
                ImGui.Text(warning.IconLabel);
            }

            var playerText = count == 1 ? "1 player" : $"{count} players";
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), playerText);

            ImGui.EndTooltip();
        }
    }
}
