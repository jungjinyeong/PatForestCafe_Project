


using DG.Tweening;

namespace Framework.State
{
    public interface IUIState : IState
    {
        Sequence ShowSequence { get; }
        Sequence HideSequence { get; }
    }
}