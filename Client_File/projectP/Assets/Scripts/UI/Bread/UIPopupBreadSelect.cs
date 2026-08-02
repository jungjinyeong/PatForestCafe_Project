using System.Collections.Generic;
using UnityEngine;
using Extension;

public class UIPopupBreadSelect : UIWndBase, IUIParam<UIPopupBreadSelect.Param>
{
    public struct Param
    {
        public Intaraction_BreadStand breadStand;
    }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mBreadRowPrefab;

    private Intaraction_BreadStand mBreadStand;

    public override eUIType GetUIType() => eUIType.UIPopupBreadSelect;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mBreadRowPrefab);
        mScrollEx.SetOnSelect(OnSelectBread);
    }

    public void Set(Param param)
    {
        mBreadStand = param.breadStand;
    }

    public override void Open()
    {
        base.Open();

        SetupBreadScroll();
    }

    private void SetupBreadScroll()
    {
        var group = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (group == null)
        {
            Logger.Warning("[UIPopupBreadSelect] BreadGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollBreadData>();
        foreach (var row in group.All.Values)
        {
            var menuItemRow = GameInstance.Table.Get<CTable.MenuItemRow>(row.Tid);
            dataList.Add(new UIScrollBreadData
            {
                Tid = row.Tid,
                Name = menuItemRow?.Name ?? row.Tid.ToString(),
            });
        }

        mScrollEx.SetData(dataList);
    }

    // 진열대 하나는 고정된 빵 Tid 하나만 취급한다(Intaraction_BreadStand.TableId == BreadRow.Tid).
    // 목록은 전체 빵을 보여주되, 이 진열대가 실제로 파는 빵을 선택했을 때만 확정한다.
    private void OnSelectBread(UIScrollRow row)
    {
        if (row is not UIScrollBread breadRow || breadRow.CurrentData == null || mBreadStand == null)
            return;

        if (breadRow.CurrentData.Tid != mBreadStand.TableId)
        {
            Logger.Log($"[UIPopupBreadSelect] 이 진열대에서 취급하지 않는 빵입니다. Tid={breadRow.CurrentData.Tid}");
            return;
        }

        mBreadStand.AddBread();
        SelfClose();
    }
}
