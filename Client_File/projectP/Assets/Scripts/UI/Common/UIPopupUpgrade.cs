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

    public override eUIType GetUIType() => eUIType.UIPopupUpgrade;

    public override void Init()
    {
        base.Init();

        mBtnUpgrade.OnSubscribeOnClick(OnClickUpgrade).AddTo(this);
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

    private void RefreshTexts()
    {
        var upgrade = GameInstance.Model.Upgrade;

        mTextLevel?.SetTextEx($"Lv.{upgrade.Level}");
        mTextMultiplier?.SetTextEx($"골드 수익 x{upgrade.GoldIncomeMultiplier:0.0}");
        mTextNextCost?.SetTextEx($"다음 업그레이드 비용: {upgrade.GetNextUpgradeCost():N0} Gold");
    }
}
