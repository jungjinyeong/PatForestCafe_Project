
using UnityEngine;
using System.Collections.Generic;

namespace Framework.UI
{
    public abstract class Manager : IManager
    {
        protected List<IWidget> widgets;
        protected Dictionary<int, IController> controllers;

        public void Initialize()
        {
            widgets = new List<IWidget>();
            controllers = new Dictionary<int, IController>();

            Framework.UI.IWidget[] tmp = GameObject.Find("Canvas").GetComponentsInChildren<Framework.UI.IWidget>();

            for (int i = 0, ii = tmp.Length; ii > i; ++i)
            {
                if (tmp[i] is Framework.UI.IController)
                {
                    Framework.UI.IController ctr = tmp[i] as Framework.UI.IController;
                    controllers.Add(ctr.ID, ctr);
                }
                Add(tmp[i]);
            }
        }

        public abstract void Prepare();

        public void Terminate()
        {
            widgets.Clear();
            widgets = null;

            controllers.Clear();
            controllers = null;
        }

        protected virtual void Add(Framework.UI.IWidget widget)
        {
            widgets.Add(widget);
        }
    }
}
