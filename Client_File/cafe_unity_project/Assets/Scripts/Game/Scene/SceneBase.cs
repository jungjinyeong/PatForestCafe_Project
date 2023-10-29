using Cysharp.Threading.Tasks;
using Framework.Scene;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scene
{
    public abstract class SceneBase : MonoBehaviour, IScene
    {
        public abstract eScene ID { get; }

        public abstract void OnEnter();

        public abstract void OnExecute();

        public abstract void OnExit();

        public abstract void Prepare();

        public abstract UniTask Preprocessing();
    }
}
