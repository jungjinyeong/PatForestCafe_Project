using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 커피머신(UIPopupDrinkRecipeProduction)과 가공섬 레시피 연구소(UIPopupRecipeLab)이 공유하는 재료 슬롯/소모 계산.
public static class DrinkCraftHelper
{
    public static List<int> GetFilledTids(IEnumerable<UIDrinkMaterialSlot> slots)
    {
        return slots.Where(s => s.IsFilled).Select(s => s.MaterialTid).ToList();
    }

    public static Dictionary<int, int> CountMaterials(IEnumerable<int> materialTids)
    {
        var counts = new Dictionary<int, int>();
        foreach (int tid in materialTids)
        {
            counts.TryGetValue(tid, out int count);
            counts[tid] = count + 1;
        }
        return counts;
    }

    public static string BuildTagText(IEnumerable<int> materialTids)
    {
        var tags = PickupDrinkData.CollectTags(materialTids);
        return tags.Count > 0 ? string.Join(", ", tags) : "없음";
    }

    // 재료 선택 모달 목록. 얼음은 자동 소모라 제외하고, 남은 수량 = 보유 - 모든 슬롯에 예약된 수량
    // (편집 중인 슬롯에 이미 든 재료는 교체 대상이므로 예약량에서 뺀다).
    public static List<UIScrollDrinkMaterialData> BuildPickerList(IEnumerable<UIDrinkMaterialSlot> allSlots, UIDrinkMaterialSlot editingSlot)
    {
        var list = new List<UIScrollDrinkMaterialData>();
        var materialTable = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (materialTable == null)
            return list;

        var reserved = CountMaterials(GetFilledTids(allSlots));

        foreach (var row in materialTable.All.Values.OrderBy(r => r.Tid))
        {
            if (row.Tid == DrinkModel.ICE_MATERIAL_TID)
                continue;

            int owned = GameInstance.Model.Material.Get(row.Tid)?.Count.Value ?? 0;
            reserved.TryGetValue(row.Tid, out int used);
            if (editingSlot != null && editingSlot.MaterialTid == row.Tid)
                used--;

            list.Add(new UIScrollDrinkMaterialData
            {
                Tid = row.Tid,
                Name = row.Name,
                RemainCount = Mathf.Max(0, owned - used),
            });
        }

        return list;
    }

    // 슬롯 재료 + 레시피 얼음 자동 소모분을 합친 필요 재료.
    public static Dictionary<int, int> BuildRequiredMaterials(IEnumerable<int> materialTids, int iceCount)
    {
        var required = CountMaterials(materialTids);
        if (iceCount > 0)
        {
            required.TryGetValue(DrinkModel.ICE_MATERIAL_TID, out int ice);
            required[DrinkModel.ICE_MATERIAL_TID] = ice + iceCount;
        }
        return required;
    }

    // 부족한 재료가 있으면 그 이름을 돌려준다.
    public static bool TryFindShortage(IReadOnlyDictionary<int, int> required, out string materialName)
    {
        foreach (var pair in required)
        {
            if (!GameInstance.Model.Material.HasEnough(pair.Key, pair.Value))
            {
                materialName = GameInstance.Table.Get<CTable.DrinkMaterialRow>(pair.Key)?.Name ?? pair.Key.ToString();
                return true;
            }
        }

        materialName = null;
        return false;
    }

    public static void Consume(IReadOnlyDictionary<int, int> required)
    {
        foreach (var pair in required)
            GameInstance.Model.Material.Consume(pair.Key, pair.Value);
    }
}
