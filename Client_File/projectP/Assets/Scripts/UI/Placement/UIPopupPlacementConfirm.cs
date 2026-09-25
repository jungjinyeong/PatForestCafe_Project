using UnityEngine;
using UniRx;
using Extension;

// 가구 드래그 배치 중(PlacementModel.IsPlacing)에 뜨는 확정/취소 팝업(프리팹 UI_Popup_PlacementConfirm).
// UIRootLobby가 IsPlacing 변화에 맞춰 열고 닫는다.
// 2026-09: UI_Root_Lobby에 붙어 있던 UIPlacementConfirm을 팝업으로 분리(.cs.meta guid 유지).
public class UIPopupPlacementConfirm : UIWndBase, IUIParam<UIPopupPlacementConfirm.Param>
{
    public struct Param
    {
    }

    [SerializeField] private UIButtonEx mBtnConfirm;
    [SerializeField] private UIButtonEx mBtnCancel;

    public override eUIType GetUIType() => eUIType.PopupPlacementConfirm;

    public override void Init()
    {
        base.Init();

        mBtnConfirm.OnSubscribeOnClick(OnClickConfirm).AddTo(this);
        mBtnCancel.OnSubscribeOnClick(OnClickCancel).AddTo(this);
    }

    public void Set(Param param)
    {
    }

    private void OnClickConfirm()
    {
        GameInstance.Model.Placement.Confirm();
    }

    private void OnClickCancel()
    {
        GameInstance.Model.Placement.Cancel();
    }
}
