using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 직원 관리(프리팹 UI_Popup_Staff, 로비 하단 [직원]). ForestCafe UI Lab 직원 관리 시안 기준.
// 왼쪽: 채용한 직원 명단(StaffModel.HiredStaff). 비어 있으면 안내 문구. [직업사무소로 이동]으로 바로 채용하러 간다.
// 오른쪽: 선택한 직원 상세(이름·작업 속도·배치 상태).
// 층/업무 배치는 기획 미정이라 아직 없다(상세에 "미배치"로만 표시).
public class UIPopupStaff : UIWndBase, IUIParam<UIPopupStaff.Param>
{
    public struct Param
    {
    }

    [Header("List")]
    [SerializeField] private TextMeshProUGUI mTextHiredCount;
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UIStaffCell mCellTemplate;
    [SerializeField] private GameObject mEmptyRoot;
    [SerializeField] private UIButtonEx mBtnJobOffice;

    [Header("Detail")]
    [SerializeField] private GameObject mDetailRoot;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextNumber;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private GameObject[] mInfoRoots;
    [SerializeField] private TextMeshProUGUI[] mInfoLabels;
    [SerializeField] private TextMeshProUGUI[] mInfoValues;

    private readonly List<UIStaffCell> mCells = new List<UIStaffCell>();
    private readonly SerialDisposable mHiredDisposable = new SerialDisposable();
    private int mSelectedIndex = -1;
    private int mLastCount = -1;

    public override eUIType GetUIType() => eUIType.PopupStaff;

    public override void Init()
    {
        base.Init();

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);
        if (mBtnJobOffice != null)
            mBtnJobOffice.OnSubscribeOnClick(OnClickJobOffice).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        mSelectedIndex = -1;
        mLastCount = -1;
        // 직업사무소에서 채용하고 돌아오면(이 팝업이 뒤에 열려 있어도) 명단을 바로 갱신한다.
        mHiredDisposable.Disposable = GameInstance.Model.Staff.HiredCount
            .Subscribe(_ => Refresh());
    }

    public void Set(Param param)
    {
    }

    private void OnDisable()
    {
        mHiredDisposable.Disposable = null;
    }

    private void OnDestroy()
    {
        mHiredDisposable.Dispose();
    }

    private void OnClickJobOffice()
    {
        GameInstance.UI.Open<UIPopupJobOffice, UIPopupJobOffice.Param>(eUIType.PopupJobOffice, new UIPopupJobOffice.Param());
    }

    private void OnSelectCell(UIStaffCell cell)
    {
        SelectEntry(cell.Data);
    }

    private List<UIStaffCellData> GetEntries()
    {
        var hired = GameInstance.Model.Staff.HiredStaff;
        var list = new List<UIStaffCellData>(hired.Count);
        for (int i = 0; i < hired.Count; i++)
        {
            var row = GameInstance.Table.Get<CTable.StaffRow>(hired[i].Tid);
            list.Add(new UIStaffCellData
            {
                Index = i,
                Tid = hired[i].Tid,
                Name = row?.Name ?? $"직원 {hired[i].Tid}",
                WorkSpeed = row != null && row.WorkSpeed > 0f ? row.WorkSpeed : 1f,
            });
        }
        return list;
    }

    private void Refresh()
    {
        var list = GetEntries();
        mTextHiredCount.SetTextEx($"채용 {list.Count}명");

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
                mCells[i].SetData(list[i]);
        }

        if (mEmptyRoot != null)
            mEmptyRoot.SetActive(list.Count == 0);

        // 열린 채로 새로 채용했으면 방금 들어온 직원(마지막)을, 아니면 보던 직원을 유지한다.
        bool isNewHire = mLastCount >= 0 && list.Count > mLastCount;
        mLastCount = list.Count;
        var selected = isNewHire
            ? list.LastOrDefault()
            : list.FirstOrDefault(e => e.Index == mSelectedIndex) ?? list.FirstOrDefault();
        SelectEntry(selected);
    }

    private void SelectEntry(UIStaffCellData entry)
    {
        mSelectedIndex = entry?.Index ?? -1;

        foreach (var cell in mCells)
            cell.SetSelected(cell.gameObject.activeSelf && cell.Data != null && cell.Data.Index == mSelectedIndex);

        if (mDetailRoot != null)
            mDetailRoot.SetActive(entry != null);
        if (entry == null)
            return;

        mTextName.SetTextEx(entry.Name);
        mTextNumber.SetTextEx($"STAFF {entry.Index + 1:00}");
        mTextGlyph.SetTextEx(string.IsNullOrEmpty(entry.Name) ? "?" : entry.Name.Substring(0, 1));
        mTextDesc.SetTextEx("직업사무소에서 채용한 직원이에요.\n업무 배치 기능이 열리면 층과 업무를 정해 줄 수 있어요.");

        var infos = new List<(string label, string value)>
        {
            ("직원 번호", $"#{entry.Index + 1}"),
            ("작업 속도", $"x{entry.WorkSpeed:0.0}"),
            ("배치", "미배치"),
        };

        for (int i = 0; i < mInfoRoots.Length; i++)
        {
            bool active = i < infos.Count;
            mInfoRoots[i].SetActive(active);
            if (!active) continue;
            mInfoLabels[i].SetTextEx(infos[i].label);
            mInfoValues[i].SetTextEx(infos[i].value);
        }
    }
}
