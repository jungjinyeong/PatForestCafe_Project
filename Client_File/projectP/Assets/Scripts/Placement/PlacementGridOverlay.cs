using UniRx;
using UnityEngine;

// G키 그리드 오버레이 표시 담당. 배치 모드이면서 그리드가 켜져 있을 때(PlacementModel.IsGridOverlayOn),
// 지금 보고 있는 층(FloorModel.ViewingFloor)의 배치 영역에만 칸 테두리를 보여 준다. 켜짐 여부는 PlayerPrefs에 기억한다.
// UIRootLobby.Init()에서 AddComponent 후 Init()을 호출한다(GameInstance 의존 초기화 규칙).
public class PlacementGridOverlay : MonoBehaviour
{
    private const string PREFS_KEY_GRID_ON = "Placement.GridOverlayOn";

    private TilePlacementArea[] mAreas;

    public void Init()
    {
        mAreas = FindObjectsByType<TilePlacementArea>(FindObjectsSortMode.None);

        var placement = GameInstance.Model.Placement;
        placement.SetGridOverlay(PlayerPrefs.GetInt(PREFS_KEY_GRID_ON, 0) == 1);

        placement.IsGridOverlayOn
            .Skip(1)
            .Subscribe(SaveGridOverlayOn)
            .AddTo(this);

        placement.IsEditMode
            .CombineLatest(placement.IsGridOverlayOn, GameInstance.Model.Floor.ViewingFloor,
                (isEditMode, isGridOn, viewingFloor) => isEditMode && isGridOn ? viewingFloor : 0)
            .DistinctUntilChanged()
            .Subscribe(Refresh)
            .AddTo(this);
    }

    private static void SaveGridOverlayOn(bool isOn)
    {
        PlayerPrefs.SetInt(PREFS_KEY_GRID_ON, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // visibleFloor = 그리드를 보여 줄 층(0이면 모두 숨김).
    private void Refresh(int visibleFloor)
    {
        foreach (var area in mAreas)
        {
            if (area != null)
                area.SetGridOverlayVisible(visibleFloor != 0 && area.Floor == visibleFloor);
        }
    }
}
