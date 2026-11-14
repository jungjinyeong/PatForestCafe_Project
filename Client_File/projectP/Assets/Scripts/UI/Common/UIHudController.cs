using UnityEngine;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;
    // 건축(배치) 모드 조작 안내. 배치 모드일 때만 스스로 켜진다.
    [SerializeField] private UIPlacementControlGuide mPlacementControlGuide;

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

        if (mPlacementControlGuide != null)
            mPlacementControlGuide.Init();

        // 로비에서는 오른쪽 세로 탭(UILobbySidePanel)이 ToggleIngredientIsland()를 부르고, HUD 버튼은 가공섬에 있을 때(로비로 돌아가기)만 보인다.
        if (mBtnToggleIngredientIsland != null)
            mBtnToggleIngredientIsland.OnSubscribeOnClick(ToggleIngredientIsland).AddTo(this);

        mIsInIngredientIsland = false;
        RefreshToggleIcon();
    }

    public bool IsInIngredientIsland => mIsInIngredientIsland;

    // 로비 <-> 가공섬 화면 전환.
    public void ToggleIngredientIsland()
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
        if (mBtnToggleIngredientIsland != null)
            mBtnToggleIngredientIsland.gameObject.SetActive(mIsInIngredientIsland);

        if (mIconGoToIngredientIsland != null)
            mIconGoToIngredientIsland.SetActive(!mIsInIngredientIsland);

        if (mIconGoToLobby != null)
            mIconGoToLobby.SetActive(mIsInIngredientIsland);
    }
}
