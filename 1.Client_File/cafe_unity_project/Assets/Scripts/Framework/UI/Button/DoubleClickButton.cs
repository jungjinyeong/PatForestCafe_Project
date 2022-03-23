

using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;
using UniRx.Triggers;
using System;

namespace Framework.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [RequireComponent(typeof(ClickAudioPlayer))]
    public class DoubleClickButton : CommonButton
    {
        protected override void Init()
        {
            button = gameObject.GetComponent<UnityEngine.UI.Button>();
            var clickStream = this.UpdateAsObservable()
                                            .Where(_ => Input.GetMouseButtonDown(0));

            clickStream.Buffer(clickStream.Throttle(TimeSpan.FromMilliseconds(300)))
                .Where(x => x.Count >= 2)
                .Subscribe(_=> OnClickEvent());
        }
    }
}