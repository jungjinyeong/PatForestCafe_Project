
namespace Framework.Page
{
    public interface IManager
    {
        /// <summary>
        /// 초기화하다.
        /// </summary>
        void Initialize();

        /// <summary>
        /// 종료되다.
        /// </summary>
        void Terminate();

        /// <summary>
        /// 준비하다.
        /// </summary>
        void Prepare();

        void OnUpdate();

        /// <summary>
        /// 페이지를 추가합니다.
        /// </summary>
        /// <param name="page"></param>
        void Add(IPage page);

        /// <summary>
        /// 페이지를 삭제합니다.
        /// </summary>
        /// <param name="page"></param>
        void Remove(IPage page);

        /// <summary>
        /// 페이지를 교체합니다.
        /// </summary>
        /// <param name="ID"></param>
        void Change(int ID);

        /// <summary>
        /// 페이지를 찾습니다.
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        IPage Find(int ID);
    }
}