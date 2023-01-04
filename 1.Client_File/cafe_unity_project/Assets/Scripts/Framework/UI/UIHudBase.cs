using System;
using Game.Event;

namespace Framework.UI
{
    public abstract class UIHudBase : UIBase
    {
        public abstract void OnEvent(IUIEvent evt);
    }
}
