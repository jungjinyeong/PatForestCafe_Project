using UnityEngine;

public class Animator2D : AnimatorBase
{
    private Animator mAnimator;

    protected override void Awake()
    {
        base.Awake();

        mAnimator = GetComponent<Animator>();
    }

    public override void PlayAnimation(string animName, float startTime = 0, bool loop = false)
    {
        base.PlayAnimation(animName, startTime, loop);

        // base가 클립을 못 찾았으면 mCurrentAnim이 갱신되지 않으므로 중단
        if (mCurrentAnim == null || mCurrentAnim.name != animName) return;

        int stateHash = Animator.StringToHash(animName);
        if (!mAnimator.HasState(0, stateHash))
        {
            Logger.Warning($"[Animator2D] Animator에 '{animName}' 상태가 없음. AnimatorController를 확인하세요.");
            return;
        }

        float normalizedTime = mCurrentAnim.clip != null && mCurrentAnim.clip.length > 0f
            ? startTime / mCurrentAnim.clip.length
            : 0f;

        mAnimator.speed = 1f;
        mAnimator.Play(stateHash, 0, normalizedTime);
    }

    public override void PauseAnimation()
    {
        base.PauseAnimation();
        mAnimator.speed = 0f;
    }

    public override void ResumeAnimation()
    {
        base.ResumeAnimation();
        mAnimator.speed = 1f;
    }

    public override void StopAnimation()
    {
        base.StopAnimation();
        mAnimator.speed = 0f;
    }

    public override bool IsPaused(string animName)
    {
        return mCurrentAnim != null && mCurrentAnim.name == animName && mAnimator.speed == 0f;
    }
}
