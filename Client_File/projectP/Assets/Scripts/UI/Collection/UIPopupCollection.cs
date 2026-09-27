using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

// 도감(프리팹 UI_Popup_Collection, 로비 하단 [도감]). ForestCafe UI Lab 레시피 북 시안 기준 책 형태.
// 왼쪽 페이지: 분류 탭(전체/음료/디저트) + 레시피 목록(번호·썸네일·분류·이름·발견 상태).
// 오른쪽 페이지: 선택한 레시피 상세(번호·이름·분류 도장·그림·오늘의 기록·재료 목록).
// 발견 여부는 RecipeBookModel(음료 = 레시피 연구소 개발, 디저트 = 기본 발견 + 상점가 레시피 구매)을 그대로 쓴다.
public class UIPopupCollection : UIWndBase, IUIParam<UIPopupCollection.Param>
{
    public struct Param
    {
    }

    private enum eTab
    {
        All,
        Drink,
        Dessert,
    }

    private static readonly string[] TAB_NAMES = { "전체", "음료", "디저트" };
    private static readonly string[] CHAPTER_NAMES = { "모든 레시피", "음료 레시피", "디저트 레시피" };

    [Header("Tabs")]
    [SerializeField] private UIButtonEx[] mTabButtons;
    [SerializeField] private TextMeshProUGUI[] mTabTexts;
    [SerializeField] private GameObject[] mTabSelectedMarks;
    [SerializeField] private TextMeshProUGUI mTextChapter;
    [SerializeField] private TextMeshProUGUI mTextChapterCount;

    [Header("List")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mRowPrefab;

    [Header("Detail")]
    [SerializeField] private TextMeshProUGUI mTextBookmark;
    [SerializeField] private TextMeshProUGUI mTextRecipeNumber;
    [SerializeField] private TextMeshProUGUI mTextRecipeName;
    [SerializeField] private TextMeshProUGUI mTextCategoryStamp;
    [SerializeField] private Image mImageArt;
    [SerializeField] private TextMeshProUGUI mTextArtPlaceholder;
    [SerializeField] private TextMeshProUGUI mTextNote;
    [SerializeField] private GameObject[] mIngredientRoots;
    [SerializeField] private TextMeshProUGUI[] mIngredientNames;
    [SerializeField] private TextMeshProUGUI[] mIngredientOwned;

    private readonly List<UIScrollCollectionData> mAllEntries = new List<UIScrollCollectionData>();
    private eTab mTab = eTab.All;
    private int mSelectedTid;

    public override eUIType GetUIType() => eUIType.PopupCollection;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mRowPrefab);
        mScrollEx.SetOnSelect(OnSelectRow);

