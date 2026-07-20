using UnityEngine;
using UniRx;
using Extension;

public class UIPlacementConfirm : MonoBehaviour
{
    [SerializeField] private GameObject mRoot;
    [SerializeField] private UIButtonEx mBtnConfirm;
    [SerializeField] private UIButtonEx mBtnCancel;

    public void Init()
    {
        mBtnConfirm.OnSubscribeOnClick(OnClickConfirm).AddTo(this);
        mBtnCancel.OnSubscribeOnClick(OnClickCancel).AddTo(this);

        GameInstance.Model.Placement.IsPlacing
            .Subscribe(isPlacing => mRoot.SetActive(isPlacing))
            .AddTo(this);
    }

    private void OnClickConfirm()
    {
        GameInstance.Model.Placement.Confirm();
    }

    private void OnClickCancel()
    {
        GameInstance.Model.Placement.Cancel();
    }
}
