using System.Collections.Generic;
using System.Linq;
using Extension;

// 커피머신에서 완성해 픽업대에 올려둔 음료. DrinkTid == 0이면 레시피와 맞지 않은 "실패한 음료".
public class PickupDrinkData
{
    public int DrinkTid { get; private set; }
    public CTable.eDrinkTempType Temp { get; private set; }
    public IReadOnlyList<int> BaseMaterialTids { get; private set; }
    public IReadOnlyList<int> CustomMaterialTids { get; private set; }

    // 기본+커스텀 재료 속성의 합집합. 생성 시 1회 계산.
    public IReadOnlyCollection<string> Tags { get; private set; }

    public bool IsFailed => DrinkTid == 0;

    public static PickupDrinkData Create(int drinkTid, CTable.eDrinkTempType temp, IEnumerable<int> baseMaterialTids, IEnumerable<int> customMaterialTids)
    {
        var baseList = baseMaterialTids?.ToList() ?? new List<int>();
        var customList = customMaterialTids?.ToList() ?? new List<int>();

        return new PickupDrinkData
        {
            DrinkTid = drinkTid,
            Temp = temp,
            BaseMaterialTids = baseList,
            CustomMaterialTids = customList,
            Tags = CollectTags(baseList.Concat(customList)),
        };
    }

    public static HashSet<string> CollectTags(IEnumerable<int> materialTids)
    {
        var tags = new HashSet<string>();
        var table = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (table == null) return tags;

        foreach (int tid in materialTids)
            tags.UnionWith(table.Get(tid).GetTags());

        return tags;
    }
}
