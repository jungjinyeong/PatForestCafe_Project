using UnityEngine;
using UniRx;
using Extension;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;

    //[Header("Only Lobby")]
    

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

        mIsInMaterialIsland = false;
        RefreshToggleIcon();
    }

    private void OnClickToggleMaterialIsland()
    {
        mIsInMaterialIsland = !mIsInMaterialIsland;

        if (mIsInMaterialIsland)
            GameInstance.UI.Open<UIRootMaterialIsland, UIRootMaterialIsland.Param>(eUIType.RootMaterialIsland, new UIRootMaterialIsland.Param());
        else
            GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.RootLobby, new UIRootLobby.Param());

        RefreshToggleIcon();
    }

    private void RefreshToggleIcon()
    {
        if (mIconGoToMaterialIsland != null)
            mIconGoToMaterialIsland.SetActive(!mIsInMaterialIsland);

        if (mIconGoToLobby != null)
            mIconGoToLobby.SetActive(mIsInMaterialIsland);
    }
}
