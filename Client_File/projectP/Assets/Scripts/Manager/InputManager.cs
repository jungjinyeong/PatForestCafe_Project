using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IManager
{
    private int mInteractableLayerMask;

    public void Init()
    {
        // CharNpc는 "Character" 레이어, Intaraction_BreadStand는 UI/RectTransform 기반이라 "UI" 레이어에 있다.
        // 둘 다 감지하려면 마스크에 둘 다 포함해야 한다(PlaceableObject.OnPointerDown도 같은 이유로 레이어 제한 없이 OverlapPoint를 쓴다).
        mInteractableLayerMask = LayerMask.GetMask("Character", "UI");
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
            //TODO: 이 부분은 나중에 NPC와 상호작용하는 로직으로 변경 필요
            var npc = boxCollider2D.GetComponentInParent<CharNpc>();

            if (npc != null)
            {
                if (npc.IsWaitingSpecialOrder)
                {
                    GameInstance.UI.Open<UIPopupOrderDetail, UIPopupOrderDetail.Param>(eUIType.PopupOrderDetail,
                        new UIPopupOrderDetail.Param() { npc = boxCollider2D });
                }
                return;
            }

            var breadStand = boxCollider2D.GetComponentInParent<Intaraction_BreadStand>();
            if (breadStand != null)
            {
                GameInstance.UI.Open<UIPopupBreadSelect, UIPopupBreadSelect.Param>(eUIType.UIPopupBreadSelect,
                    new UIPopupBreadSelect.Param() { breadStand = breadStand });
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
