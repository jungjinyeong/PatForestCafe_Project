using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Framework.Page
{
    public abstract class Manager : IManager
    {
        protected Dictionary<int, IPage> pages;

        /// <summary>
        /// 초기화하다.
        /// </summary>
        public void Initialize()
        {
            pages = new Dictionary<int, IPage>();
        }

        /// <summary>
        /// 종료되다.
        /// </summary>
        public void Terminate()
        {
            pages.Clear();
            pages = null;
        }

        /// <summary>
        /// 활성화된 페이지
        /// </summary>
        protected IPage activatedPage { private set; get; }

        /// <summary>
        /// 준비단계
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// 업데이트
        /// </summary>
        public abstract void OnUpdate();

        /// <summary>
        /// 페이지를 추가합니다.
        /// </summary>
        /// <param name="page"></param>
        public void Add(IPage page)
        {
            pages.Add(page.ID, page);
        }

        public IPage Find(int ID)
        {
            if (pages.ContainsKey(ID))
            {
                return pages[ID];
            }
            return null;
        }

        /// <summary>
        /// 페이지를 교체합니다.
        /// </summary>
        /// <param name="ID"></param>
        public void Change(int NextPageID)
        {
            Processing(NextPageID).Forget();
        }

        protected async UniTask Processing(int NextPageID)
        {
            if (activatedPage != null)
            {
                activatedPage.OnExit();
            }
            IPage nextPage = Find(NextPageID);

            if (nextPage != null)
            {
                activatedPage = nextPage;

                await activatedPage.Preprocessing();

                activatedPage.OnEnter();
            }
        }

        /// <summary>
        /// 페이지를 삭제합니다.
        /// </summary>
        /// <param name="page"></param>
        public void Remove(IPage page)
        {
            pages.Remove(page.ID);
        }

    }
}