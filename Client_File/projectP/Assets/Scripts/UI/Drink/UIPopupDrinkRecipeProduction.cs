using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 스페셜 주문 기획이 삭제되어, 이 팝업은 이제 손님 주문과 무관한 "음료 레시피 제작"(연구소) UI다.
// 재료 선택→매칭 방식은 그대로(UIPopupBreadProduction과 동일 패턴): 목표 레시피를 미리 지정하지 않고,
// 선택한 재료 조합과 일치하는 미발견 DrinkRow가 있으면 자동으로 발견 처리한다.
// 2026-08: 클래스/파일명을 UIPopupSpecialDrinkProduction → UIPopupDrinkRecipeProduction으로 변경.
// 프리팹(UI_Popup_SpecialDrinkProduction.prefab)의 m_Script는 guid 기반 참조라 파일/클래스명 변경과 무관하게
// 유지된다(.cs.meta의 guid를 그대로 보존한 채 파일만 옮김) — 프리팹 파일명 자체는 이번엔 바꾸지 않았다.
public class UIPopupDrinkRecipeProduction : UIWndBase, IUIParam<UIPopupDrinkRecipeProduction.Param>
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

    public override eUIType GetUIType() => eUIType.UIPopupDrinkRecipeProduction;

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
            Logger.Warning("[UIPopupDrinkRecipeProduction] DrinkMaterialGroup을 찾을 수 없습니다.");
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
            Logger.Log($"[UIPopupDrinkRecipeProduction] 재료가 부족합니다. Tid={tid}");
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
            Logger.Log("[UIPopupDrinkRecipeProduction] 레시피 개발북이 부족합니다.");
            return;
        }

        if (!GameInstance.Model.Drink.TryGetRecipeMatch(mSelectedMaterialCounts, out int drinkTid) ||
            GameInstance.Model.RecipeBook.IsDiscovered(drinkTid))
        {
            OnRecipeFail();
            return;
        }

        OnRecipeSuccess(drinkTid);
    }

    private void OnRecipeSuccess(int drinkTid)
    {
        foreach (var pair in mSelectedMaterialCounts)
            GameInstance.Model.Material.Consume(pair.Key, pair.Value);

        GameInstance.Model.Item.Consume(RecipeBookItemTid, 1);
        GameInstance.Model.RecipeBook.Discover(drinkTid);

        Logger.Log($"[UIPopupDrinkRecipeProduction] 레시피 개발 성공. Tid={drinkTid}");

        ResetSelectedMaterials();
        SetupMaterialScroll();
    }

    private void OnRecipeFail()
    {
        Logger.Log("[UIPopupDrinkRecipeProduction] 재료 조합이 일치하는 미발견 레시피가 없습니다.");
        ResetSelectedMaterials();
    }
}
