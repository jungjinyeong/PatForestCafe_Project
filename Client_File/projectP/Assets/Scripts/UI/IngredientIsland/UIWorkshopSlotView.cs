using System;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;
using UnityEngine.Serialization;

// 드롭다운 없이 버튼만으로 슬롯의 생산 재료를 순환 선택/배치·해제하는 단일 슬롯 뷰.
public class UIWorkshopSlotView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mTextStatus;
    [SerializeField, FormerlySerializedAs("mBtnCycleMaterial")] private UIButtonEx mBtnCycleIngredient;
    [SerializeField] private UIButtonEx mBtnToggleAssign;

    private int mSlotIndex;

    public void Init(int slotIndex, Action<int> onClickCycle, Action<int> onClickToggle)
    {
        mSlotIndex = slotIndex;

        mBtnCycleIngredient.OnSubscribeOnClick(() => onClickCycle?.Invoke(mSlotIndex)).AddTo(this);
        mBtnToggleAssign.OnSubscribeOnClick(() => onClickToggle?.Invoke(mSlotIndex)).AddTo(this);
    }

    public void SetStatusText(string text)
    {
        mTextStatus?.SetTextEx(text);
    }
}