        for (int i = 0; i < mTabButtons.Length; i++)
        {
            var tab = (eTab)i;
            mTabButtons[i].OnSubscribeOnClick(() => SelectTab(tab)).AddTo(this);
        }
    }

    public override void Open()
    {
        base.Open();

        BuildEntries();
        mSelectedTid = 0;
        SelectTab(eTab.All);
    }

    public void Set(Param param)
    {
    }

    public static string GetCategoryName(bool isDrink) => isDrink ? "음료" : "디저트";

    private void OnSelectRow(UIScrollRow row)
    {
        if (row is not UIScrollCollection collectionRow || collectionRow.CurrentData == null)
            return;

        SelectEntry(collectionRow.CurrentData);
    }

    // 음료(Tid 순) → 디저트(Tid 순)로 도감 번호를 매긴다.
    private void BuildEntries()
    {
        mAllEntries.Clear();

        var drinkTable = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (drinkTable != null)
        {
            foreach (var row in drinkTable.All.Values.OrderBy(r => r.Tid))
                mAllEntries.Add(new UIScrollCollectionData { Tid = row.Tid, IsDrink = true });
        }

        var breadTable = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (breadTable != null)
        {
            foreach (var row in breadTable.All.Values.OrderBy(r => r.Tid))
                mAllEntries.Add(new UIScrollCollectionData { Tid = row.Tid, IsDrink = false });
        }

        for (int i = 0; i < mAllEntries.Count; i++)
        {
            mAllEntries[i].Number = i + 1;
            mAllEntries[i].Discovered = GameInstance.Model.RecipeBook.IsDiscovered(mAllEntries[i].Tid);
        }
    }

    private IEnumerable<UIScrollCollectionData> GetEntries(eTab tab)
    {
        switch (tab)
        {
            case eTab.Drink: return mAllEntries.Where(e => e.IsDrink);
            case eTab.Dessert: return mAllEntries.Where(e => !e.IsDrink);
            default: return mAllEntries;
        }
    }

    private void SelectTab(eTab tab)
    {
        mTab = tab;

        for (int i = 0; i < mTabButtons.Length; i++)
        {
            var entries = GetEntries((eTab)i).ToList();
            if (i < mTabTexts.Length)
                mTabTexts[i].SetTextEx($"{TAB_NAMES[i]} <size=80%>{entries.Count(e => e.Discovered)}/{entries.Count}</size>");
            if (i < mTabSelectedMarks.Length)
                mTabSelectedMarks[i].SetActive(i == (int)tab);
        }

        var list = GetEntries(tab).ToList();
        mTextChapter.SetTextEx(CHAPTER_NAMES[(int)tab]);
        mTextChapterCount.SetTextEx($"발견 {list.Count(e => e.Discovered)} / {list.Count}");

        mScrollEx.SetData(list);
        mScrollEx.ScrollToTop();

        // 선택 중인 레시피가 이 탭에 없으면 첫 번째 발견 레시피(없으면 첫 항목)를 펼친다.
        var selected = list.FirstOrDefault(e => e.Tid == mSelectedTid)
            ?? list.FirstOrDefault(e => e.Discovered)
            ?? list.FirstOrDefault();
        SelectEntry(selected);
    }

    private void SelectEntry(UIScrollCollectionData entry)
    {
        mSelectedTid = entry?.Tid ?? 0;

        for (int i = 0; i < mScrollEx.ActiveCount; i++)
        {
            if (mScrollEx.GetRow(i) is UIScrollCollection row)
                row.SetSelected(row.CurrentData != null && row.CurrentData.Tid == mSelectedTid);
        }

        RefreshDetail(entry);
    }

    private void RefreshDetail(UIScrollCollectionData entry)
    {
        if (entry == null)
        {
            mTextBookmark.SetTextEx(string.Empty);
            mTextRecipeNumber.SetTextEx(string.Empty);
            mTextRecipeName.SetTextEx("레시피 없음");
            mTextCategoryStamp.SetTextEx(string.Empty);
            mImageArt.enabled = false;
            mTextArtPlaceholder.SetTextEx(string.Empty);
            mTextNote.SetTextEx(string.Empty);
            SetIngredients(null, false);
            return;
        }

        var menuRow = GameInstance.Table.Get<CTable.MenuItemRow>(entry.Tid);

        mTextBookmark.SetTextEx(entry.Discovered ? "발견" : "미발견");
        mTextRecipeNumber.SetTextEx($"RECIPE {entry.Number:00}");
        mTextRecipeName.SetTextEx(entry.Discovered ? menuRow?.Name ?? entry.Tid.ToString() : "???");
        mTextCategoryStamp.SetTextEx(GetStampText(entry));

        if (entry.Discovered && menuRow != null)
            mImageArt.SetSpriteEx(menuRow.Atlas, menuRow.Icon);
        else
            mImageArt.enabled = false;
        mTextArtPlaceholder.SetTextEx(mImageArt.enabled ? string.Empty : "?");

        mTextNote.SetTextEx(entry.Discovered ? GetNote(entry) : GetUndiscoveredHint(entry));
        SetIngredients(GetIngredients(entry), entry.Discovered);
    }

    private static string GetStampText(UIScrollCollectionData entry)
    {
        if (!entry.IsDrink)
            return GetCategoryName(false);

        var drinkRow = GameInstance.Table.Get<CTable.DrinkRow>(entry.Tid);
        if (drinkRow == null)
            return GetCategoryName(true);

        return $"{GetCategoryName(true)} | {(drinkRow.DrinkTempType == CTable.eDrinkTempType.Ice ? "ICE" : "HOT")}";
    }

    // 디저트는 Bread.csv Desc, 음료는 Drink.csv Desc를 쓴다. 음료 Desc가 비어 있으면 온도·종류로 문장을 만든다.
    private static string GetNote(UIScrollCollectionData entry)
    {
        if (!entry.IsDrink)
        {
            var breadRow = GameInstance.Table.Get<CTable.BreadRow>(entry.Tid);
            var tags = breadRow.GetTags().ToList();
            string desc = breadRow?.Desc ?? string.Empty;
            return tags.Count > 0 ? $"{desc}\n맛: {string.Join(", ", tags)}" : desc;
        }

        var drinkRow = GameInstance.Table.Get<CTable.DrinkRow>(entry.Tid);
        if (drinkRow == null)
            return string.Empty;

        string temp = drinkRow.DrinkTempType == CTable.eDrinkTempType.Ice ? "차갑게" : "따뜻하게";
        var materialTable = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        var drinkTags = GetDrinkMaterialTids(drinkRow)
            .SelectMany(tid => materialTable?.Get(tid).GetTags() ?? Enumerable.Empty<string>())
            .Distinct()
            .ToList();

        string note = string.IsNullOrEmpty(drinkRow.Desc)
            ? $"{temp} 즐기는 {GetDrinkTypeName(drinkRow.DrinkType)} 음료예요."
            : drinkRow.Desc;
        return drinkTags.Count > 0 ? $"{note}\n맛: {string.Join(", ", drinkTags)}" : note;
    }

    private static string GetUndiscoveredHint(UIScrollCollectionData entry)
    {
        return entry.IsDrink
            ? "아직 발견하지 못한 레시피예요.\n가공섬 레시피 연구소에서 개발해 보세요."
            : "아직 배우지 못한 레시피예요.\n상점가 찍찍이에게서 레시피를 살 수 있어요.";
    }

    private static string GetDrinkTypeName(CTable.eDrinkType type)
    {
        switch (type)
        {
            case CTable.eDrinkType.Coffee: return "커피";
            case CTable.eDrinkType.Tea: return "차";
            case CTable.eDrinkType.Ade: return "에이드";
            case CTable.eDrinkType.Juice: return "주스";
            case CTable.eDrinkType.Smoothie: return "스무디";
            case CTable.eDrinkType.BubbleTea: return "버블티";
            default: return string.Empty;
        }
    }

    private static IEnumerable<int> GetDrinkMaterialTids(CTable.DrinkRow row)
    {
        return new[] { row.DrinkMaterial1, row.DrinkMaterial2, row.DrinkMaterial3, row.DrinkMaterial4, row.DrinkMaterial5 }
            .Where(tid => tid != 0);
    }

    // (재료 이름, 보유 수량) 목록.
    private static List<(string Name, int Owned)> GetIngredients(UIScrollCollectionData entry)
    {
        var result = new List<(string, int)>();

        if (entry.IsDrink)
        {
            var drinkRow = GameInstance.Table.Get<CTable.DrinkRow>(entry.Tid);
            if (drinkRow == null) return result;

            foreach (int tid in GetDrinkMaterialTids(drinkRow))
                result.Add((GameInstance.Table.Get<CTable.DrinkMaterialRow>(tid)?.Name ?? tid.ToString(), GetOwned(tid)));
        }
        else
        {
            foreach (int tid in GameInstance.Table.Get<CTable.BreadRow>(entry.Tid).GetMaterialTids())
                result.Add((GameInstance.Table.Get<CTable.BreadMaterialRow>(tid)?.Name ?? tid.ToString(), GetOwned(tid)));
        }

        return result;
    }

    private static int GetOwned(int materialTid) => GameInstance.Model.Material.Get(materialTid)?.Count.Value ?? 0;

    // 미발견 레시피는 재료 개수만 보여 주고 이름은 가린다.
    private void SetIngredients(List<(string Name, int Owned)> ingredients, bool discovered)
    {
        for (int i = 0; i < mIngredientRoots.Length; i++)
        {
            bool active = ingredients != null && i < ingredients.Count;
            mIngredientRoots[i].SetActive(active);
            if (!active) continue;

            mIngredientNames[i].SetTextEx(discovered ? ingredients[i].Name : "???");
            mIngredientOwned[i].SetTextEx(discovered ? $"보유 {ingredients[i].Owned}" : string.Empty);
        }
    }
}
