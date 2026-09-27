using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

// 오븐 굽기 트레이 1칸. 잠김 / 비어 있음 / 설정 완료 / 굽는 중 4가지 상태를 보여준다.
public class UIOvenTraySlot : UIBase
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextState;

    [Header("Roots")]
    [SerializeField] private GameObject mLockedRoot;
    [SerializeField] private GameObject mEmptyRoot;
    [SerializeField] private GameObject mRecipeRoot;

    [Header("Recipe")]
    [SerializeField] private TextMeshProUGUI mTextRecipe;
    [SerializeField] private TextMeshProUGUI mTextQuantity;
    [SerializeField] private TextMeshProUGUI mTextQuality;
    [SerializeField] private TextMeshProUGUI mTextAction;
    [SerializeField] private GameObject mProgressRoot;
    [SerializeField] private Image mImgProgress;

    public void Init(Action onClick)
    {
        mButton.OnSubscribeOnClick(onClick).AddTo(this);
    }

    public void Refresh(int index, OvenModel oven)
    {
        var tray = oven.Trays[index];
        bool unlocked = oven.IsTrayUnlocked(index);

        mTextTitle.SetTextEx($"트레이 {index + 1}");
        mButton.interactable = unlocked && tray.State != eOvenTrayState.Baking;

        mLockedRoot.SetActive(!unlocked);
        mEmptyRoot.SetActive(unlocked && tray.IsEmpty);
        mRecipeRoot.SetActive(unlocked && !tray.IsEmpty);

        if (!unlocked)
        {
            mTextState.SetTextEx(string.Empty);
            return;
        }

        switch (tray.State)
        {
            case eOvenTrayState.Empty:
                mTextState.SetTextEx("비어 있음");
                return;
            case eOvenTrayState.Ready:
                mTextState.SetTextEx("설정 완료");
                break;
            case eOvenTrayState.Baking:
                long remain = oven.GetRemainingSeconds(index);
                mTextState.SetTextEx($"굽는 중 {remain}초");
                break;
        }

        mTextRecipe.SetTextEx(GameInstance.Table.Get<CTable.MenuItemRow>(tray.RecipeTid)?.Name ?? tray.RecipeTid.ToString());
        mTextQuantity.SetTextEx($"x {tray.Quantity}개");
        mTextQuality.SetTextEx($"<color={BreadQuality.GetColorHex(tray.Quality)}>{BreadQuality.GetName(tray.Quality)}</color>");

        bool baking = tray.State == eOvenTrayState.Baking;
        mTextAction.SetTextEx(baking ? string.Empty : "수정하기");
        mProgressRoot.SetActive(baking);
        if (baking && mImgProgress != null)
        {
            float total = Mathf.Max(1, tray.BakeDurationSeconds > 0 ? tray.BakeDurationSeconds : oven.BakeSeconds);
            mImgProgress.fillAmount = 1f - Mathf.Clamp01(oven.GetRemainingSeconds(index) / total);
        }
    }
}
