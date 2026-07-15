
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class UIPopupOption : UIWndBase, IUIParam<Param>
{
    [Header("Volume")]
    [SerializeField] private Slider mSliderBgmVolume;
    [SerializeField] private Slider mSliderSfxVolume;

    public override eUIType GetUIType() => eUIType.UIPopupOption;

    public override void Init()
    {
        base.Init();

        mSliderBgmVolume.OnValueChangedAsObservable()
            .Subscribe(OnChangeBgmVolume)
            .AddTo(this);

        mSliderSfxVolume.OnValueChangedAsObservable()
            .Subscribe(OnChangeSfxVolume)
            .AddTo(this);
    }

    public void Set(Param param)
    {
    }

    public override void Open()
    {
        base.Open();

        mSliderBgmVolume.SetValueWithoutNotify(GameInstance.Sound.BgmVolume);
        mSliderSfxVolume.SetValueWithoutNotify(GameInstance.Sound.SfxVolume);
    }

    private void OnChangeBgmVolume(float value)
    {
        GameInstance.Sound.SetBgmVolume(value);
    }

    private void OnChangeSfxVolume(float value)
    {
        GameInstance.Sound.SetSfxVolume(value);
    }
}
