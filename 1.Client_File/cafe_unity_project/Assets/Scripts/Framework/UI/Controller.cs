

using UnityEngine;


namespace Framework.UI
{
    public abstract class Controller : MonoBehaviour, IController
    {
        protected OnCompletedAppearFuntion onCompletedAppearFuntion;
        protected OnCompletedDisappearFuntion onCompletedDisappearFuntion;
        /// <summary>
        /// 컨트롤러의 고유 ID
        /// </summary>
        public abstract int ID { get; }

        /// <summary>
        /// 준비가 다 되었습니까?
        /// </summary>
        public bool IsPrepared { get { return !gameObject.activeInHierarchy; } }
       
        protected virtual void Awake()
        {


        }
        public virtual void Appear()
        {

        }

        public virtual void Disappear()
        {

        }

        public virtual void InvokeCompletedAppearFuntion()
        {
            onCompletedAppearFuntion?.Invoke();
        }

        public virtual void InvokeCompletedDisappearFuntion()
        {
            onCompletedDisappearFuntion?.Invoke();
        }

        public virtual void Prepare()
        {

        }

        public void SetActivated(bool activate)
        {
            gameObject.SetActive(activate);
        }

        public virtual void SetAppearCompletedCallback(OnCompletedAppearFuntion callback)
        {
            onCompletedAppearFuntion = callback ;
        }
        public void SetDisappearCompletedCallback(OnCompletedDisappearFuntion callback)
        {
            onCompletedDisappearFuntion = callback;
        }
    }
}
