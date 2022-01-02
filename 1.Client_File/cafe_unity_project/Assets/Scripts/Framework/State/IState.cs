


using UnityEngine;

namespace Framework.State
{
    public interface IState
    {
        string Key { get; }

        /// <summary>
        /// 상태가 전환 가능한 상태인가?
        /// </summary>
        /// <returns></returns>
        bool IsTransition();

        /// <summary>
        /// 상태가 종료되었는가?
        /// </summary>
        /// <returns></returns>
        bool IsStateEnd { get; set; }

        /// <summary>
        /// 상태 실행 중인가?
        /// </summary>
        /// <returns></returns>
        bool IsStateExecute();

        void OperateEnter();
        void OperateOnUpdate();
        void OperateExit();
    }

}

