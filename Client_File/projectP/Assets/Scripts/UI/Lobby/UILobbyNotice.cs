using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 로비 사이드바 알림 패널(UI_Root_Lobby/Sidebar/NoticeSlot/NoticePanel). NoticeModel의 최신 알림을 줄 수만큼 보여준다.
// UIRootLobby.Init()에서 Init()을 호출한다.
public class UILobbyNotice : MonoBehaviour
{
    private const string EMPTY_MESSAGE = "새 알림이 없어요";

    [SerializeField] private TextMeshProUGUI[] mTextLines;

    private bool mIsInitialized;

    public void Init()
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        GameInstance.Model.Notice.Notices.ObserveCountChanged(true)
            .ThrottleFrame(1)
            .Subscribe(_ => Refresh())
            .AddTo(this);
        Refresh();
    }

    private void Refresh()
    {
        var notices = GameInstance.Model.Notice.Notices;

        for (int i = 0; i < mTextLines.Length; i++)
        {
            if (notices.Count == 0)
            {
                mTextLines[i].SetTextEx(i == 0 ? $"• {EMPTY_MESSAGE}" : string.Empty);
                continue;
            }

            mTextLines[i].SetTextEx(i < notices.Count ? $"• {notices[i].Message}" : string.Empty);
        }
    }
}
