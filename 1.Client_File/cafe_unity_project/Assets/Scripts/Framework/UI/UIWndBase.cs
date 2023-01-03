using System;
using System.Collections.Generic;

namespace Framework.UI
{
    public abstract class UIWndBase : UIBase
    {
        protected UIManager UIMgr => GameInstance.Instance.UIMgr;

        public abstract eUIType GetUIType();

        public virtual void Init() { }

        public virtual void Destroy() { }

        public virtual void Open()
        {
            Active();
        }

        public virtual void Close() { }

        protected void SelfClose()
        {
            UIMgr.Close(GetUIType());
        }

    }
}
