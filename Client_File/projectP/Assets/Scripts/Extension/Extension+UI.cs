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
        private static readonly System.Collections.Generic.Dictionary<string, UnityEngine.U2D.SpriteAtlas> sAtlasCache = new();

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

            // 테이블의 Atlas(스프라이트 아틀라스 주소) + Icon(아틀라스 내 스프라이트 이름) 조합. CharNpc와 동일한 조회 방식.
            // 아틀라스는 주소당 1회만 로드해 캐싱한다(실패도 null로 캐싱 — 없는 주소에 매 갱신마다 에러 로그/참조 카운트가 쌓이는 것 방지).
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(atlas) && !string.IsNullOrEmpty(icon))
            {
                // (object)!=null && ==null : 캐싱된 아틀라스가 파괴된 경우(Enter Play Mode 도메인 리로드 생략 등) 다시 로드.
                if (!sAtlasCache.TryGetValue(atlas, out var spriteAtlas) || ((object)spriteAtlas != null && spriteAtlas == null))
                {
                    spriteAtlas = GameInstance.Resource.LoadSync<UnityEngine.U2D.SpriteAtlas>(atlas);
                    sAtlasCache[atlas] = spriteAtlas;
                }
                sprite = spriteAtlas != null ? spriteAtlas.GetSprite(icon) : null;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
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
