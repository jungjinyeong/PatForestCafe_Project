using UniRx;
using UnityEngine;
using Extension;

public class UIRootLobby : UIWndBase
{
    public struct Param { }

    public override eUIType GetUIType() => eUIType.UIRootLobby;

    public override void Init()
    {
        // UIRootLobby는 최상위 씬 UI이므로 닫기 버튼 없이 직접 초기화
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Destroy()
    {
    }
}
