using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IManager
{
    private int characterLayerMask;

    public void Init()
    {
        characterLayerMask = LayerMask.GetMask("Character");
    }

    private void Update()
    {
        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        var col = Physics2D.OverlapPoint(worldPos, characterLayerMask);
        if (col is BoxCollider2D boxCollider2D)
        {
            // TODO : 수정 예정
            GameInstance.UI.Open<UIPopupOrderDetail, UIPopupOrderDetail.Param>(eUIType.PopupOrderDetail,
                new UIPopupOrderDetail.Param() { npc = boxCollider2D });
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
