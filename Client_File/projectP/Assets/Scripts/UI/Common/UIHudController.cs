using UnityEngine;

public class UIHudController : MonoBehaviour
{
    [SerializeField] private UITopbarInfo mTopbarInfo;

    private bool mIsInitialized;

    public void Init()
    {
        if (mIsInitialized)
            return;
        mIsInitialized = true;

        mTopbarInfo.Init();
    }
}
