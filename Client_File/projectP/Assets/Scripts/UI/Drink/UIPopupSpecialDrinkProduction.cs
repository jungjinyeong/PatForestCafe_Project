using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIPopupSpecialDrinkProduction : UIWndBase, IUIParam<UIPopupSpecialDrinkProduction.Param>
{
    public struct Param
    {
        public BoxCollider2D npc;
    }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mDrinkMaterialRowPrefab;

    [Header("Recipe")]
    [SerializeField] private TextMeshProUGUI mTextSelectedMaterials;
    [SerializeField] private UIButtonEx mBtnConfirmRecipe;
    [SerializeField] private UIButtonEx mBtnResetRecipe;

    private BoxCollider2D mNpcCollider;
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
        mNpcCollider = param.npc;
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
        if (mNpcCollider == null) return;

        var waiter = mNpcCollider.GetComponentInParent<ISpecialOrderWaiter>();
        var lobbyCharUI = mNpcCollider.GetComponentInChildren<LobbyCharUI>();
        if (waiter == null || lobbyCharUI == null) return;

        var desiredDrinkRow = GameInstance.Table.Get<CTable.DrinkRow>(lobbyCharUI.DesiredDrinkTid);
        if (desiredDrinkRow == null)
        {
            Logger.Warning($"[UIPopupSpecialDrinkProduction] 원하는 DrinkRow를 찾을 수 없습니다. Tid={lobbyCharUI.DesiredDrinkTid}");
            return;
        }

        if (IsRecipeMatch(desiredDrinkRow))
            OnRecipeSuccess(lobbyCharUI.DesiredDrinkTid, waiter);
        else
            OnRecipeFail();
    }

    private bool IsRecipeMatch(CTable.DrinkRow desiredDrinkRow)
    {
        var required = new[]
        {
            desiredDrinkRow.DrinkMaterial1,
            desiredDrinkRow.DrinkMaterial2,
            desiredDrinkRow.DrinkMaterial3,
            desiredDrinkRow.DrinkMaterial4,
            desiredDrinkRow.DrinkMaterial5,
        };

        var requiredCounts = new Dictionary<int, int>();
        foreach (var tid in required)
        {
            if (tid == 0) continue;
            requiredCounts.TryGetValue(tid, out int count);
            requiredCounts[tid] = count + 1;
        }

        if (requiredCounts.Count != mSelectedMaterialCounts.Count)
            return false;

        foreach (var pair in requiredCounts)
        {
            if (!mSelectedMaterialCounts.TryGetValue(pair.Key, out int selectedCount) || selectedCount != pair.Value)
                return false;
        }

        return true;
    }

    private void OnRecipeSuccess(int desiredDrinkTid, ISpecialOrderWaiter waiter)
    {
        var menuItemRow = GameInstance.Model.Drink.Get(desiredDrinkTid)?.MenuItemRow;
        if (menuItemRow != null)
            GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add((int)menuItemRow.Price);

        waiter.ResumeFromSpecialOrderWait();

        GameInstance.UI.Close(eUIType.UIPopupSpecialDrinkProduction);
        GameInstance.UI.Close(eUIType.PopupOrderDetail);
    }

    private void OnRecipeFail()
    {
        Logger.Log("[UIPopupSpecialDrinkProduction] 재료 조합이 일치하지 않습니다.");
        ResetSelectedMaterials();
    }
}
