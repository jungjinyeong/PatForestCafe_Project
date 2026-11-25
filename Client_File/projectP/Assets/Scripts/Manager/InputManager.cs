using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IManager
{
    private int mInteractableLayerMask;

    public void Init()
    {
        // CharNpc는 "Character" 레이어, Intaraction_DessertStand는 가구(PlaceableObject와 같은 GameObject에 붙는
        // 월드 오브젝트)라 "Furniture" 레이어에 있다. 둘 다 감지하려면 마스크에 둘 다 포함해야 한다
        // (PlaceableObject.OnPointerDown은 배치 모드 전용이라 레이어 제한 없이 OverlapPoint를 쓴다 — 그건 그대로 둬도 됨).
        mInteractableLayerMask = LayerMask.GetMask("Character", "Furniture");
    }

    private void Update()
    {
        if(Camera.main == null) return;

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        var col = Physics2D.OverlapPoint(worldPos, mInteractableLayerMask);
        if (col is BoxCollider2D boxCollider2D)
        {
            // 손님 클릭: 지금은 키오스크 이벤트(말풍선) 중인 손님만 반응한다(CharNpc.OnClicked).
            var npc = boxCollider2D.GetComponentInParent<CharNpc>();

            if (npc != null)
            {
                // UI(HUD 버튼·팝업)를 누른 탭이 뒤의 손님까지 누르지 않게 한다.
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (!isOverUI && !GameInstance.Model.Placement.IsEditMode.Value)
                    npc.OnClicked();
                return;
            }

            var dessertStand = boxCollider2D.GetComponentInParent<Intaraction_DessertStand>();
            if (dessertStand != null)
            {
                // 가구 배치 모드 중에는 같은 콜라이더를 PlaceableObject가 드래그 시작 판정에 쓰므로,
                // 여기서 팝업까지 열면 탭 하나에 두 입력이 동시에 반응하게 된다 — 배치 모드일 때는 건너뛴다.
                if (GameInstance.Model.Placement.IsEditMode.Value)
                    return;

                GameInstance.UI.Open<UIPopupDessertSelect, UIPopupDessertSelect.Param>(eUIType.PopupDessertSelect,
                    new UIPopupDessertSelect.Param() { dessertStand = dessertStand });
            }
        }
    }

    public void Clear()
    {
    }

    public void Subscribe()
    {
    }

    public void Destory()
    {
    }
}
