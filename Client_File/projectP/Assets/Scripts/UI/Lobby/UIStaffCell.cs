using System;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

public class UIStaffCellData
{
    public int Index;   // 명단 순번(0부터)
    public int Tid;
    public string Name;
    public float WorkSpeed;
}

// 직원 관리(UIPopupStaff) 명단 한 칸. 캐릭터 초상화 리소스가 없어 이름 첫 글자를 대신 표시한다.
public class UIStaffCell : UIBase
{
    [SerializeField] private UIButtonEx mBtnSelect;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextNumber;
    [SerializeField] private GameObject mSelectedMark;

    public UIStaffCellData Data { get; private set; }

    private Action<UIStaffCell> mOnSelect;
    private bool mIsBound;

    public void Bind(Action<UIStaffCell> onSelect)
    {
        mOnSelect = onSelect;
        if (mIsBound) return;

        mIsBound = true;
        mBtnSelect.OnSubscribeOnClick(() => mOnSelect?.Invoke(this)).AddTo(this);
    }

    public void SetData(UIStaffCellData data)
    {
        Data = data;
        mTextGlyph.SetTextEx(string.IsNullOrEmpty(data.Name) ? "?" : data.Name.Substring(0, 1));
        mTextName.SetTextEx(data.Name);
        mTextNumber.SetTextEx($"#{data.Index + 1}");
    }

    public void SetSelected(bool isSelected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(isSelected);
    }
}
