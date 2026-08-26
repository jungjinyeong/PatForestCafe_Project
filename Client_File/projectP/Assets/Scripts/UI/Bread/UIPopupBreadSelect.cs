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
    private bool mIsTableSelectStep;

    public override eUIType GetUIType() => eUIType.UIPopupBreadSelect;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mBreadRowPrefab);
        mScrollEx.SetOnSelect(OnSelectRow);
    }

    public void Set(Param param)
    {
        mBreadStand = param.breadStand;
    }

    public override void Open()
    {
        base.Open();

        mIsTableSelectStep = false;
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
            string name = menuItemRow?.Name ?? row.Tid.ToString();
            // 여기서 보여주는 수량은 진열 수량(Count)이 아니라 테이블에 놓을 수 있는 생산 재고(ProducedCount)다.
            int producedCount = GameInstance.Model.Bread.Get(row.Tid)?.ProducedCount.Value ?? 0;
            dataList.Add(new UIScrollBreadData
            {
                Tid = row.Tid,
                Name = $"{name} X{producedCount}",
            });
        }

        mScrollEx.SetData(dataList);
    }

    // 진열대 하나는 빵 Tid 하나만 취급하되, 그 종류는 빈 진열대에 처음 넣는 빵으로 정해진다(Intaraction_BreadStand.TryAssignBreadType).
    // 특정 진열대를 월드 탭으로 직접 연 경우(mBreadStand != null)는 기존처럼 그 자리에서 바로 확정하고,
    // HUD의 공용 "빵 선택" 버튼으로 연 경우(mBreadStand == null)는 배치 가능한 테이블 목록 단계로 넘어간다.
    private void OnSelectRow(UIScrollRow row)
    {
        if (row is not UIScrollBread breadRow || breadRow.CurrentData == null)
            return;

        if (mIsTableSelectStep)
        {
            OnSelectTable(breadRow.CurrentData);
            return;
        }

        if (mBreadStand != null)
        {
            // 빈 진열대(IsAssigned == false)면 지금 고른 빵으로 최초 고정을 시도하고, 이미 종류가 정해져 있으면
            // 같은 종류일 때만 통과한다 — TryAssignBreadType()이 두 경우를 한 번에 처리한다.
            if (!mBreadStand.TryAssignBreadType(breadRow.CurrentData.Tid))
            {
                Logger.Log($"[UIPopupBreadSelect] 이 진열대에서 취급하지 않는 빵입니다. Tid={breadRow.CurrentData.Tid}");
                return;
            }

            mBreadStand.AddBread();
            SelfClose();
            return;
        }

        SetupTableScroll(breadRow.CurrentData.Tid);
    }

    // 이 빵을 이미 취급 중인 진열대(TableId == breadTid) + 아직 아무 빵도 지정되지 않은 빈 진열대(TableId == 0)만
    // 후보로 보여준다. 다른 종류를 이미 취급 중인 진열대는 목록에서 빠지므로 추가할 수 없다.
    // 빈 진열대를 고르면 OnSelectTable()에서 그 자리에 이 빵 종류가 최초로 고정된다.
    private void SetupTableScroll(int breadTid)
    {
        var stands = FindObjectsByType<Intaraction_BreadStand>(FindObjectsSortMode.None);

        var dataList = new List<UIScrollBreadData>();
        foreach (var stand in stands)
        {
            if (stand == null || (stand.TableId != breadTid && stand.IsAssigned))
                continue;

            dataList.Add(new UIScrollBreadData
            {
                Tid = breadTid,
                Name = stand.IsAssigned ? stand.gameObject.name : $"{stand.gameObject.name} (빈 자리)",
                ShowAddIcon = true,
                TargetStand = stand,
            });
        }

        if (dataList.Count == 0)
            Logger.Log($"[UIPopupBreadSelect] 이 빵을 놓을 수 있는 진열대가 없습니다. Tid={breadTid}");

        mIsTableSelectStep = true;
        mScrollEx.SetData(dataList);
    }

    private void OnSelectTable(UIScrollBreadData data)
    {
        if (data?.TargetStand == null)
            return;

        if (!data.TargetStand.TryAssignBreadType(data.Tid))
        {
            Logger.Log($"[UIPopupBreadSelect] 이 진열대는 이미 다른 종류를 취급 중입니다. Tid={data.Tid}");
            return;
        }

        data.TargetStand.AddBread();
        SelfClose();
    }
}
