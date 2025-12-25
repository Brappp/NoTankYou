using Lumina.Excel.Sheets;
using BuffAlert.Classes;

namespace BuffAlert.Modules;

public class Food : ConsumableModule<FoodConfiguration> {
    public override ModuleName ModuleName => ModuleName.Food;
    protected override string DefaultWarningText => "Food Warning";

    private const uint FoodItemId = 30482; // High-quality food item for icon
    private uint? _iconId;
    protected override uint IconId => _iconId ??= Services.DataManager.GetExcelSheet<Item>().GetRowOrDefault(FoodItemId)?.Icon ?? 0;
    protected override string IconLabel => "Food";
    protected override uint StatusId => 48; // Well Fed
}

public class FoodConfiguration() : ConsumableConfiguration(ModuleName.Food);
