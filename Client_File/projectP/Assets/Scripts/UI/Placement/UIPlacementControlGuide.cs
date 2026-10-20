using System.Text;
using TMPro;
using UniRx;
using UnityEngine;

// 건축(배치) 모드 조작 안내(프리팹 UI_PlacementControlGuide, UI_HudController 안에 배치). UIHudController.Init()에서 Init을 호출한다.
// 배치 모드(PlacementModel.IsEditMode)일 때만 보이고, 들고 있는지(IsPlacing)에 따라 안내 줄이 바뀐다.
// 키 이름은 PlacementInputController의 실제 바인딩에서 읽으므로 키를 바꾸면 안내도 같이 바뀐다.
public class UIPlacementControlGuide : MonoBehaviour
{
    [SerializeField] private GameObject mRoot;
    [SerializeField] private TextMeshProUGUI mTextGuide;

    private readonly StringBuilder mBuilder = new StringBuilder();
    private readonly SerialDisposable mBindingsChangedSubscription = new SerialDisposable();
    private bool mIsInitialized;

    public void Init()
    {
        if (mIsInitialized)
            return;
        mIsInitialized = true;

        mBindingsChangedSubscription.AddTo(this);

        var placement = GameInstance.Model.Placement;

        placement.IsEditMode
            .CombineLatest(placement.IsPlacing, placement.IsDemolishMode, PlacementInputController.Current,
                (isEditMode, isPlacing, isDemolishMode, controller) => isEditMode)
            .Subscribe(_ => Refresh())
            .AddTo(this);

        // 컨트롤러가 바뀌면(로비 재진입 등) 그 컨트롤러의 바인딩 변경을 다시 구독한다.
        PlacementInputController.Current
            .Subscribe(controller => mBindingsChangedSubscription.Disposable = controller != null
                ? controller.BindingsChanged.Subscribe(_ => Refresh())
                : null)
            .AddTo(this);
    }

    private void Refresh()
    {
        // 종료/씬 정리 중에 컨트롤러가 파괴되며 Current=null을 알릴 때 이미 모델이나 이 오브젝트가 정리됐을 수 있다.
        if (this == null || GameInstance.Model == null || GameInstance.Model.Placement == null)
            return;

        var placement = GameInstance.Model.Placement;
        var controller = PlacementInputController.Current.Value;

        // 상점가 구매처럼 배치 모드 밖에서 들고 있기에 들어간 경우에도 조작법을 보여 준다.
        bool isVisible = (placement.IsEditMode.Value || placement.IsPlacing.Value) && controller != null;
        if (mRoot != null)
            mRoot.SetActive(isVisible);

        if (!isVisible || mTextGuide == null)
            return;

        mBuilder.Clear();

        if (placement.IsDemolishMode.Value)
        {
            mBuilder.Append("<color=#FF7A7A><b>철거 모드</b></color>\n");
            AppendLine(controller, PlacementInputController.ACTION_PLACE, "철거 (보관함으로 회수) · 바닥타일은 드래그 연속");
            AppendKeyLine($"{controller.GetKeyDisplayName(PlacementInputController.ACTION_DEMOLISH)} / {controller.GetKeyDisplayName(PlacementInputController.ACTION_CANCEL)}",
                "철거 모드 끄기");
        }
        else if (placement.IsBulkPlacing)
        {
            string placeKey = controller.GetKeyDisplayName(PlacementInputController.ACTION_PLACE);
            AppendLine(controller, PlacementInputController.ACTION_PLACE, "배치 · 드래그 연속 설치");
            AppendKeyLine($"{controller.GetKeyDisplayName(PlacementInputController.ACTION_FILL_MODIFIER)} + {placeKey}", "드래그 영역 채우기");
            if (placement.CanRotatePlacing)
                AppendLine(controller, PlacementInputController.ACTION_ROTATE, "회전");
            AppendLine(controller, PlacementInputController.ACTION_CANCEL, "그만 놓기");
        }
        else if (placement.IsPlacing.Value)
        {
            AppendLine(controller, PlacementInputController.ACTION_PLACE, "배치");
            if (placement.CanRotatePlacing)
                AppendLine(controller, PlacementInputController.ACTION_ROTATE,
                    placement.PlacingOrientMode == CTable.eFurnitureOrientMode.FlipXOnly ? "좌우 반전" : "회전");
            AppendLine(controller, PlacementInputController.ACTION_CANCEL, "취소");
        }
        else
        {
            AppendLine(controller, PlacementInputController.ACTION_PLACE, "가구 집기");
            AppendLine(controller, PlacementInputController.ACTION_DEMOLISH, "철거 모드");
            AppendLine(controller, PlacementInputController.ACTION_TOGGLE_GRID, "그리드 표시");
        }

        mTextGuide.text = mBuilder.ToString().TrimEnd();
    }

    private void AppendLine(PlacementInputController controller, string actionName, string description)
    {
        AppendKeyLine(controller.GetKeyDisplayName(actionName), description);
    }

    private void AppendKeyLine(string key, string description)
    {
        if (string.IsNullOrEmpty(key))
            return;

        mBuilder.Append("<b>").Append(key).Append("</b>  ").Append(description).Append('\n');
    }
}
