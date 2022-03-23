

using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [RequireComponent(typeof(ClickAudioPlayer))]
    public class CommonButton : Widget
    {
        protected UnityEngine.UI.Button button;
        protected OnClick onClick;

        protected override void Awake()
        {
            Init();
        }

        public override void Prepare()
        {
        }

        public override void Appear()
        {

        }

        public override void Disappear()
        {

        }

        protected virtual void Init()
        {
            button = gameObject.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(OnClickEvent);
        }

        public void SetOnClickEvent(OnClick evt)
        {
            onClick = evt;
        }

        public void OnClickEvent()
        {
            onClick?.Invoke();
        }
    }
}