using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.UI
{
    public abstract class UIBase : MonoBehaviour
    {
        protected UIManager UIMgr => GameInstance.Instance.UIMgr;

        public abstract eUIType GetUIType();

        public virtual void Init() { }

        public virtual void Destroy() { }

        public virtual void Open() 
        {
            gameObject.SetActive(true);
        }

        public virtual void Close() { }

        public virtual void SelfClose()
        {
            UIMgr.Close(GetUIType());
        }
    }
}