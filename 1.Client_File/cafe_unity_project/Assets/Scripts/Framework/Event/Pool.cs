using System;
using System.Collections.Generic;

namespace Framework.Event
{
    public class Pool
    {
        protected static Dictionary<System.Type, List<IMessage>> storage = new Dictionary<System.Type, List<IMessage>>();
        public static void Push(IMessage message)
        {
            storage[message.GetType()].Add(message);
        }

        public static T Pop<T> () where T : IMessage
        {
            T retValue = default(T);
            System.Type type = typeof(T);

            if(!storage.ContainsKey(type))
            {
                storage.Add(type, new List<IMessage>());
            }

            if(storage[type].Count > 0)
            {
                retValue = (T)storage[type][0];
                storage[type].RemoveAt(0);
            }
            else
            {
                retValue = (T)Activator.CreateInstance(type);
            }
            return retValue;
        }
    }
}