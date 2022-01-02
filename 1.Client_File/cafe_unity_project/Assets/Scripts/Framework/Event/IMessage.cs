

namespace Framework.Event
{
    public interface IMessage
    {
        /// <summary>
        /// 메세지 고유아이디
        /// </summary>
        int ID { get; }
        
        string Sender { get; set; }

        string Receiver { get; set; }
    }
}