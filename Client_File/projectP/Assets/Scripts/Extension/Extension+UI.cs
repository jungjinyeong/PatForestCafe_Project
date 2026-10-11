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
        // SpriteAtlas.GetSprite는 호출할 때마다 Sprite 사본을 새로 만든다 — (아틀라스, 이름)별로 한 번만 꺼내 재사용해 사본이 쌓이지 않게 한다.
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> sAtlasSpriteCache = new();

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

            Sprite sprite = GetAtlasSprite(atlas, icon);
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        // 테이블의 Atlas(스프라이트 아틀라스 주소) + 스프라이트 이름 조합으로 스프라이트를 찾는다. CharNpc와 동일한 조회 방식.
        // 아틀라스는 주소당 1회만 로드해 캐싱한다(실패도 null로 캐싱 — 없는 주소에 매 갱신마다 에러 로그/참조 카운트가 쌓이는 것 방지).
        public static Sprite GetAtlasSprite(string atlas, string spriteName)
        {
            if (string.IsNullOrEmpty(atlas) || string.IsNullOrEmpty(spriteName))
                return null;

            // (object)!=null && ==null : 캐싱된 아틀라스가 파괴된 경우(Enter Play Mode 도메인 리로드 생략 등) 다시 로드.
            if (!sAtlasCache.TryGetValue(atlas, out var spriteAtlas) || ((object)spriteAtlas != null && spriteAtlas == null))
            {
                spriteAtlas = GameInstance.Resource.LoadSync<UnityEngine.U2D.SpriteAtlas>(atlas);
                sAtlasCache[atlas] = spriteAtlas;
            }
            if (spriteAtlas == null)
                return null;

            string key = atlas + "/" + spriteName;
            // (object)!=null && ==null : 캐싱된 사본이 파괴된 경우 다시 꺼낸다. 없는 이름(null)도 캐싱해 매번 조회하지 않는다.
            if (sAtlasSpriteCache.TryGetValue(key, out var sprite) && !((object)sprite != null && sprite == null))
                return sprite;

            sprite = spriteAtlas.GetSprite(spriteName);
            sAtlasSpriteCache[key] = sprite;
            return sprite;
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
