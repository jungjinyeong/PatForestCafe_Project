using UnityEngine;
using UniRx;
using Extension;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;

    [Header("Only Lobby")]
    [SerializeField] private UIButtonEx mBtnBread;


    [Header("Material Island Toggle")]
    [SerializeField] private UIButtonEx mBtnToggleMaterialIsland;
    [SerializeField] private GameObject mIconGoToMaterialIsland;
    [SerializeField] private GameObject mIconGoToLobby;

    private bool mIsInitialized;
    private bool mIsInMaterialIsland;

    public void Init()
    {
        if (mIsInitialized)
            return;
        mIsInitialized = true;

        mTopbarInfo.Init();

        mBtnToggleMaterialIsland.OnSubscribeOnClick(OnClickToggleMaterialIsland).AddTo(this);

        mBtnBread.OnSubscribeOnClick(OnClickBread).AddTo(this);

        mIsInMaterialIsland = false;
        RefreshToggleIcon();
    }

    private void OnClickToggleMaterialIsland()
    {
        mIsInMaterialIsland = !mIsInMaterialIsland;

        if (mIsInMaterialIsland)
            GameInstance.UI.Open<UIRootMaterialIsland, UIRootMaterialIsland.Param>(eUIType.UIRootMaterialIsland, new UIRootMaterialIsland.Param());
        else
            GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.UIRootLobby, new UIRootLobby.Param());

        RefreshToggleIcon();
    }

    private void RefreshToggleIcon()
    {
        if (mIconGoToMaterialIsland != null)
            mIconGoToMaterialIsland.SetActive(!mIsInMaterialIsland);

        if (mIconGoToLobby != null)
            mIconGoToLobby.SetActive(mIsInMaterialIsland);
    }

    private void OnClickBread()
    {
        GameInstance.UI.Open<UIPopupBreadSelect, UIPopupBreadSelect.Param>(eUIType.UIPopupBreadSelect, 
            new UIPopupBreadSelect.Param());
    }
}
