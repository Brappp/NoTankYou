using System;
using System.ComponentModel;

namespace BuffAlert.Classes;

public enum ModuleCategory {
    Tank,
    Healer,
    DPS,
    Gatherer,
    General,
}

[AttributeUsage(AttributeTargets.Field)]
public class ModuleIconAttribute(uint moduleIcon) : Attribute {
    public uint ModuleIcon { get; } = moduleIcon;
}

[AttributeUsage(AttributeTargets.Field)]
public class ModuleCategoryAttribute(ModuleCategory category) : Attribute {
    public ModuleCategory Category { get; } = category;
}

public enum ModuleName {
    [Description("Tank Stance")]
    [ModuleIcon(62019)]
    [ModuleCategory(ModuleCategory.Tank)]
    Tanks,

    [Description("Blue Mage")]
    [ModuleIcon(62036)]
    [ModuleCategory(ModuleCategory.DPS)]
    BlueMage,

    [Description("Dancer")]
    [ModuleIcon(62038)]
    [ModuleCategory(ModuleCategory.DPS)]
    Dancer,

    [Description("Free Company")]
    [ModuleIcon(60460)]
    [ModuleCategory(ModuleCategory.General)]
    FreeCompany,

    [Description("Food")]
    [ModuleIcon(62015)]
    [ModuleCategory(ModuleCategory.General)]
    Food,

    [Description("Spiritbond")]
    [ModuleIcon(62014)]
    [ModuleCategory(ModuleCategory.General)]
    SpiritBond,

    [Description("Sage")]
    [ModuleIcon(62040)]
    [ModuleCategory(ModuleCategory.Healer)]
    Sage,

    [Description("Scholar")]
    [ModuleIcon(62028)]
    [ModuleCategory(ModuleCategory.Healer)]
    Scholar,

    [Description("Summoner")]
    [ModuleIcon(62027)]
    [ModuleCategory(ModuleCategory.DPS)]
    Summoner,

    [Description("Chocobo")]
    [ModuleIcon(62043)]
    [ModuleCategory(ModuleCategory.General)]
    Chocobo,

    [Description("Gatherers")]
    [ModuleIcon(62017)]
    [ModuleCategory(ModuleCategory.Gatherer)]
    Gatherers,

    [Description("Monk")]
    [ModuleIcon(62020)]
    [ModuleCategory(ModuleCategory.DPS)]
    Monk,

    [Description("Pictomancer")]
    [ModuleIcon(62042)]
    [ModuleCategory(ModuleCategory.DPS)]
    Pictomancer,

    [Description("Reaper")]
    [ModuleIcon(62039)]
    [ModuleCategory(ModuleCategory.DPS)]
    Reaper,
}