using UnityEngine;
using UnityEditor;


namespace Framework.UI
{
    public class ClickAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip mAudioClip = null;

        public void OnPlayAudio()
        {
            if(mAudioClip != null)
            {
                GameInstance.Sound.PlayEffect(mAudioClip);
            }
        }
    }
}