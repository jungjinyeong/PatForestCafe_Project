using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 로비 오른쪽 패널의 업그레이드 화면(UI_Root_Lobby/Sidebar/TabArea/ContentSlot/UpgradeView). 오른쪽 세로 탭 [업그레이드]가 켠다.
// 위: 카페 수익 업그레이드(UpgradeModel) / 아래: 오븐 업그레이드(OvenModel). 예전 UIPopupUpgrade를 좁은 칸에 맞춰 옮긴 것.
// 패널은 화면에 계속 떠 있으므로 켜져 있는 동안 골드·오븐 레벨 변화를 구독해 비용 색과 버튼 상태를 바로 맞춘다.
// UILobbySidePanel.Init()에서 Init()을 호출한다.
public class UISideUpgradeView : MonoBehaviour
{
    [Header("Cafe")]
    [SerializeField] private TextMeshProUGUI mTextCafeLevel;
    [SerializeField] private TextMeshProUGUI mTextCafeEffect;
    [SerializeField] private TextMeshProUGUI mTextCafeCost;
    [SerializeField] private UIButtonEx mBtnCafeUpgrade;

    [Header("Oven")]
    [SerializeField] private TextMeshProUGUI mTextOvenLevel;
    [SerializeField] private TextMeshProUGUI mTextOvenCurrent;
    [SerializeField] private TextMeshProUGUI mTextOvenNext;
    [SerializeField] private TextMeshProUGUI mTextOvenCost;
    [SerializeField] private UIButtonEx mBtnOvenUpgrade;

    [Header("Colors")]
    [SerializeField] private Color mCostColor = new Color(0.35f, 0.22f, 0.12f);
    [SerializeField] private Color mCostShortColor = new Color(0.75f, 0.2f, 0.15f);

    private readonly CompositeDisposable mWatchDisposables = new CompositeDisposable();
    private bool mIsInitialized;

    private void OnEnable()
    {
        if (mIsInitialized)
            StartWatching();
    }

    private void OnDisable()
    {
        mWatchDisposables.Clear();
    }

    private void OnDestroy()
    {
        mWatchDisposables.Dispose();
    }

    public void Init()
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        if (mBtnCafeUpgrade != null)
            mBtnCafeUpgrade.OnSubscribeOnClick(OnClickCafeUpgrade).AddTo(this);
        if (mBtnOvenUpgrade != null)
            mBtnOvenUpgrade.OnSubscribeOnClick(OnClickOvenUpgrade).AddTo(this);

        if (isActiveAndEnabled)
            StartWatching();
    }

    // 골드가 바뀌면(영업 수익, 구매 등) 살 수 있는지가 바뀐다. 오븐 레벨은 다른 곳에서 바뀔 일은 없지만 같이 본다.
    private void StartWatching()
    {
        mWatchDisposables.Clear();

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold != null)
            gold.Count.Subscribe(_ => Refresh()).AddTo(mWatchDisposables);

        GameInstance.Model.Oven.Level.Subscribe(_ => Refresh()).AddTo(mWatchDisposables);
        Refresh();
    }

    private void OnClickCafeUpgrade()
    {
        // 버튼은 골드가 모자라면 꺼져 있다. 그래도 실패하면 상태만 다시 맞춘다.
        GameInstance.Model.Upgrade.TryUpgrade();
        Refresh();
    }

    private void OnClickOvenUpgrade()
    {
        GameInstance.Model.Oven.TryUpgrade();
        Refresh();
    }

    private void Refresh()
    {
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        long owned = gold != null ? gold.Count.Value : 0;

        var upgrade = GameInstance.Model.Upgrade;
        int cafeCost = upgrade.GetNextUpgradeCost();
        bool canCafe = owned >= cafeCost;
        mTextCafeLevel?.SetTextEx($"카페 수익 Lv.{upgrade.Level}");
        mTextCafeEffect?.SetTextEx($"골드 수익 x{upgrade.GoldIncomeMultiplier:0.0}");
        SetCost(mTextCafeCost, $"{cafeCost:N0} G", canCafe);
        if (mBtnCafeUpgrade != null)
            mBtnCafeUpgrade.interactable = canCafe;

        var oven = GameInstance.Model.Oven;
        bool isMax = oven.IsMaxLevel;
        bool canOven = !isMax && owned >= oven.NextUpgradeCost;
        mTextOvenLevel?.SetTextEx($"오븐 Lv.{oven.Level.Value}");
        mTextOvenCurrent?.SetTextEx($"트레이 {oven.UnlockedTrayCount}칸 · 최대 {oven.MaxQuantity}개\n굽기 {oven.BakeSeconds}초");
        mTextOvenNext?.SetTextEx(isMax ? "최대 레벨이에요" : $"다음: {oven.GetNextUpgradeSummary()}");
        SetCost(mTextOvenCost, isMax ? string.Empty : $"{oven.NextUpgradeCost:N0} G", canOven || isMax);
        if (mBtnOvenUpgrade != null)
            mBtnOvenUpgrade.interactable = canOven;
    }

    private void SetCost(TextMeshProUGUI text, string value, bool affordable)
    {
        if (text == null) return;
        text.SetTextEx(value);
        text.color = affordable ? mCostColor : mCostShortColor;
    }
}
