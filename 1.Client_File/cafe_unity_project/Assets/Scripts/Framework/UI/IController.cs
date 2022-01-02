



namespace Framework.UI
{
    public interface IController : IWidget
    {
        /// <summary>
        /// 컨트롤러의 고유 ID
        /// </summary>
        int ID { get; }
    }
}