

namespace Framework.Audio
{
    public enum AudioType
    {
        _BEGIN = 0,
        Effect,
        BGM,
        ButtonEffect,
    }

    public interface IAudio
    {
        AudioType audioType { get; set; }

        void PlayAudio();
    }
}
