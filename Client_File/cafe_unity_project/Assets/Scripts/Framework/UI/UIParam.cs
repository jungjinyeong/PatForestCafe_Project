using System;

namespace Framework.UI
{
    public struct Param
    {
    }

    public struct UIEmptyParam : IUIParam<Param>
    {
        public void Set(Param param)
        {
        }
    }

    public interface IUIParam<T> where T : struct
    {
        public abstract void Set(T param);
    }
}
