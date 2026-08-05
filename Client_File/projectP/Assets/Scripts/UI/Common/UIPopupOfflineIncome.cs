using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIPopupOfflineIncome : UIWndBase, IUIParam<UIPopupOfflineIncome.Param>
{
    public struct Param
    {
        public int Gold;
        public double OfflineSeconds;
    }

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI mTextGold;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnConfirm;

    public override eUIType GetUIType() => eUIType.UIPopupOfflineIncome;

    public override void Init()
    {
        base.Init();

        mBtnConfirm.OnSubscribeOnClick(SelfClose).AddTo(this);
    }

    public void Set(Param param)
    {
        if (mTextGold == null) return;

        mTextGold.SetTextEx($"자리를 비운 동안 {param.Gold:N0} Gold를 획득했습니다!");
    }
}
