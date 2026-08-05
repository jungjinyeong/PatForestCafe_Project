using UnityEngine;
using TMPro;
using Extension;

public class UIScrollRecipeBookData
{
    public int Tid;
    public string Name;
    public bool Discovered;
}

public class UIScrollRecipeBook : UIScrollRow<UIScrollRecipeBookData>
{
    private const string UndiscoveredName = "???";

    [SerializeField] private TextMeshProUGUI mName;

    protected override void OnSetData(UIScrollRecipeBookData data)
    {
        if (data == null) return;

        mName.SetTextEx(data.Discovered ? data.Name : UndiscoveredName);
    }
}
