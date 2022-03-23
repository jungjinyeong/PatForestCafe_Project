
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class UIManager : Framework.UI.Manager
{
    /// <summary>
    /// 준비단계
    /// </summary>
    public override void Prepare()
	{
		Framework.UI.IWidget[] widgets = GameObject.FindWithTag("Canvas").GetComponentsInChildren<Framework.UI.IWidget>();

		for (int i = 0, ii = widgets.Length; ii > i; i++)
		{
			widgets[i].Prepare();
			widgets[i].Disappear();
		}
	}

	public T Get<T>(int id) where T : Framework.UI.IController
	{
		if (base.controllers.ContainsKey(id))
		{
			return (T)controllers[id];
		}
		return default(T);
	}

	/// <summary>
	/// UI 컨트롤러가 준비가 되었습니까?
	/// </summary>
	/// <returns></returns>
	public IEnumerator WaitForPrepared()
	{
		List<int> keys = new List<int>(base.controllers.Keys);

		while (true)
		{
			yield return null;

			bool prepared = true;

			for (int i = 0, ii = keys.Count; i < ii; ++i)
			{
				if (!controllers[keys[i]].IsPrepared)
				{
					prepared = false;
					break;
				}
			}

			if (prepared)
				break;
		}
	}
}