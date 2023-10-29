﻿using System.Collections;
using Cysharp.Threading.Tasks;
using Framework.UI;

namespace Game.Scene
{
    public class Intro : SceneBase
    {
        public override eScene ID => eScene.Intro;

        public override void OnEnter()
        {

        }

        public override void OnExecute()
        {

        }

        public override void OnExit()
        {

        }

        public override void Prepare()
        {
            GameInstance.UIMgr.Open<UIRootIntro, UIEmptyParam>(eUIType.UIRootIntro, new UIEmptyParam());
        }

        public override async UniTask Preprocessing()
        {
            bool isComplete = true;

            await UniTask.WaitUntil(() => isComplete);
        }
    }
}