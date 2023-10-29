using System.Collections;
using Cysharp.Threading.Tasks;
using Framework.UI;

namespace Game.Scene
{
    public class Ingame : SceneBase
    {
        public override eScene ID => eScene.Ingame;

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
            GameInstance.UIMgr.Open<UIRootLobby, UIEmptyParam>(eUIType.UIRootLobby, new UIEmptyParam());

            GameInstance.UIMgr.Open<UIHudController, UIEmptyParam>(eUIType.UIHudController, new UIEmptyParam());
        }

        public override async UniTask Preprocessing()
        {
            bool isComplete = true;

            await UniTask.WaitUntil(() => isComplete);
        }
    }
}