
using UnityEngine;

namespace Framework.UI
{
    public interface IWidget
    {
        bool IsPrepared { get; }
        void SetActivated(bool activate);
        void Prepare();
        void Appear();
        void Disappear();

        void SetAppearCompletedCallback(OnCompletedAppearFuntion callback);

        /// <summary>
        /// 발동하다
        /// </summary>
        void InvokeCompletedAppearFuntion();
    }
}
