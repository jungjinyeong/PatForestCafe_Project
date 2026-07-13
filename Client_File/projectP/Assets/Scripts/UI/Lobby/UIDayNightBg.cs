using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class UIDayNightBg : UIBase
{
    [Header("Night Overlay")]
    [SerializeField] private Image mNightOverlay;

    public void Init()
    {
        GameInstance.DayNight.NightAlpha
            .Subscribe(SetNightAlpha)
            .AddTo(this);
    }

    private void SetNightAlpha(float alpha)
    {
        var color = mNightOverlay.color;
        color.a = alpha;
        mNightOverlay.color = color;
    }
}
