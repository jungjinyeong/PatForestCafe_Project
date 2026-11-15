using System;
using UniRx;
using UnityEngine;

// 화면 전환 연출(UIManager.prefab에 넣은 UI_Transition 프리팹). 층 이동 등에서 화면을 덮은 동안 전환 작업을 한다.
// 연출은 Animator의 두 상태로 만든다 — 스크립트는 순서만 맡고, 그림/움직임은 Animation 창에서 클립을 고치면 된다.
//   In  : 화면을 덮는 클립. 끝나는 시점(마지막 프레임)에 화면이 다 가려져 있어야 한다.
//   Out : 화면을 걷어내는 클립.
// 순서: 켜기 → In 재생 → In 끝 → onCovered 실행(카메라 이동 등) → mHoldSeconds 대기 → Out 재생 → Out 끝 → 끄기.
// Animator는 Unscaled Time으로 돈다(게임 시간이 멈춘 영업 준비 중에도 재생). 재생 중에는 덮개(raycastTarget)가 입력을 막는다.
// Animator나 In 상태가 없으면 연출 없이 onCovered만 바로 실행한다.
public class UITransition : MonoBehaviour
{
    private enum ePhase
    {
        None,
        Covering,   // In 재생 중
        Revealing,  // 대기 + Out 재생 중
    }

    [SerializeField] private Animator mAnimator;
    [SerializeField] private string mInState = "In";
    [SerializeField] private string mOutState = "Out";
    [Tooltip("다 덮은 뒤 Out을 시작하기 전 기다리는 시간(초, 실제 시간).")]
    [SerializeField] private float mHoldSeconds = 0.1f;

    private readonly SerialDisposable mStep = new SerialDisposable();
    private ePhase mPhase = ePhase.None;
    private Action mOnCovered;

    public bool IsPlaying => mPhase != ePhase.None;

    private void Awake()
    {
        if (mAnimator == null)
            mAnimator = GetComponent<Animator>();
        if (mAnimator != null)
            mAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void OnDestroy()
    {
        mStep.Dispose();
    }

    // 화면을 덮고 onCovered를 실행한 뒤 걷어낸다.
    // 덮는 중(In)에 다시 부르면 마지막 onCovered 하나만 실행한다(층 이동을 빠르게 연달아 누른 경우 마지막 층으로).
    // 걷는 중(대기·Out)에 다시 부르면 In부터 다시 시작한다.
    public void Play(Action onCovered)
    {
        if (mPhase == ePhase.Covering)
        {
            mOnCovered = onCovered;
            return;
        }

        // UI_Transition 루트는 꺼 둔 상태라 재생할 때 켠다(처음 켜질 때 Awake가 먼저 돈다).
        gameObject.SetActive(true);

        if (mAnimator == null || !HasState(mInState))
        {
            Finish();
            onCovered?.Invoke();
            return;
        }

        mOnCovered = onCovered;
        mPhase = ePhase.Covering;
        PlayState(mInState);
        mStep.Disposable = WaitStateEnd(mInState).Subscribe(_ => OnCovered());
    }

    private void OnCovered()
    {
        mPhase = ePhase.Revealing;

        var callback = mOnCovered;
        mOnCovered = null;
        callback?.Invoke();

        mStep.Disposable = Observable.Timer(TimeSpan.FromSeconds(mHoldSeconds), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ => StartReveal());
    }

    private void StartReveal()
    {
        if (!HasState(mOutState))
        {
            Finish();
            return;
        }

        PlayState(mOutState);
        mStep.Disposable = WaitStateEnd(mOutState).Subscribe(_ => Finish());
    }

    private void Finish()
    {
        mStep.Disposable = null;
        mPhase = ePhase.None;
        gameObject.SetActive(false);
    }

    private void PlayState(string state)
    {
        mAnimator.Play(state, 0, 0f);
        // 같은 프레임에 상태 정보가 바뀌도록 바로 한 번 평가한다(이전 상태의 끝을 새 상태의 끝으로 잘못 읽지 않게).
        mAnimator.Update(0f);
    }

    // state가 마지막 프레임까지 재생되면 한 번 알린다(전이 중이면 기다린다).
    private IObservable<Unit> WaitStateEnd(string state)
    {
        return Observable.EveryUpdate()
            .Where(_ =>
            {
                var info = mAnimator.GetCurrentAnimatorStateInfo(0);
                return info.IsName(state) && info.normalizedTime >= 1f && !mAnimator.IsInTransition(0);
            })
            .First()
            .AsUnitObservable();
    }

    private bool HasState(string state)
    {
        return mAnimator != null && mAnimator.runtimeAnimatorController != null
            && mAnimator.HasState(0, Animator.StringToHash(state));
    }
}
