using UnityEngine;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;
    // 건축(배치) 모드 조작 안내. 배치 모드일 때만 스스로 켜진다.
    [SerializeField] private UIPlacementControlGuide mPlacementControlGuide;


    private bool mIsInitialized;
    
    public void Init()
    {
        if (mIsInitialized)
            return;

        mIsInitialized = true;

        mTopbarInfo.Init();

        if (mPlacementControlGuide != null)
            mPlacementControlGuide.Init();
    }
}
