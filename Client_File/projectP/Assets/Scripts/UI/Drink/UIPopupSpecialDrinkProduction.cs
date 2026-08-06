using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 스페셜 주문 기획이 삭제되어, 이 팝업은 이제 손님 주문과 무관한 "레시피 개발" UI로 재사용된다.
// 재료 선택→매칭 방식은 그대로(UIPopupBreadProduction과 동일 패턴): 목표 레시피를 미리 지정하지 않고,
// 선택한 재료 조합과 일치하는 미발견 DrinkRow가 있으면 자동으로 발견 처리한다.
// 프리팹(UI_Popup_SpecialDrinkProduction.prefab)의 스크립트 GUID를 유지하기 위해 클래스/파일명은 바꾸지 않았다.
public class UIPopupSpecialDrinkProduction : UIWndBase, IUIParam<UIPopupSpecialDrinkProduction.Param>
{
    public struct Param
    {
    }

    private const int RecipeBookItemTid = 1002;

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mDrinkMaterialRowPrefab;

    [Header("Recipe")]
    [SerializeField] private TextMeshProUGUI mTextSelectedMaterials;
    [SerializeField] private UIButtonEx mBtnConfirmRecipe;
    [SerializeField] private UIButtonEx mBtnResetRecipe;

    private readonly Dictionary<int, int> mSelectedMaterialCounts = new();

    public override eUIType GetUIType() => eUIType.UIPopupSpecialDrinkProduction;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mDrinkMaterialRowPrefab);
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
        var group = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (group == null)
        {
            Logger.Warning("[UIPopupSpecialDrinkProduction] DrinkMaterialGroup을 찾을 수 없습니다.");
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
            Logger.Log($"[UIPopupSpecialDrinkProduction] 재료가 부족합니다. Tid={tid}");
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

        var group = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
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
        if (!GameInstance.Model.Item.HasEnough(RecipeBookItemTid, 1))
        {
            Logger.Log("[UIPopupSpecialDrinkProduction] 레시피 개발북이 부족합니다.");
            return;
        }

        var drinkGroup = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (drinkGroup == null) return;

        foreach (var drinkRow in drinkGroup.All.Values)
        {
            if (GameInstance.Model.RecipeBook.IsDiscovered(drinkRow.Tid))
                continue;

            if (IsRecipeMatch(drinkRow))
            {
                OnRecipeSuccess(drinkRow.Tid);
                return;
            }
        }

        OnRecipeFail();
    }

    private bool IsRecipeMatch(CTable.DrinkRow drinkRow)
    {
        var required = new[]
        {
            drinkRow.DrinkMaterial1,
            drinkRow.DrinkMaterial2,
            drinkRow.DrinkMaterial3,
            drinkRow.DrinkMaterial4,
            drinkRow.DrinkMaterial5,
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

    private void OnRecipeSuccess(int drinkTid)
    {
        foreach (var pair in mSelectedMaterialCounts)
            GameInstance.Model.Material.Consume(pair.Key, pair.Value);

        GameInstance.Model.Item.Consume(RecipeBookItemTid, 1);
        GameInstance.Model.RecipeBook.Discover(drinkTid);

        Logger.Log($"[UIPopupSpecialDrinkProduction] 레시피 개발 성공. Tid={drinkTid}");

        ResetSelectedMaterials();
        SetupMaterialScroll();
    }

    private void OnRecipeFail()
    {
        Logger.Log("[UIPopupSpecialDrinkProduction] 재료 조합이 일치하는 미발견 레시피가 없습니다.");
        ResetSelectedMaterials();
    }
}
