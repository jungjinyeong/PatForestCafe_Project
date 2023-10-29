using Cysharp.Threading.Tasks;
using Framework.Scene;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scene
{
    public abstract class SceneBase : MonoBehaviour, IScene
    {
        protected virtual void Awake()
        {
            var contain = GameObject.Find("GameInstance");
            if(null == contain)
            {
                GameObject parent = new GameObject("GameInstance");
                parent.AddComponent<GameInstance>();

                Debug.Log("Create GameInstance");
            }
        }

        public abstract eScene ID { get; }

        public abstract void OnEnter();

        public abstract void OnExecute();

        public abstract void OnExit();

        public abstract void Prepare();

        public abstract UniTask Preprocessing();
    }
}
