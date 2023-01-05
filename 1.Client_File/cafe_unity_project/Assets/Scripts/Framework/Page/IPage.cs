using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Framework.Page
{
    public interface IPage
    { 
        /// <summary>
        /// 페이지 고유 ID
        /// </summary>
        int ID { get; }

        void Prepare();

        UniTask Preprocessing();

        /// <summary>
        /// 페이지에 진입했습니다.
        /// </summary>
        void OnEnter();

        void OnExecute();

        /// <summary>
        /// 페이지에 나갔습니다.
        /// </summary>
        void OnExit();
    }
}
