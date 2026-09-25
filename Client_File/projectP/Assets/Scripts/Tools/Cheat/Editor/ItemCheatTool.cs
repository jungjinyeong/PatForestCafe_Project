#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 아이템 치트 툴 — Play 모드에서 재화/아이템/음료 재료/빵 재료 수량을 바로 조절한다.
// 런타임 모델(GameInstance.Model)을 직접 건드리므로 Play 모드 + GameInstance 초기화 이후에만 동작한다.
// 주의: 30초 자동 저장/종료 시 저장 때문에 바꾼 수량이 실제 save.json에 남는다.
public class ItemCheatTool : EditorWindow
{
    private enum eTab
    {
        Wealth,
        Item,
        DrinkMaterial,
        BreadMaterial,
    }

    private static readonly string[] TAB_NAMES = { "재화", "아이템", "음료 재료", "빵 재료" };
    private static readonly int[] QUICK_AMOUNTS = { 1, 10, 100, 1000, 10000 };

    // 한 행에서 조작할 대상. 모델 타입이 제각각(WealthData/ItemData/MaterialData)이라 델리게이트로 감싼다.
    private class Entry
    {
        public int Tid;
        public string Name;
        public Func<int> GetCount;
        public Action<int> SetCount;
    }

    private eTab mTab = eTab.Wealth;
    private string mSearch = string.Empty;
    private int mAmount = 10;
    private Vector2 mScroll;

    [MenuItem("Tools/Cheat/아이템 치트")]
    private static void OpenWindow()
    {
        var window = GetWindow<ItemCheatTool>("아이템 치트");
        window.minSize = new Vector2(420, 300);
        window.Show();
    }

