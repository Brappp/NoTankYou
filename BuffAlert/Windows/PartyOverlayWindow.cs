using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using BuffAlert.Classes;

namespace BuffAlert.Windows;

public class PartyOverlayWindow : Window {
    private float IconSize => System.SystemConfig?.PartyOverlayIconSize ?? 32f;
    private float HeightOffset => System.SystemConfig?.PartyOverlayHeightOffset ?? 2.5f;

    // Test mode action ID
    private const uint TestActionId = 24285; // Kardia (sage)

    private List<WarningState> partyWarnings = [];

    public PartyOverlayWindow() : base("##BuffAlertPartyOverlay",
        ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoBackground |
        ImGuiWindowFlags.NoSavedSettings |
        ImGuiWindowFlags.NoFocusOnAppearing |
        ImGuiWindowFlags.NoNav) {

        RespectCloseHotkey = false;
        IsOpen = true;
    }

    public override void PreDraw() {
        // Make window cover entire screen
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw() {
        ImGui.PopStyleVar();
    }

    public void UpdateWarnings(List<WarningState> warnings) {
        // Only keep warnings for party members (not self)
        var localPlayerId = Services.ObjectTable.LocalPlayer?.EntityId ?? 0;
        partyWarnings = warnings
            .Where(w => w.SourceEntityId != localPlayerId && w.SourceEntityId != 0)
            .Where(w => System.ModuleController.GetModule(w.SourceModule)?.GetActionDisplaySettings(w.ActionId)?.ShowInPartyOverlay != false)
            .GroupBy(w => w.SourceEntityId)
            .Select(g => g.OrderByDescending(w => w.Priority).First())
            .ToList();
    }

    public override bool DrawConditions() {
        if (!Services.ClientState.IsLoggedIn) return false;
        if (System.SystemConfig?.Enabled != true) return false;
        if (System.SystemConfig?.PartyOverlayMode != true) return false;

        // In test mode, always show (we'll draw above self)
        if (System.SystemConfig.TestMode) return true;

        if (System.ShouldHidePartyOverlayForCombat()) return false;
        if (partyWarnings.Count == 0) return false;

        return true;
    }

    public override void Draw() {
        var drawList = ImGui.GetWindowDrawList();

        if (System.SystemConfig?.TestMode == true) {
            // In test mode, draw above the local player
            DrawTestWarning(drawList);
        }
        else {
            foreach (var warning in partyWarnings) {
                // Skip suppressed warnings
                if (System.SuppressionManager.IsSuppressed(warning)) continue;
                DrawWarningAbovePlayer(drawList, warning);
            }
        }
    }

    private void DrawTestWarning(ImDrawListPtr drawList) {
        var localPlayer = Services.ObjectTable.LocalPlayer;
        if (localPlayer is null) return;

        var actionSheet = Services.DataManager.GetExcelSheet<Action>();
        var action = actionSheet.GetRowOrDefault(TestActionId);
        if (action is null) return;

        var testWarning = new WarningState {
            IconId = action.Value.Icon,
            Message = "Test Party Warning",
            IconLabel = action.Value.Name.ToString(),
            SourcePlayerName = "Party Member",
            Priority = 5,
            ActionId = TestActionId,
            SourceEntityId = localPlayer.EntityId,
            SourceModule = ModuleName.Sage,
        };

        DrawWarningAbovePlayer(drawList, testWarning);
    }

    private void DrawWarningAbovePlayer(ImDrawListPtr drawList, WarningState warning) {
        // Find the game object for this entity
        var gameObject = Services.ObjectTable.FirstOrDefault(o => o.EntityId == warning.SourceEntityId);
        if (gameObject is null) return;

        // Get position above the player's head
        var worldPos = gameObject.Position with { Y = gameObject.Position.Y + HeightOffset };

        // Convert world position to screen position
        if (!Services.GameGui.WorldToScreen(worldPos, out var screenPos)) return;

        // Load and draw the icon
        var texture = Services.TextureProvider.GetFromGameIcon(new GameIconLookup(warning.IconId));
        var wrap = texture.GetWrapOrEmpty();

        if (wrap.Handle == nint.Zero) return;

        var scaledSize = ImGuiHelpers.ScaledVector2(IconSize, IconSize);
        var halfSize = scaledSize / 2f;

        // Center the icon on the screen position
        var iconMin = new Vector2(screenPos.X - halfSize.X, screenPos.Y - halfSize.Y);
        var iconMax = new Vector2(screenPos.X + halfSize.X, screenPos.Y + halfSize.Y);

        var white = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddImage(wrap.Handle, iconMin, iconMax, Vector2.Zero, Vector2.One, white);

        // Draw player name below the icon (only in test mode to keep real overlay clean)
        if (System.SystemConfig?.TestMode == true && !string.IsNullOrEmpty(warning.SourcePlayerName)) {
            var textSize = ImGui.CalcTextSize(warning.SourcePlayerName);
            var textPos = new Vector2(screenPos.X - textSize.X / 2f, iconMax.Y + 2f);

            // Draw text shadow for readability
            drawList.AddText(textPos + Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)), warning.SourcePlayerName);
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), warning.SourcePlayerName);
        }
    }
}
