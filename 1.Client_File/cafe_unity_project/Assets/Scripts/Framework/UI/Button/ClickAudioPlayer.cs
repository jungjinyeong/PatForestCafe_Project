using UnityEngine;
using UnityEditor;


namespace Framework.UI
{
    public class ClickAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip _audioClip = null;

        public void OnPlayAudio()
        {
            if(_audioClip != null)
            {
                GameInstance.Sound.PlayEffect(_audioClip);
            }
        }
    }
}