// 창고 — 아직 실제 화면이 없어 빈 상태 안내만 띄운다(뼈대 단계).
public class UIPopupWarehouse : UIWndBase, IUIParam<UIPopupWarehouse.Param>
{
    public struct Param
    {
    }

    public override eUIType GetUIType() => eUIType.UIPopupWarehouse;

    public void Set(Param param)
    {
    }
}
