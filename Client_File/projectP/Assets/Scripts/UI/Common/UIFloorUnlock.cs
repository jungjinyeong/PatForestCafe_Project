using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 다음 잠긴 층을 골드로 언락하는 팝업. UIPopupUpgrade와 동일한 "현재 상태 + 다음 비용 + 버튼" 패턴.
public class UIFloorUnlock : UIWndBase, IUIParam<UIFloorUnlock.Param>
{
    public struct Param
    {
    }

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextCurrentFloor;
    [SerializeField] private TextMeshProUGUI mTextNextCost;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnUnlock;

    public override eUIType GetUIType() => eUIType.UIFloorUnlock;

    public override void Init()
    {
        base.Init();

        mBtnUnlock.OnSubscribeOnClick(OnClickUnlock).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        RefreshTexts();
    }

    public void Set(Param param)
    {
    }

    private void OnClickUnlock()
    {
        if (!GameInstance.Model.Floor.TryUnlockNextFloor())
        {
            Logger.Log("[UIFloorUnlock] 골드가 부족하거나 이미 전부 언락된 상태입니다.");
            return;
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var floor = GameInstance.Model.Floor;

        mTextCurrentFloor?.SetTextEx($"현재 {floor.HighestUnlockedFloor}층까지 언락됨");

        var next = floor.GetNextLockedFloor();
        mTextNextCost?.SetTextEx(next.HasValue
            ? $"{next.Value}층 언락 비용: {floor.GetUnlockCost(next.Value):N0} Gold"
            : "모든 층이 언락되었습니다");
    }
}
