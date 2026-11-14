using System.Collections.Generic;

// 채용한 직원 명단(UIStaffCellData) 만들기. 로비 패널 직원 화면(UISideStaffView)과 직원 상세 팝업(UIPopupStaff)이 같이 쓴다.
public static class StaffEntries
{
    public static List<UIStaffCellData> Get()
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
}
