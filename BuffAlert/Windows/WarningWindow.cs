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

public class WarningWindow : Window {
    private const float IconSpacing = 4f;

    private float IconSize => System.SystemConfig?.SoloIconSize ?? 32f;

    // Test mode action IDs
    private const uint TestActionId1 = 28;      // Iron Will (tank stance)
    private const uint TestActionId2 = 16006;   // Closed Position (dancer)

    private List<WarningState> activeWarnings = [];

    public WarningWindow() : base("##BuffAlertWarnings",
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
        // Only show warnings for self (solo mode bar)
        var localPlayerId = Services.ObjectTable.LocalPlayer?.EntityId ?? 0;
        activeWarnings = warnings
            .Where(w => w.SourceEntityId == localPlayerId)
            .Where(w => System.ModuleController.GetModule(w.SourceModule)?.GetActionDisplaySettings(w.ActionId)?.ShowInSolo != false)
            .GroupBy(w => (w.SourceModule, w.SourceEntityId))
            .Select(g => g.OrderByDescending(w => w.Priority).First())
            .OrderByDescending(w => w.Priority)
            .ToList();
    }

    public override bool DrawConditions() {
        if (!Services.ClientState.IsLoggedIn) return false;
        if (System.SystemConfig?.Enabled != true) return false;
        if (System.SystemConfig?.SoloMode != true) return false;

        // In test mode, always show
        if (System.SystemConfig.TestMode) return true;

        if (System.ShouldHideSoloForCombat()) return false;
        if (activeWarnings.Count == 0) return false;

        return true;
    }

    public override void PreDraw() {
        // Always allow mouse input so icons can be clicked
        Flags &= ~ImGuiWindowFlags.NoMouseInputs;

        // Configurable background color
        var config = System.SystemConfig;
        if (config != null) {
            var bgColor = new Vector4(config.SoloBgColor_R, config.SoloBgColor_G, config.SoloBgColor_B, config.SoloBgColor_A);
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
        var warnings = System.SystemConfig?.TestMode == true ? CreateTestWarnings() : activeWarnings;
        if (warnings.Count == 0) return;

        foreach (var warning in warnings) {
            DrawWarningIcon(warning);

            // Same line unless this is the last item
            if (warning != warnings[^1]) {
                ImGui.SameLine();
            }
        }
    }

    private List<WarningState> CreateTestWarnings() {
        var actionSheet = Services.DataManager.GetExcelSheet<Action>();
        var warnings = new List<WarningState>();

        var action1 = actionSheet.GetRowOrDefault(TestActionId1);
        if (action1 is not null) {
            warnings.Add(new WarningState {
                IconId = action1.Value.Icon,
                Message = "Test Warning 1",
                IconLabel = action1.Value.Name.ToString(),
                SourcePlayerName = "You",
                Priority = 10,
                ActionId = TestActionId1,
                SourceEntityId = Services.ObjectTable.LocalPlayer?.EntityId ?? 0,
                SourceModule = ModuleName.Tanks,
            });
        }

        var action2 = actionSheet.GetRowOrDefault(TestActionId2);
        if (action2 is not null) {
            warnings.Add(new WarningState {
                IconId = action2.Value.Icon,
                Message = "Test Warning 2",
                IconLabel = action2.Value.Name.ToString(),
                SourcePlayerName = "You",
                Priority = 5,
                ActionId = TestActionId2,
                SourceEntityId = Services.ObjectTable.LocalPlayer?.EntityId ?? 0,
                SourceModule = ModuleName.Dancer,
            });
        }

        return warnings;
    }

    private void DrawWarningIcon(WarningState warning) {
        var texture = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(warning.IconId));
        var wrap = texture.GetWrapOrEmpty();

        if (wrap.Handle == nint.Zero) return;

        var scaledSize = ImGuiHelpers.ScaledVector2(IconSize, IconSize);
        var isSuppressed = System.SuppressionManager.IsModuleSuppressed(warning.SourceModule);

        // Dim the icon if suppressed
        var tint = isSuppressed ? new Vector4(0.4f, 0.4f, 0.4f, 0.7f) : new Vector4(1f, 1f, 1f, 1f);

        var cursorPos = ImGui.GetCursorScreenPos();

        ImGui.Image(wrap.Handle, scaledSize, Vector2.Zero, Vector2.One, tint);

        // Draw mute icon overlay if suppressed
        if (isSuppressed) {
            var drawList = ImGui.GetWindowDrawList();
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

        // Click to toggle suppression for this module
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

            if (!string.IsNullOrEmpty(warning.SourcePlayerName)) {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), warning.SourcePlayerName);
            }

            ImGui.EndTooltip();
        }
    }
}
