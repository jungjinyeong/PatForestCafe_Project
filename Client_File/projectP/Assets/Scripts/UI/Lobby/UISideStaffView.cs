using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 로비 오른쪽 패널의 직원 화면(UI_Root_Lobby/Sidebar/TabArea/ContentSlot/StaffView). 오른쪽 세로 탭 [직원]이 켠다.
// 채용한 직원 명단(4열) + [직업사무소]. 칸을 누르면 직원 상세 팝업(UIPopupStaff)을 그 직원을 고른 채로 연다.
// 켜져 있는 동안 채용 수를 구독해 명단을 다시 그린다. UILobbySidePanel.Init()에서 Init()을 호출한다.
public class UISideStaffView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mTextHiredCount;
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UIStaffCell mCellTemplate;
    [SerializeField] private TextMeshProUGUI mTextEmpty;
    [SerializeField] private UIButtonEx mBtnJobOffice;

    private readonly List<UIStaffCell> mCells = new List<UIStaffCell>();
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

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);
        if (mBtnJobOffice != null)
            mBtnJobOffice.OnSubscribeOnClick(OnClickJobOffice).AddTo(this);

        if (isActiveAndEnabled)
            StartWatching();
    }

    // 직업사무소에서 채용하면 바로 명단에 들어온다.
    private void StartWatching()
    {
        mWatchDisposables.Clear();
        GameInstance.Model.Staff.HiredCount
            .Subscribe(_ => Refresh())
            .AddTo(mWatchDisposables);
    }

    private void OnClickJobOffice()
    {
        GameInstance.UI.Open<UIPopupJobOffice, UIPopupJobOffice.Param>(eUIType.PopupJobOffice, new UIPopupJobOffice.Param());
    }

    private void OnSelectCell(UIStaffCell cell)
    {
        if (cell.Data == null) return;
        GameInstance.UI.Open<UIPopupStaff, UIPopupStaff.Param>(eUIType.PopupStaff, new UIPopupStaff.Param { SelectIndex = cell.Data.Index });
    }

    private void Refresh()
    {
        var list = StaffEntries.Get();
        mTextHiredCount?.SetTextEx($"채용 {list.Count}명");

        while (mCells.Count < list.Count)
        {
            var cell = Instantiate(mCellTemplate, mGridContent);
            cell.Bind(OnSelectCell);
            mCells.Add(cell);
        }

        for (int i = 0; i < mCells.Count; i++)
        {
            bool active = i < list.Count;
            mCells[i].gameObject.SetActive(active);
            if (active)
            {
                mCells[i].SetData(list[i]);
                mCells[i].SetSelected(false);
            }
        }

        if (mTextEmpty != null)
            mTextEmpty.gameObject.SetActive(list.Count == 0);
    }
}
