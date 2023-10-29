

namespace Framework.Event
{
    public interface IListener : IHasKey
    {
        /// <summary>
        /// 리스너를 등록합니다.
        /// </summary>
        /// <param name="Ilistener"></param>
        void RegistListener();

        /// <summary>
        /// 메세지를 받습니다.
        /// </summary>
        /// <param name="Message"></param>
        void Receive(IMessage Message);
    }
}