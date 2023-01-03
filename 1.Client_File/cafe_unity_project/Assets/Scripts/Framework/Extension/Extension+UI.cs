using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UniRx;

namespace Framework.Extension
{
    public static class ExtensionUI
    {
        public static IDisposable OnSubscribeOnClick(this Button button, UnityAction onClickAction)
        {
            button.onClick.AddListener(onClickAction);
            return button.OnClickAsObservable().Subscribe();
        }
    }
}
