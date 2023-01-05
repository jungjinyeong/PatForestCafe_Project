using System.Collections;
using Cysharp.Threading.Tasks;

namespace Game.Page
{
    public class Intro : Framework.Page.IPage
    {
        public int ID { get { return (int)Game.Page.ID.Intro; } }

        public void OnEnter()
        {

        }

        public void OnExecute()
        {

        }

        public void OnExit()
        {

        }

        public void Prepare()
        {

        }

        public async UniTask Preprocessing()
        {
            bool isComplete = true;

            await UniTask.WaitUntil(() => isComplete);
        }
    }
}