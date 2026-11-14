using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Extension;

// 로비 층 이동 목록(UI_Root_Lobby/Sidebar/FloorList). 오른쪽 세로 탭의 [층 이동]이 켜고 끈다.
// 위층부터 "T3 · 하늘 테라스" … "B3 · 깊은 지하 매장" 순으로 보여 주고, 지금 보고 있는 층을 강조한다.
// 잠긴 층은 흐리게 표시하고 누르면 이동하지 않고 층 해금 팝업을 연다.
// UILobbySidePanel.Init()(← UIRootLobby.Init())에서 Init()을 호출한다.
public class UILobbyFloorList : MonoBehaviour
{
    [SerializeField] private UIButtonEx mBtnClose;
    [Tooltip("줄 템플릿(꺼 둔다). 층 수만큼 복제한다.")]
    [SerializeField] private UILobbyFloorRow mRowTemplate;
    [SerializeField] private Transform mRowParent;

    private readonly List<UILobbyFloorRow> mRows = new List<UILobbyFloorRow>();
    private LobbyFloorCameraController mFloorCamera;
    private bool mIsInitialized;

    public bool IsOpen => gameObject.activeSelf;

    // 열림/닫힘이 바뀔 때(층을 골라 스스로 닫힐 때 포함). 세로 탭 선택 표시를 맞추는 데 쓴다.
    public event System.Action<bool> OpenChanged;

    // 프리팹에서는 켜 둔 상태여야 한다(구독을 붙인 뒤 여기서 끈다).
    public void Init(LobbyFloorCameraController floorCamera)
    {
        if (mIsInitialized) return;
        mIsInitialized = true;
        mFloorCamera = floorCamera;

        mRowTemplate.gameObject.SetActive(false);

        var floorModel = GameInstance.Model.Floor;
        var floors = floorModel.Floors;
        for (int i = floors.Count - 1; i >= 0; i--)
        {
            int floorId = floors[i].Id;
            var row = Instantiate(mRowTemplate, mRowParent);
            row.gameObject.SetActive(true);
            row.Bind(floors[i], () => OnClickFloor(floorId));
            mRows.Add(row);
        }

        if (mBtnClose != null)
            mBtnClose.OnSubscribeOnClick(() => SetOpen(false)).AddTo(this);

        floorModel.ViewingFloor.CombineLatest(floorModel.UnlockedCount, (viewing, unlocked) => Unit.Default)
            .Subscribe(_ => Refresh())
            .AddTo(this);

        gameObject.SetActive(false);
    }

    public void SetOpen(bool open)
    {
        if (gameObject.activeSelf == open)
            return;

        gameObject.SetActive(open);
        if (open)
            Refresh();
        OpenChanged?.Invoke(open);
    }

    private void OnClickFloor(int floorId)
    {
        if (!GameInstance.Model.Floor.IsUnlocked(floorId))
        {
            GameInstance.UI.Open<UIFloorUnlock, UIFloorUnlock.Param>(eUIType.PopupFloorUnlock, new UIFloorUnlock.Param());
            return;
        }

        if (mFloorCamera != null)
            mFloorCamera.MoveToFloor(floorId);
        SetOpen(false);
    }

    private void Refresh()
    {
        var floorModel = GameInstance.Model.Floor;
        int viewing = floorModel.ViewingFloor.Value;
        foreach (var row in mRows)
            row.SetState(row.FloorId == viewing, !floorModel.IsUnlocked(row.FloorId));
    }
}
