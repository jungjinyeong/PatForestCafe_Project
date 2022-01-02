using System.Collections.Generic;

namespace Framework.Event
{
    /// <summary>
    /// 리스너를 통하여 메세지를 보내는 클래스
    /// </summary>
    public class Dispatcher
    {
        protected static Dictionary<string, IListener> listeners;

        public static void Initialize()
        {
            listeners = new Dictionary<string, IListener>();
        }

        public static void Terminate()
        {
            listeners.Clear();
            listeners = null;
        }

        /// <summary>
        /// 메세지를 보냅니다.
        /// </summary>
        /// <param name="message"></param>
        public static void Dispatch(IMessage message)
        {
            if(listeners.ContainsKey(message.Receiver))
            {
                listeners[message.Receiver].Receive(message);
            }
            Pool.Push(message);
        }

        /// <summary>
        /// 리스너를 등록합니다.
        /// </summary>
        /// <param name="listener"></param>
        public static void RegistListener(IListener listener)
        {
            listeners.Add(listener.Key, listener);
        }

        public static void RemoveRegistListener(string key)
        {
            listeners.Remove(key);
        }
    }
}