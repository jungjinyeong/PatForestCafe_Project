using System;
using UnityEngine; 

public class Animator2D : AnimatorBase
{
    protected override void Awake()
    {
        base.Awake();

        mAnimation = GetComponent<Animation>();

        foreach (var anim in mClipTable)
        {
            if (anim.Value.clip == null) continue;

#if UNITY_EDITOR
            // Animation 컴포넌트는 Legacy 클립만 동작함. 에디터에서 자동으로 변환.
            if (!anim.Value.clip.legacy)
            {
                anim.Value.clip.legacy = true;
                UnityEditor.EditorUtility.SetDirty(anim.Value.clip);
            }
#endif
            mAnimation.AddClip(anim.Value.clip, anim.Value.name);

            if (mAnimation[anim.Value.name] == null)
                Debug.LogWarning($"[Animator2D] '{anim.Value.name}' 클립 등록 실패. AnimationClip을 Legacy로 설정하세요.");
        }
    }

    public override void PlayAnimation(string animName, float startTime = 0, bool loop = false)
    {
        base.PlayAnimation(animName, startTime, loop);

        // base가 클립을 못 찾았으면 mCurrentAnim이 갱신되지 않으므로 중단
        if (mCurrentAnim == null || mCurrentAnim.name != animName) return;

        if (mAnimation[animName] == null)
        {
            Debug.LogWarning($"[Animator2D] Animation 컴포넌트에 '{animName}' 상태가 없음. Legacy 클립인지 확인하세요.");
            return;
        }

        mAnimation[animName].wrapMode = mCurrentAnim.loop ? WrapMode.Loop : WrapMode.Once;
        mAnimation.Play(animName);
    }

    //TODO: Pause, Resume ���� �ʿ�
    override public void PauseAnimation()
    {
        if (mAnimation.clip != null)
        {
            mAnimation.Stop();
        }
    }

    override public void ResumeAnimation()
    {
        if (mAnimation.clip != null)
        {
            mAnimation.Play();
        }
    }

    override public void StopAnimation()
    {
        if (mAnimation.clip != null)
        {
            mAnimation.Stop();
        }
    }

    override public bool IsPaused(string animName)
    {
        return mAnimation.clip != null && mAnimation.isPlaying == false && mAnimation.clip.name == animName;
    }
}