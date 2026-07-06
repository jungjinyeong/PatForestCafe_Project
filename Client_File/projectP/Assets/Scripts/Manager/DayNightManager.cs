using UniRx;
using UnityEngine;

public class DayNightManager : MonoBehaviour, IManager
{
    [Header("Night Alpha Curve")]
    [Tooltip("X축: 시간(0~24), Y축: 밤 오버레이 알파(0=낮, 1=밤)")]
    [SerializeField] private AnimationCurve mNightAlphaCurve = new AnimationCurve(
        new Keyframe(0f,  1f),
        new Keyframe(6f,  1f),
        new Keyframe(8f,  0f),
        new Keyframe(18f, 0f),
        new Keyframe(20f, 1f),
        new Keyframe(24f, 1f)
    );

    public IReadOnlyReactiveProperty<float> NightAlpha => mNightAlpha;
    private readonly ReactiveProperty<float> mNightAlpha = new ReactiveProperty<float>();

    public void Init()
    {
        GameInstance.Time.CurrentHour
            .Select(hour => mNightAlphaCurve.Evaluate(hour))
            .Subscribe(alpha => mNightAlpha.Value = alpha)
            .AddTo(this);
    }

    public void Subscribe() { }
    public void Clear() { }
    public void Destory() => mNightAlpha.Dispose();
}
