using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DrinkModel : IModelBase
{
    private readonly Dictionary<int, DrinkData> mDicDrinks = new();

    // 재료 조합 서명("Tid:Count,Tid:Count...", Tid 오름차순) → Tid. 재료 조합 하나당 음료 하나를 가정한다.
    // 레시피 제작대(UIPopupDrinkRecipeProduction)에서 매 제작 클릭마다 DrinkRow 전체를 스캔하지 않고
    // O(1)로 일치 여부를 찾기 위한 캐시 — 테이블 로드 시 1회만 구축(런타임에 DrinkTable이 바뀌지 않으므로 무효화 불필요).
    private readonly Dictionary<string, int> mRecipeSignatureToTid = new();

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

            string signature = BuildRecipeSignature(row);
            if (signature == null)
                continue;

            if (mRecipeSignatureToTid.ContainsKey(signature))
                Logger.Warning($"[DrinkModel] 재료 조합이 중복된 DrinkRow가 있습니다. Tid={row.Tid}");
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

    // 선택한 재료 조합(Tid→개수)과 정확히 일치하는 DrinkRow의 Tid를 찾는다. O(M log M)(M = 선택한 재료 종류 수, ≤5)
    // — 사전 구축된 mRecipeSignatureToTid 덕분에 DrinkRow 개수(N)와 무관하다.
    public bool TryGetRecipeMatch(IReadOnlyDictionary<int, int> selectedMaterialCounts, out int drinkTid)
    {
        drinkTid = 0;
        if (selectedMaterialCounts == null || selectedMaterialCounts.Count == 0)
            return false;

        return mRecipeSignatureToTid.TryGetValue(BuildSignature(selectedMaterialCounts), out drinkTid);
    }

    private static string BuildRecipeSignature(CTable.DrinkRow row)
    {
        var counts = new Dictionary<int, int>();
        AddMaterial(counts, row.DrinkMaterial1);
        AddMaterial(counts, row.DrinkMaterial2);
        AddMaterial(counts, row.DrinkMaterial3);
        AddMaterial(counts, row.DrinkMaterial4);
        AddMaterial(counts, row.DrinkMaterial5);

        return counts.Count > 0 ? BuildSignature(counts) : null;
    }

    private static void AddMaterial(Dictionary<int, int> counts, int materialTid)
    {
        if (materialTid == 0) return;
        counts.TryGetValue(materialTid, out int count);
        counts[materialTid] = count + 1;
    }

    // 선택 순서와 무관하게 같은 조합이면 같은 문자열이 나오도록 Tid 오름차순으로 정규화한다.
    private static string BuildSignature(IReadOnlyDictionary<int, int> materialCounts)
    {
        var sb = new StringBuilder();
        foreach (var tid in materialCounts.Keys.OrderBy(t => t))
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(tid).Append(':').Append(materialCounts[tid]);
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        mDicDrinks.Clear();
        mRecipeSignatureToTid.Clear();
        DefaultDrink = null;
    }
}
