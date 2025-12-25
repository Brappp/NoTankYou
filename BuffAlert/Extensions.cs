using System;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace BuffAlert;

public static class ConditionExtensions {
    public static bool IsBoundByDuty(this ICondition condition)
        => condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95);

    public static bool IsInCombat(this ICondition condition)
        => condition.Any(ConditionFlag.InCombat);

    public static bool IsBetweenAreas(this ICondition condition)
        => condition.Any(ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51);

    public static bool IsInCutsceneOrQuestEvent(this ICondition condition)
        => condition.Any(ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.OccupiedInQuestEvent, ConditionFlag.WatchingCutscene);

    public static bool IsCrossWorld(this ICondition condition)
        => condition.Any(ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance);
}

public static class Helpers {
    /// <summary>
    /// Gets an attribute from an enum value
    /// </summary>
    public static T? GetAttribute<T>(this Enum value) where T : Attribute {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString());
        if (memberInfo.Length > 0) {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(T), false);
            if (attrs.Length > 0) {
                return (T)attrs[0];
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the Description attribute value from an enum, or the enum name if not present
    /// </summary>
    public static string GetDescription(this Enum value) {
        var attr = value.GetAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    /// <summary>
    /// Converts a KnownColor to a Vector4 for ImGui
    /// </summary>
    public static Vector4 Vector(this KnownColor color) {
        var c = Color.FromKnownColor(color);
        return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    }

    /// <summary>
    /// Gets the current duty type
    /// </summary>
    public static unsafe DutyType GetCurrentDutyType(this IDataManager dataManager) {
        var cfc = dataManager.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault(GameMain.Instance()->CurrentContentFinderConditionId);
        return cfc is null ? DutyType.Other : GetDutyType(cfc.Value);
    }

    private static DutyType GetDutyType(ContentFinderCondition cfc)
        => cfc switch {
            { ContentType.RowId: 5, ContentMemberType.RowId: 4 } => DutyType.Alliance,
            { ContentType.RowId: 5 } => DutyType.Raid,
            _ => DutyType.Other,
        };
}

public enum DutyType {
    Other,
    Alliance,
    Raid,
}
