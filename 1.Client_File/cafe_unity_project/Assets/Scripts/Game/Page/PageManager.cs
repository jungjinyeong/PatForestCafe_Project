
using System.Collections.Generic;

public class PageManager : Framework.Page.Manager
{
	public override void Prepare()
	{
		using (Dictionary<int, Framework.Page.IPage>.Enumerator e = pages.GetEnumerator())
		{
			while (e.MoveNext())
			{
				Framework.Page.IPage page = e.Current.Value;
				int key = e.Current.Key;
			}
		}

		List<int> keys = new List<int>(pages.Keys);
		for (int i = 0, ii = keys.Count; ii > i; ++i)
		{
			pages[keys[i]].Prepare();
		}
	}

	public override void OnUpdate()
	{
		if (activatedPage == null)
		{
			return;
		}
		activatedPage.OnExecute();
	}
}