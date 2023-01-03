using System;

namespace Framework.UI
{
    public struct Param
    {
    }

    public class UIEmptyParam : UIParam<Param>
    {
        public void Set(Param param)
        {
        }
    }

    public interface UIParam<T> where T : struct
    {
        public abstract void Set(T param);
    }
}
