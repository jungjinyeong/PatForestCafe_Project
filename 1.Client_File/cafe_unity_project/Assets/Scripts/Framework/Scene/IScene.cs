using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Framework.Scene
{
    public interface IScene
    {
        void Prepare();

        UniTask Preprocessing();

        /// <summary>
        /// Scene에 진입했습니다.
        /// </summary>
        void OnEnter();

        void OnExecute();

        /// <summary>
        /// Scene에 나갔습니다.
        /// </summary>
        void OnExit();
    }
}
