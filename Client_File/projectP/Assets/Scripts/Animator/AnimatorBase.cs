using System.Collections.Generic;
using UnityEngine;

public class AnimatorBase : MonoBehaviour
{
    [System.Serializable]
    public class AnimationData
    {
        public string name;
        public AnimationClip clip;
        public bool loop;
    }

    [SerializeField]
    List<AnimationData> mAnimationList = new List<AnimationData>();

    protected Dictionary<string, AnimationData> mClipTable = new();
    protected AnimationData mCurrentAnim;
    protected float mTime;
    protected bool mIsPlaying;

    protected virtual void Awake()
    {
        foreach (var anim in mAnimationList)
        {
            if (anim.clip != null)
                mClipTable[anim.name] = anim;
        }
    }

    public virtual void PlayAnimation(string animName, float startTime = 0f, bool loop = false)
    {
        if (!mClipTable.TryGetValue(animName, out var anim))
        {
            Logger.Warning($"[Anim] {animName} 애니메이션을 찾을 수 없습니다.");
            return;
        }

        mCurrentAnim = anim;
        mTime = startTime;
        mIsPlaying = true;
    }

    public virtual void PauseAnimation()
    {
        mIsPlaying = false;
    }

    public virtual void ResumeAnimation()
    {
        if (mCurrentAnim != null)
            mIsPlaying = true;
    }

    public virtual void StopAnimation()
    {
        mIsPlaying = false;
    }

    public string CurrentAnimationName => mCurrentAnim?.name ?? string.Empty;

    public virtual bool IsPlaying(string animName)
    {
        return mIsPlaying && mCurrentAnim != null && mCurrentAnim.name == animName;
    }

    public virtual bool IsPaused(string animName)
    {
        return !mIsPlaying && mCurrentAnim != null && mCurrentAnim.name == animName;
    }
}
