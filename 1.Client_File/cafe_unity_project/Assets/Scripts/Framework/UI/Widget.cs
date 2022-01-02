


using UnityEngine;
using DG.Tweening;

namespace Framework.UI
{
    public class Widget : MonoBehaviour, IWidget
    {

        public virtual Sequence ShowSequence { get; }
        public virtual Sequence HideSequence { get; }

        public bool IsPrepared { get { return true; } }
        public void SetActivated(bool activate)
        {
            gameObject.SetActive(activate);
        }

        protected virtual void Awake()
        {

        }

        public virtual void Appear()
        {

        }

        public virtual  void Disappear()
        {

        }

        public virtual  void InvokeCompletedAppearFuntion()
        {

        }

        public virtual void Prepare()
        {

        }

        public void SetAppearCompletedCallback(OnCompletedAppearFuntion callback)
        {

        }


    }
}
