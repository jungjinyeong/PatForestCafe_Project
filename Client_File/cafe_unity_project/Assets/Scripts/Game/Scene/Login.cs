using System.Collections;
using Cysharp.Threading.Tasks;
using Framework.UI;

namespace Game.Scene
{
    public class Login : SceneBase
    {
        public override eScene ID => eScene.Login;

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
            GameInstance.UIMgr.Open<UIRootLogin, UIEmptyParam>(eUIType.UIRootLogin, new UIEmptyParam());
        }

        public override async UniTask Preprocessing()
        {
            bool isComplete = true;

            await UniTask.WaitUntil(() => isComplete);
        }
    }
}