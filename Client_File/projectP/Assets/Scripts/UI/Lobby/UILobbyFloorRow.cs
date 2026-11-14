using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Extension;

// 층 이동 목록(UILobbyFloorList)의 한 줄. "B1 · 지하 매장" + 현재 층 강조 + 잠긴 층 표시.
public class UILobbyFloorRow : MonoBehaviour
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private Image mBg;
    [SerializeField] private TextMeshProUGUI mText;
    [Tooltip("잠긴 층에만 켜는 표시(자물쇠/\"잠김\"). 비어 있어도 된다.")]
    [SerializeField] private GameObject mLockMark;

    [SerializeField] private Color mBgNormal = Color.white;
    [SerializeField] private Color mBgCurrent = new Color(0.45f, 0.62f, 0.35f);
    [SerializeField] private Color mTextNormal = new Color(0.35f, 0.22f, 0.12f);
    [SerializeField] private Color mTextCurrent = Color.white;
    [SerializeField] private Color mTextLocked = new Color(0.35f, 0.22f, 0.12f, 0.45f);

    public int FloorId { get; private set; }

    private Action mOnClick;
    private bool mIsBound;

    public void Bind(FloorModel.FloorInfo info, Action onClick)
    {
        FloorId = info.Id;
        mOnClick = onClick;
        mText.SetTextEx(string.IsNullOrEmpty(info.Desc) ? info.Name : $"{info.Name} · {info.Desc}");

        if (mIsBound) return;
        mIsBound = true;
        mButton.OnSubscribeOnClick(() => mOnClick?.Invoke()).AddTo(this);
    }

    public void SetState(bool isCurrent, bool isLocked)
    {
        if (mBg != null)
            mBg.color = isCurrent ? mBgCurrent : mBgNormal;

        mText.color = isCurrent ? mTextCurrent : isLocked ? mTextLocked : mTextNormal;

        if (mLockMark != null)
            mLockMark.SetActive(isLocked);
    }
}
