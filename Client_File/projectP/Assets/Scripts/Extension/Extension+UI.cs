using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Extension
{
    public static class ExtensionUI
    {
        public static IDisposable OnSubscribeOnClick(this Button button, Action onClickAction)
        {
            return button.OnClickAsObservable().Subscribe(action =>
            {
                onClickAction?.Invoke();
            });
        }

        public static void SetTextEx(this TextMeshProUGUI textMeshProUGUI, string text)
        {
            if (textMeshProUGUI == null)
                return;

            textMeshProUGUI.text = text;
        }

        public static void SetSpriteEx(this Image image, string atlas, string icon)
        {
            if (null == image)
                return;

            // TODO load

            image.sprite = null;
        }

        public static void SetTextureEx(this RawImage rowImg, string texture)
        {
            if(null == rowImg)
                return;

            var result = GameInstance.Resource.LoadSync<Texture>(texture);
            if (result == null)
                return;

            rowImg.texture = result;
        }
    }
}
