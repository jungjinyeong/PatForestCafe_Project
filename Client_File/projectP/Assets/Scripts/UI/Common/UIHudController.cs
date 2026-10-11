using UnityEngine;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;

    //[Header("Only Lobby")]
    

    [Header("Ingredient Island Toggle")]
    [SerializeField, FormerlySerializedAs("mBtnToggleMaterialIsland")] private UIButtonEx mBtnToggleIngredientIsland;
    [SerializeField, FormerlySerializedAs("mIconGoToMaterialIsland")] private GameObject mIconGoToIngredientIsland;
    [SerializeField] private GameObject mIconGoToLobby;

    private bool mIsInitialized;
    private bool mIsInIngredientIsland;

    public void Init()
    {
        if (mIsInitialized)
            return;
        mIsInitialized = true;

        mTopbarInfo.Init();

        mBtnToggleIngredientIsland.OnSubscribeOnClick(OnClickToggleIngredientIsland).AddTo(this);

        mIsInIngredientIsland = false;
        RefreshToggleIcon();
    }

    private void OnClickToggleIngredientIsland()
    {
        mIsInIngredientIsland = !mIsInIngredientIsland;

        if (mIsInIngredientIsland)
            GameInstance.UI.Open<UIRootIngredientIsland, UIRootIngredientIsland.Param>(eUIType.RootIngredientIsland, new UIRootIngredientIsland.Param());
        else
            GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.RootLobby, new UIRootLobby.Param());

        RefreshToggleIcon();
    }

    private void RefreshToggleIcon()
    {
        if (mIconGoToIngredientIsland != null)
            mIconGoToIngredientIsland.SetActive(!mIsInIngredientIsland);

        if (mIconGoToLobby != null)
            mIconGoToLobby.SetActive(mIsInIngredientIsland);
    }
}
