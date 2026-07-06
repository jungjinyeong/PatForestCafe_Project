using System;
using UnityEngine;

/// <summary>
/// 스크롤 아이템의 공통 베이스. 구체적인 아이템은 UIScrollRow<T>를 상속받아 구현한다.
/// </summary>
public abstract class UIScrollRow : UIBase
{
    public int Index { get; private set; }

    private Action<UIScrollRow> _onSelectAction;

    internal void Setup(int index, Action<UIScrollRow> onSelectAction)
    {
        Index = index;
        _onSelectAction = onSelectAction;
    }

    /// <summary>
    /// 행이 선택됐을 때 자식 클래스에서 호출한다.
    /// </summary>
    protected void Select() => _onSelectAction?.Invoke(this);

    public abstract void SetData(object data);
}

/// <summary>
/// 타입 안전한 스크롤 아이템 베이스.
/// 사용 예) public class MyRow : UIScrollRow<MyData> { protected override void OnSetData(MyData data) { ... } }
/// </summary>
public abstract class UIScrollRow<T> : UIScrollRow
{
    public sealed override void SetData(object data) => OnSetData((T)data);

    protected abstract void OnSetData(T data);
}
