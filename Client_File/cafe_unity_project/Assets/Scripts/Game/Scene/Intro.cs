using System.Collections;
using Cysharp.Threading.Tasks;

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

        }

        public override async UniTask Preprocessing()
        {
            bool isComplete = true;

            await UniTask.WaitUntil(() => isComplete);
        }
    }
}