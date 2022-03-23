

using UnityEngine;

namespace Game.UI
{
    public class Intro : Framework.UI.Controller
    {
        public override int ID { get { return (int)Game.UI.ID.Intro; } }

        #region Inspactor
        [SerializeField] private Framework.UI.CommonButton _btnStart = null;
        #endregion

        public override void Prepare()
        {
            _btnStart.SetOnClickEvent(OnclickGameStart);
        }

        public override void Appear()
        {

        }

        public override void Disappear()
        {

        }

        #region onclick
        private void OnclickGameStart()
        {
            Debug.Log("onclick game start!!!");
        }
        #endregion
    }
}