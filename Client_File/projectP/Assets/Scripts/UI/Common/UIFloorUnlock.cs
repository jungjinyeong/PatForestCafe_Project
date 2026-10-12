using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 잠긴 층을 골드로 해금하는 팝업. 매장층(1F→B1→B2→B3)과 테라스층(T1→T2→T3)을 각각 따로 해금한다.
// 순서·비용은 Floor.csv(UnlockOrder, UnlockCost). UIPopupUpgrade와 동일한 "현재 상태 + 다음 비용 + 버튼" 패턴.
public class UIFloorUnlock : UIWndBase, IUIParam<UIFloorUnlock.Param>
{
    public struct Param
    {
    }

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextCurrentFloor;
    [SerializeField] private TextMeshProUGUI mTextNextCost;             // 매장층
    [SerializeField] private TextMeshProUGUI mTextTerraceNextCost;      // 테라스층

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnUnlock;                     // 매장층
    [SerializeField] private UIButtonEx mBtnUnlockTerrace;              // 테라스층

    public override eUIType GetUIType() => eUIType.PopupFloorUnlock;

    public override void Init()
    {
        base.Init();

        // 테라스 줄은 프리팹에 아직 없을 수 있어 null 체크 후 연결한다.
        if (mBtnUnlock != null) mBtnUnlock.OnSubscribeOnClick(() => OnClickUnlock(eFloorType.Store)).AddTo(this);
        if (mBtnUnlockTerrace != null) mBtnUnlockTerrace.OnSubscribeOnClick(() => OnClickUnlock(eFloorType.Terrace)).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        RefreshTexts();
    }

    public void Set(Param param)
    {
    }

    private void OnClickUnlock(eFloorType type)
    {
        if (!GameInstance.Model.Floor.TryUnlockNextFloor(type))
        {
            Logger.Log($"[UIFloorUnlock] 골드가 부족하거나 이미 모든 {GetTypeName(type)}이 열렸습니다.");
            return;
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var floor = GameInstance.Model.Floor;

        var unlockedNames = floor.Floors.Where(f => floor.IsUnlocked(f.Id)).Select(f => f.Name);
        mTextCurrentFloor?.SetTextEx($"열린 층: {string.Join(", ", unlockedNames)}");

        mTextNextCost?.SetTextEx(BuildNextText(eFloorType.Store));
        mTextTerraceNextCost?.SetTextEx(BuildNextText(eFloorType.Terrace));
    }

    private static string BuildNextText(eFloorType type)
    {
        var floor = GameInstance.Model.Floor;
        var next = floor.GetNextLockedFloor(type);
        return next.HasValue
            ? $"{floor.GetName(next.Value)} 해금 비용: {floor.GetUnlockCost(next.Value):N0} Gold"
            : $"모든 {GetTypeName(type)}이 열렸습니다";
    }

    private static string GetTypeName(eFloorType type) => type == eFloorType.Terrace ? "테라스층" : "매장층";
}
