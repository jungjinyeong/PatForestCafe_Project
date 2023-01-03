using System;
using Game.Event;
using Framework.UI;

public abstract class UIHudBase : UIBase
{
    public virtual void Init() { }

    public abstract void OnEvent(IUIEvent evt);
}


[Serializable]
public class UIHudDictionary : SerializableDictionary<eUIHudType, UIHudBase> { }