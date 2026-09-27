using UniRx;
using UnityEngine;
using Sirenix.OdinInspector;

public class TimeManager : MonoBehaviour, IManager
{
    [Header("Settings")]
    [SerializeField] private float mDayDurationSeconds = 120f;
    [SerializeField] private float mStartHour = 8f;

    [Header("Debug")]
    [SerializeField] [Range(0f, 24f)] private float mDebugHour = 8f;
    [SerializeField] [Range(0f, 20f)] private float mTimeScale = 1f;
    [SerializeField] private bool mIsPaused = false;

    public IReadOnlyReactiveProperty<float> CurrentHour => mCurrentHour;
    private readonly ReactiveProperty<float> mCurrentHour = new ReactiveProperty<float>();

    public void Init()
    {
        // 하루 길이·시작 시각(= 개점 시각)은 GameTime.csv 값으로 덮어쓴다(테이블은 GameInstance.Init()에서 먼저 로드됨).
        mDayDurationSeconds = BusinessModel.DayDurationSeconds;
        mStartHour = BusinessModel.OpenHour;

        mCurrentHour.Value = mStartHour;
        mDebugHour = mStartHour;

        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                if (!mIsPaused)
                {
                    float hoursPerSecond = 24f / mDayDurationSeconds * mTimeScale;
                    mCurrentHour.Value = (mCurrentHour.Value + hoursPerSecond * Time.deltaTime) % 24f;
                    mDebugHour = mCurrentHour.Value;
                }
                else
                {
                    mCurrentHour.Value = mDebugHour;
                }
            })
            .AddTo(this);
    }

    public void SetHour(float hour)
    {
        mDebugHour = Mathf.Clamp(hour, 0f, 24f);
        mCurrentHour.Value = mDebugHour;
    }

    public void Pause() => mIsPaused = true;
    public void Resume() => mIsPaused = false;
    public void SetTimeScale(float scale) => mTimeScale = scale;

    [Button("일시정지 / 재개"), PropertyOrder(10)]
    private void TogglePause() => mIsPaused = !mIsPaused;

    [HorizontalGroup("TimeButtons"), Button("새벽 06:00"), PropertyOrder(11)]
    private void SetDawn() => SetHour(6f);

    [HorizontalGroup("TimeButtons"), Button("정오 12:00"), PropertyOrder(11)]
    private void SetNoon() => SetHour(12f);

    [HorizontalGroup("TimeButtons"), Button("저녁 18:00"), PropertyOrder(11)]
    private void SetDusk() => SetHour(18f);

    [HorizontalGroup("TimeButtons"), Button("밤 22:00"), PropertyOrder(11)]
    private void SetMidnight() => SetHour(22f);

    public void Subscribe() { }
    public void Clear() => mCurrentHour.Value = mStartHour;
    public void Destory() => mCurrentHour.Dispose();
}
