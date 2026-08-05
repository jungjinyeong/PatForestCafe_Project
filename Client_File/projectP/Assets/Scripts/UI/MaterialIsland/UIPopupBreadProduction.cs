using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// UIPopupSpecialDrinkProduction과 동일한 재료 선택→매칭 패턴이지만,
// 특정 NPC 주문에 묶이지 않고 빵 레시피(BreadRow.BreadMaterial1~5) 전체를 대상으로 매칭한다.
public class UIPopupBreadProduction : UIWndBase, IUIParam<UIPopupBreadProduction.Param>
{
    public struct Param
    {
    }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mBreadMaterialRowPrefab;

    [Header("Recipe")]
    [SerializeField] private TextMeshProUGUI mTextSelectedMaterials;
    [SerializeField] private UIButtonEx mBtnConfirmRecipe;
    [SerializeField] private UIButtonEx mBtnResetRecipe;

    private readonly Dictionary<int, int> mSelectedMaterialCounts = new();

    public override eUIType GetUIType() => eUIType.UIPopupBreadProduction;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mBreadMaterialRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);

        mBtnConfirmRecipe.OnSubscribeOnClick(OnClickConfirmRecipe).AddTo(this);
        mBtnResetRecipe.OnSubscribeOnClick(ResetSelectedMaterials).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        ResetSelectedMaterials();
        SetupMaterialScroll();
    }

    public void Set(Param param)
    {
    }

    private void SetupMaterialScroll()
    {
        var group = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (group == null)
        {
            Logger.Warning("[UIPopupBreadProduction] BreadMaterialGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollDrinkMaterialData>();
        foreach (var row in group.All.Values)
        {
            dataList.Add(new UIScrollDrinkMaterialData
            {
                Tid = row.Tid,
                Name = row.Name,
                OwnedCount = GameInstance.Model.Material.Get(row.Tid)?.Count.Value ?? 0,
            });
        }

        mScrollEx.SetData(dataList);
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is not UIScrollDrinkMaterial materialRow || materialRow.CurrentData == null)
            return;

        int tid = materialRow.CurrentData.Tid;
        mSelectedMaterialCounts.TryGetValue(tid, out int count);

        if (!GameInstance.Model.Material.HasEnough(tid, count + 1))
        {
            Logger.Log($"[UIPopupBreadProduction] 재료가 부족합니다. Tid={tid}");
            return;
        }

        mSelectedMaterialCounts[tid] = count + 1;

        RefreshSelectedMaterialsText();
    }

    private void ResetSelectedMaterials()
    {
        mSelectedMaterialCounts.Clear();
        RefreshSelectedMaterialsText();
    }

    private void RefreshSelectedMaterialsText()
    {
        if (mTextSelectedMaterials == null) return;

        if (mSelectedMaterialCounts.Count == 0)
        {
            mTextSelectedMaterials.SetTextEx(string.Empty);
            return;
        }

        var group = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        var parts = new List<string>();

        foreach (var pair in mSelectedMaterialCounts)
        {
            string name = group?.Get(pair.Key)?.Name ?? pair.Key.ToString();
            parts.Add($"{name} x{pair.Value}");
        }

        mTextSelectedMaterials.SetTextEx(string.Join(", ", parts));
    }

    private void OnClickConfirmRecipe()
    {
        var breadGroup = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (breadGroup == null) return;

        foreach (var breadRow in breadGroup.All.Values)
        {
            if (IsRecipeMatch(breadRow))
            {
                OnRecipeSuccess(breadRow.Tid);
                return;
            }
        }

        OnRecipeFail();
    }

    private bool IsRecipeMatch(CTable.BreadRow breadRow)
    {
        var required = new[]
        {
            breadRow.BreadMaterial1,
            breadRow.BreadMaterial2,
            breadRow.BreadMaterial3,
            breadRow.BreadMaterial4,
            breadRow.BreadMaterial5,
        };

        var requiredCounts = new Dictionary<int, int>();
        foreach (var tid in required)
        {
            if (tid == 0) continue;
            requiredCounts.TryGetValue(tid, out int count);
            requiredCounts[tid] = count + 1;
        }

        if (requiredCounts.Count == 0 || requiredCounts.Count != mSelectedMaterialCounts.Count)
            return false;

        foreach (var pair in requiredCounts)
        {
            if (!mSelectedMaterialCounts.TryGetValue(pair.Key, out int selectedCount) || selectedCount != pair.Value)
                return false;
        }

        return true;
    }

    private void OnRecipeSuccess(int breadTid)
    {
        foreach (var pair in mSelectedMaterialCounts)
            GameInstance.Model.Material.Consume(pair.Key, pair.Value);

        // 진열대가 아직 이 빵을 Register()하지 않았을 수도 있어(에디터 배치 전) 여기서 보장한다. 이미 등록돼 있으면 아무 동작 없음.
        GameInstance.Model.Bread.Register(breadTid);
        // 여기서 늘리는 건 "생산 재고"이며 진열 수량(Count)이 아니다 — 진열대(Intaraction_BreadStand.AddBread)가 이 재고를 소비해야 실제로 진열된다.
        GameInstance.Model.Bread.AddProduced(breadTid);

        Logger.Log($"[UIPopupBreadProduction] 빵 생산 완료. Tid={breadTid}");

        ResetSelectedMaterials();
        SetupMaterialScroll();
    }

    private void OnRecipeFail()
    {
        Logger.Log("[UIPopupBreadProduction] 재료 조합이 일치하는 빵 레시피가 없습니다.");
        ResetSelectedMaterials();
    }
}