    // Play 중 수량 변화(자동 획득/소모)를 계속 보여주기 위해 주기적으로 다시 그린다.
    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying)
            Repaint();
    }

    private void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용할 수 있습니다.", MessageType.Info);
            return;
        }

        if (GameInstance.Model == null || GameInstance.Model.Item == null || GameInstance.Model.Material == null || GameInstance.Table == null)
        {
            EditorGUILayout.HelpBox("GameInstance 초기화를 기다리는 중입니다.", MessageType.Warning);
            return;
        }

        mTab = (eTab)GUILayout.Toolbar((int)mTab, TAB_NAMES);
        EditorGUILayout.Space(4);

        DrawAmountControls();
        mSearch = EditorGUILayout.TextField("검색 (이름/Tid)", mSearch);

        var entries = FilterEntries(BuildEntries(mTab));

        DrawBulkControls(entries);
        EditorGUILayout.Space(4);
        DrawEntryList(entries);

        EditorGUILayout.Space(4);
        DrawSaveControls();
    }

    #region Draw

    private void DrawAmountControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            mAmount = Mathf.Max(0, EditorGUILayout.IntField("수량", mAmount));
            foreach (int quick in QUICK_AMOUNTS)
            {
                if (GUILayout.Button(quick.ToString("N0"), EditorStyles.miniButton, GUILayout.Width(52)))
                {
                    mAmount = quick;
                    GUI.FocusControl(null);
                }
            }
        }
    }

    private void DrawBulkControls(List<Entry> entries)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label($"{entries.Count}개 항목", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(entries.Count == 0))
            {
                if (GUILayout.Button($"목록 전체 +{mAmount:N0}", GUILayout.Width(120)))
                {
                    foreach (var entry in entries)
                        entry.SetCount(ClampAdd(entry.GetCount(), mAmount));
                }

                if (GUILayout.Button("목록 전체 0", GUILayout.Width(90)) &&
                    EditorUtility.DisplayDialog("아이템 치트", $"현재 목록 {entries.Count}개 항목의 수량을 모두 0으로 만들까요?", "0으로", "취소"))
                {
                    foreach (var entry in entries)
                        entry.SetCount(0);
                }
            }
        }
    }

    private void DrawEntryList(List<Entry> entries)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Tid", GUILayout.Width(60));
            GUILayout.Label("이름", GUILayout.MinWidth(80));
            GUILayout.Label("보유", GUILayout.Width(80));
            GUILayout.Label(string.Empty, GUILayout.Width(196));
        }

        mScroll = EditorGUILayout.BeginScrollView(mScroll);
        foreach (var entry in entries)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(entry.Tid.ToString(), GUILayout.Width(60));
                GUILayout.Label(entry.Name, GUILayout.MinWidth(80));
                GUILayout.Label(entry.GetCount().ToString("N0"), EditorStyles.boldLabel, GUILayout.Width(80));

                if (GUILayout.Button($"+{mAmount:N0}", GUILayout.Width(56)))
                    entry.SetCount(ClampAdd(entry.GetCount(), mAmount));

                if (GUILayout.Button($"-{mAmount:N0}", GUILayout.Width(56)))
                    entry.SetCount(Mathf.Max(0, entry.GetCount() - mAmount));

                if (GUILayout.Button("설정", GUILayout.Width(40)))
                    entry.SetCount(mAmount);

                if (GUILayout.Button("0", GUILayout.Width(28)))
                    entry.SetCount(0);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSaveControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.HelpBox("자동 저장(30초)·종료 시 저장으로 바꾼 수량이 실제 save.json에 남습니다.", MessageType.None);

            if (GUILayout.Button("지금 저장", GUILayout.Width(80), GUILayout.Height(38)) && GameInstance.Save != null)
                GameInstance.Save.Save();
        }
    }

    #endregion

    #region Entries

    private static List<Entry> BuildEntries(eTab tab)
    {
        var entries = new List<Entry>();

        switch (tab)
        {
            case eTab.Wealth:
                foreach (var wealth in GameInstance.Model.Item.GetAllWealth())
                {
                    var target = wealth;
                    entries.Add(new Entry
                    {
                        Tid = target.Tid,
                        Name = $"{target.mRow.ItemName} ({target.MoneyType})",
                        GetCount = () => target.Count.Value,
                        SetCount = amount => target.Set(amount),
                    });
                }
                break;

            case eTab.Item:
                foreach (var item in GameInstance.Model.Item.GetAll())
                {
                    var target = item;
                    entries.Add(new Entry
                    {
                        Tid = target.Tid,
                        Name = target.mRow.ItemName,
                        GetCount = () => target.Count.Value,
                        SetCount = amount => target.Set(amount),
                    });
                }
                break;

            case eTab.DrinkMaterial:
                AddMaterialEntries(entries, GameInstance.Table.GetTable<CTable.DrinkMaterialRow>()?.All.Keys);
                break;

            case eTab.BreadMaterial:
                AddMaterialEntries(entries, GameInstance.Table.GetTable<CTable.BreadMaterialRow>()?.All.Keys);
                break;
        }

        return entries.OrderBy(e => e.Tid).ToList();
    }

    // 음료/빵 재료는 MaterialModel 하나에 섞여 있어 테이블 Tid 목록으로 구분한다.
    private static void AddMaterialEntries(List<Entry> entries, IEnumerable<int> tids)
    {
        if (tids == null) return;

        foreach (int tid in tids)
        {
            var material = GameInstance.Model.Material.Get(tid);
            if (material == null) continue;

            entries.Add(new Entry
            {
                Tid = material.Tid,
                Name = material.Name,
                GetCount = () => material.Count.Value,
                SetCount = amount => material.Set(amount),
            });
        }
    }

    private List<Entry> FilterEntries(List<Entry> entries)
    {
        if (string.IsNullOrWhiteSpace(mSearch))
            return entries;

        string keyword = mSearch.Trim();
        return entries
            .Where(e => (e.Name != null && e.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        e.Tid.ToString().Contains(keyword))
            .ToList();
    }

    #endregion

    private static int ClampAdd(int current, int amount)
    {
        long sum = (long)current + amount;
        return sum > int.MaxValue ? int.MaxValue : (int)sum;
    }
}
#endif
