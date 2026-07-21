using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, IManager
{
    private int mCharacterLayerMask;

    public void Init()
    {
        mCharacterLayerMask = LayerMask.GetMask("Character");
    }

    private void Update()
    {
        if(Camera.main == null) return;

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        var col = Physics2D.OverlapPoint(worldPos, mCharacterLayerMask);
        if (col is BoxCollider2D boxCollider2D)
        {
            //TODO: 이 부분은 나중에 NPC와 상호작용하는 로직으로 변경 필요
            var npc = boxCollider2D.GetComponentInParent<CharLobbyPathMover>();

            if (npc != null && npc.IsWaitingSpecialOrder)
            {
                GameInstance.UI.Open<UIPopupOrderDetail, UIPopupOrderDetail.Param>(eUIType.PopupOrderDetail,
                    new UIPopupOrderDetail.Param() { npc = boxCollider2D });
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
