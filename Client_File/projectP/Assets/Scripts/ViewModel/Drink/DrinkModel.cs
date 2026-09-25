using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DrinkModel : IModelBase
{
    // 얼음은 커피머신 제작에서 재료 슬롯에 넣지 않고, ICE 음료 제작 시 레시피에 적힌 개수만큼 자동 소모한다.
    public const int ICE_MATERIAL_TID = 100008;

    private readonly Dictionary<int, DrinkData> mDicDrinks = new();

    // (얼음 제외 재료 조합 서명 "Tid:Count,..." + 온도) → Tid. 재료 조합+온도 하나당 음료 하나를 가정한다.
    // 아이스/핫 음료가 같은 재료를 쓰는 경우(예: 아메리카노)가 있어 온도까지 키에 포함해야 구분된다.
    // 테이블 로드 시 1회만 구축(런타임에 DrinkTable이 바뀌지 않으므로 무효화 불필요).
    private readonly Dictionary<string, int> mRecipeSignatureToTid = new();

    // 음료별 얼음 제외 재료 목록(중복 포함)과 자동 소모할 얼음 개수.
    private readonly Dictionary<int, List<int>> mBaseMaterialTids = new();
    private readonly Dictionary<int, int> mIceCounts = new();

    public DrinkData DefaultDrink { get; private set; }

    public void Init()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (group == null)
        {
            Logger.Warning("[DrinkModel] DrinkGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
        {
            mDicDrinks[row.Tid] = DrinkData.Create(row);

            var baseMaterials = new List<int>();
            int iceCount = 0;
            foreach (int materialTid in GetMaterialTids(row))
            {
                if (materialTid == ICE_MATERIAL_TID) iceCount++;
                else baseMaterials.Add(materialTid);
            }

            mBaseMaterialTids[row.Tid] = baseMaterials;
            mIceCounts[row.Tid] = iceCount;

            if (baseMaterials.Count == 0)
                continue;

            string signature = BuildSignature(CountMaterials(baseMaterials), row.DrinkTempType);
            if (mRecipeSignatureToTid.ContainsKey(signature))
                Logger.Warning($"[DrinkModel] 재료 조합+온도가 중복된 DrinkRow가 있습니다. Tid={row.Tid}");
            else
                mRecipeSignatureToTid[signature] = row.Tid;
        }

        int defaultDrinkTid = GameInstance.Config.GetValue(eConfigType.DefaultDrinkTid);
        DefaultDrink = Get(defaultDrinkTid);
        if (DefaultDrink == null)
            Logger.Warning($"[DrinkModel] 기본 Drink Tid({defaultDrinkTid})를 찾을 수 없습니다.");
    }

    public DrinkData Get(int tableId)
    {
        return mDicDrinks.TryGetValue(tableId, out var drink) ? drink : null;
    }

    public string GetName(int drinkTid)
    {
        return Get(drinkTid)?.MenuItemRow?.Name ?? drinkTid.ToString();
    }

    // 얼음을 제외한 재료 조합(Tid→개수)과 온도가 정확히 일치하는 DrinkRow의 Tid를 찾는다.
    // O(M log M)(M = 선택한 재료 종류 수, ≤5) — 사전 구축된 mRecipeSignatureToTid 덕분에 DrinkRow 개수와 무관하다.
    public bool TryGetRecipeMatch(IReadOnlyDictionary<int, int> baseMaterialCounts, CTable.eDrinkTempType temp, out int drinkTid)
    {
        drinkTid = 0;
        if (baseMaterialCounts == null || baseMaterialCounts.Count == 0)
            return false;

        return mRecipeSignatureToTid.TryGetValue(BuildSignature(baseMaterialCounts, temp), out drinkTid);
    }

    public IReadOnlyList<int> GetBaseMaterialTids(int drinkTid)
    {
        return mBaseMaterialTids.TryGetValue(drinkTid, out var list) ? list : (IReadOnlyList<int>)System.Array.Empty<int>();
    }

    public int GetIceCount(int drinkTid)
    {
        return mIceCounts.TryGetValue(drinkTid, out int count) ? count : 0;
    }

    private static IEnumerable<int> GetMaterialTids(CTable.DrinkRow row)
    {
        if (row.DrinkMaterial1 != 0) yield return row.DrinkMaterial1;
        if (row.DrinkMaterial2 != 0) yield return row.DrinkMaterial2;
        if (row.DrinkMaterial3 != 0) yield return row.DrinkMaterial3;
        if (row.DrinkMaterial4 != 0) yield return row.DrinkMaterial4;
        if (row.DrinkMaterial5 != 0) yield return row.DrinkMaterial5;
    }

    private static Dictionary<int, int> CountMaterials(IEnumerable<int> materialTids)
    {
        var counts = new Dictionary<int, int>();
        foreach (int tid in materialTids)
        {
            counts.TryGetValue(tid, out int count);
            counts[tid] = count + 1;
        }
        return counts;
    }

    // 선택 순서와 무관하게 같은 조합이면 같은 문자열이 나오도록 Tid 오름차순으로 정규화한다.
    private static string BuildSignature(IReadOnlyDictionary<int, int> materialCounts, CTable.eDrinkTempType temp)
    {
        var sb = new StringBuilder();
        foreach (var tid in materialCounts.Keys.OrderBy(t => t))
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(tid).Append(':').Append(materialCounts[tid]);
        }
        sb.Append('/').Append(temp);
        return sb.ToString();
    }

    public void Dispose()
    {
        mDicDrinks.Clear();
        mRecipeSignatureToTid.Clear();
        mBaseMaterialTids.Clear();
        mIceCounts.Clear();
        DefaultDrink = null;
    }
}
