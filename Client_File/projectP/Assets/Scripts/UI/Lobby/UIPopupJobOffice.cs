using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 직업사무소 — 이력서 확인 후 채용. 배치(층/업무 지정)는 이번 범위 밖, 채용까지만 처리한다.
public class UIPopupJobOffice : UIWndBase, IUIParam<UIPopupJobOffice.Param>
{
    public struct Param
    {
    }

    [SerializeField] private Transform mListContainer;
    [SerializeField] private GameObject mRowButtonTemplate;
    [SerializeField] private TextMeshProUGUI mTextHiredCount;

    private readonly List<GameObject> mSpawnedRows = new List<GameObject>();

    public override eUIType GetUIType() => eUIType.UIPopupJobOffice;

    public override void Open()
    {
        base.Open();

        RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshList()
    {
        foreach (var go in mSpawnedRows)
            Destroy(go);
        mSpawnedRows.Clear();

        if (mTextHiredCount != null)
            mTextHiredCount.text = $"채용된 직원 {GameInstance.Model.Staff.HiredStaff.Count}명";

        if (mListContainer == null || mRowButtonTemplate == null)
            return;

        var group = GameInstance.Table.GetTable<CTable.StaffRow>();
        if (group == null)
            return;

        int nextCost = GameInstance.Model.Staff.GetNextHireCost();

        foreach (var row in group.All.Values)
        {
            var rowGO = Instantiate(mRowButtonTemplate, mListContainer);
            rowGO.SetActive(true);

            var text = rowGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{row.Name} (작업속도 x{row.WorkSpeed:0.0})  {nextCost:N0}G";

            var btn = rowGO.GetComponent<UIButtonEx>();
            int tid = row.Tid;
            btn.OnSubscribeOnClick(() => OnClickHire(tid)).AddTo(btn);

            mSpawnedRows.Add(rowGO);
        }
    }

    private void OnClickHire(int tid)
    {
        if (!GameInstance.Model.Staff.TryHireStaff(tid))
        {
            Logger.Log("[UIPopupJobOffice] 골드가 부족합니다.");
            return;
        }

        RefreshList();
    }
}
