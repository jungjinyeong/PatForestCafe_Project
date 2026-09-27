using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIPopupUpgrade : UIWndBase, IUIParam<UIPopupUpgrade.Param>
{
    public struct Param
    {
    }

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextLevel;
    [SerializeField] private TextMeshProUGUI mTextMultiplier;
    [SerializeField] private TextMeshProUGUI mTextNextCost;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnUpgrade;

    // 오븐 업그레이드(트레이 수·트레이당 최대 수량·굽기 시간). 상점가 똘이 "시설 업그레이드"에서 함께 다룬다.
    [Header("Oven")]
    [SerializeField] private TextMeshProUGUI mTextOvenLevel;
    [SerializeField] private TextMeshProUGUI mTextOvenCurrent;
    [SerializeField] private TextMeshProUGUI mTextOvenNext;
    [SerializeField] private UIButtonEx mBtnOvenUpgrade;

    public override eUIType GetUIType() => eUIType.PopupUpgrade;

    public override void Init()
    {
        base.Init();

        mBtnUpgrade.OnSubscribeOnClick(OnClickUpgrade).AddTo(this);

        if (mBtnOvenUpgrade != null)
            mBtnOvenUpgrade.OnSubscribeOnClick(OnClickOvenUpgrade).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        RefreshTexts();
    }

    public void Set(Param param)
    {
    }

    private void OnClickUpgrade()
    {
        if (!GameInstance.Model.Upgrade.TryUpgrade())
        {
            Logger.Log("[UIPopupUpgrade] 골드가 부족해 업그레이드할 수 없습니다.");
            return;
        }

        RefreshTexts();
    }

    private void OnClickOvenUpgrade()
    {
        if (!GameInstance.Model.Oven.TryUpgrade())
        {
            Logger.Log("[UIPopupUpgrade] 골드가 부족하거나 오븐이 최대 레벨입니다.");
            return;
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var upgrade = GameInstance.Model.Upgrade;

        mTextLevel?.SetTextEx($"Lv.{upgrade.Level}");
        mTextMultiplier?.SetTextEx($"골드 수익 x{upgrade.GoldIncomeMultiplier:0.0}");
        mTextNextCost?.SetTextEx($"다음 업그레이드 비용: {upgrade.GetNextUpgradeCost():N0} Gold");

        var oven = GameInstance.Model.Oven;
        mTextOvenLevel?.SetTextEx($"오븐 Lv.{oven.Level.Value}");
        mTextOvenCurrent?.SetTextEx($"트레이 {oven.UnlockedTrayCount}칸 / 최대 {oven.MaxQuantity}개 / 굽기 {oven.BakeSeconds}초");
        mTextOvenNext?.SetTextEx(oven.IsMaxLevel ? "최대 레벨" : $"다음: {oven.GetNextUpgradeSummary()}  ({oven.NextUpgradeCost:N0} Gold)");
        if (mBtnOvenUpgrade != null)
            mBtnOvenUpgrade.interactable = !oven.IsMaxLevel;
    }
}
