using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

// 로비 포스트프로세싱 낮/밤 연동.
// 낮 Volume(Global, weight 1 고정)이 기본 색감·비네트·블룸을 깔고, 밤 Volume(Global, 더 높은 priority)의 weight를
// DayNightManager.NightAlpha(0=낮, 1=밤)로 맞춰 밤 프로필 값으로 섞는다. 효과 수치는 각 Volume 프로필에서 조절한다.
// UI 캔버스는 Screen Space - Overlay라 포스트프로세싱 영향을 받지 않는다.
// GameInstance.DayNight 의존이므로 GameModeLobby+FSM.OnEnterInit에서 Init()을 부른다.
public class LobbyPostProcessController : MonoBehaviour
{
    [SerializeField] private Volume mNightVolume;

    public void Init()
    {
        if (mNightVolume == null || GameInstance.DayNight == null)
            return;

        GameInstance.DayNight.NightAlpha
            .Subscribe(alpha => mNightVolume.weight = Mathf.Clamp01(alpha))
            .AddTo(this);
    }
}
