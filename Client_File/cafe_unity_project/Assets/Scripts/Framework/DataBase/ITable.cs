
namespace Framework.DataBase
{
    public interface ITable : IHasKey
    {
        /// <summary>
        /// 파일 로드
        /// </summary>
        void LoadFromFile();

        /// <summary>
        /// RowData 반환
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        T Get<T>(string key) where T : IRowData;
    }
}