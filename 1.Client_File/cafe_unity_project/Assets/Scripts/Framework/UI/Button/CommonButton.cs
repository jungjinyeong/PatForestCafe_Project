

using UnityEngine;
using UnityEngine.EventSystems;

namespace Framework.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [RequireComponent(typeof(ClickAudioPlayer))]
    public class CommonButton : Widget
    {

        [SerializeField] AudioClip ClickAudio;

        protected UnityEngine.UI.Button button;
        protected OnClick onClick;

        protected override void Awake()
        {
            button = gameObject.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(OnClickEvent);
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

        public void SetOnClickEvent(OnClick evt)
        {
            onClick = evt;
        }

        public void OnClickEvent()
        {
            if (ClickAudio != null)
            {                
            }

            onClick();
        }
    }
}