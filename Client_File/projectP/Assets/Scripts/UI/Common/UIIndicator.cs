using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 화면 전환용 검은 화면(UIManager.prefab의 UI_Indicator). 검은 화면을 즉시 덮은 뒤 전환 콜백을 실행하고,
// DOTween으로 오른쪽 끝부터 왼쪽으로 걷히듯 사라진다(Filled Horizontal, Origin Left의 fillAmount 1 → 0).
public class UIIndicator : MonoBehaviour
{
    [SerializeField] private Image mCover;
    [SerializeField] private float mHoldSeconds = 0.1f;   // 전환 직후 검은 화면을 유지하는 시간
    [SerializeField] private float mWipeSeconds = 0.35f;  // 오른쪽 → 왼쪽으로 걷히는 시간
    [SerializeField] private Ease mWipeEase = Ease.InOutCubic;

    private Sequence mSequence;

    public bool IsPlaying => mSequence != null && mSequence.IsActive();

    private void Awake()
    {
        if (mCover != null)
        {
            mCover.type = Image.Type.Filled;
            mCover.fillMethod = Image.FillMethod.Horizontal;
            mCover.fillOrigin = (int)Image.OriginHorizontal.Left;
            mCover.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        mSequence?.Kill();
        mSequence = null;
    }

    // 검은 화면으로 덮고 onCovered(층 좌표 이동 등)를 실행한 뒤, 오른쪽에서 왼쪽으로 걷어낸다.
    public void Play(Action onCovered)
    {
        mSequence?.Kill();
        mSequence = null;

        if (mCover == null)
        {
            onCovered?.Invoke();
            return;
        }

        // UI_Indicator 루트는 prefab에서 꺼 둔 상태라 재생할 때 켠다(처음 켜질 때 Awake가 먼저 돈다).
        gameObject.SetActive(true);
        mCover.fillAmount = 1f;
        mCover.gameObject.SetActive(true);

        onCovered?.Invoke();

        mSequence = DOTween.Sequence()
            .AppendInterval(mHoldSeconds)
            .Append(mCover.DOFillAmount(0f, mWipeSeconds).SetEase(mWipeEase))
            .SetUpdate(true)
            .OnComplete(Finish);
    }

    private void Finish()
    {
        mSequence = null;

        if (mCover != null)
            mCover.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}
