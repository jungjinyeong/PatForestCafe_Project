using UnityEngine;

public class CharBase : MonoBehaviour
{
    protected Animator2D mAnimator2D;
    protected SpriteRenderer mSpriteRenderer;

    protected virtual void Awake()
    {
        mAnimator2D = GetComponentInChildren<Animator2D>();
        mSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
}
