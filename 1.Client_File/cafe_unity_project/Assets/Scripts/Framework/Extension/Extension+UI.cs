using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UniRx;

namespace Framework.Extension
{
    public static class ExtensionUI
    {
        public static IDisposable OnSubscribeOnClick(this Button button, Action<UniRx.Unit> onClickAction)
        {
            return button.OnClickAsObservable().Subscribe(action =>
            {
                onClickAction?.Invoke(action);
            });
        }
    }
}
