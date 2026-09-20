// 직원 — 아직 실제 화면이 없어 빈 상태 안내만 띄운다(뼈대 단계).
public class UIPopupStaff : UIWndBase, IUIParam<UIPopupStaff.Param>
{
    public struct Param
    {
    }

    public override eUIType GetUIType() => eUIType.UIPopupStaff;

    public void Set(Param param)
    {
    }
}
