using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extension
{
    public static class ExtensionGameObject
    {
        public static void SetActive(this Component component, bool active)
        {
            if (null == component) return;
            if (null == component.gameObject) return;

            component.gameObject.SetActive(active);
        }

        public static void SetActiveEx(this GameObject gameObject, bool active) 
        {
            if(null == gameObject) return;

            gameObject.SetActive(active);
        }
    }
}
