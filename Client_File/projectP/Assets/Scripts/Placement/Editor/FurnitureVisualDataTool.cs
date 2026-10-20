using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// 가구 아트 설정(FurnitureVisualData) 관리 툴.
// - Datas/FurnitureVisual 아래에 에셋을 만들거나 이름을 바꾸면 Addressables 주소 FurnitureVisual/{에셋명}으로 자동 등록한다
//   (CSV의 VisualPath에 이 주소를 적는다 — 주소를 손으로 입력하지 않게 하려는 목적).
// - Tools/Furniture/Validate VisualPath: Furniture.csv / SubFurniture.csv의 VisualPath가 실제 아트 설정을 가리키는지,
//   그 아트 설정에 프리팹이 지정돼 있는지 검사한다.
public class FurnitureVisualDataTool : AssetPostprocessor
{
    public const string VISUAL_ROOT = "Assets/Datas/FurnitureVisual";
    public const string ADDRESS_PREFIX = "FurnitureVisual/";
    private static readonly string[] CSV_PATHS = { "Assets/CSV/Furniture.csv", "Assets/CSV/SubFurniture.csv" };
    private const string VISUAL_PATH_COLUMN = "VisualPath";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets.Concat(movedAssets))
        {
            if (!path.StartsWith(VISUAL_ROOT + "/") || !path.EndsWith(".asset"))
                continue;

            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(FurnitureVisualData))
                Register(path, false);
        }
    }

    [MenuItem("Tools/Furniture/Register Visual Data Addressables")]
    public static void RegisterAll()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:FurnitureVisualData", new[] { VISUAL_ROOT }))
        {
            if (Register(AssetDatabase.GUIDToAssetPath(guid), false))
                count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FurnitureVisualDataTool] 아트 설정 {count}개를 Addressables에 등록했습니다.");
    }

    [MenuItem("Tools/Furniture/Validate VisualPath")]
    public static void Validate()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[FurnitureVisualDataTool] Addressables 설정이 없습니다.");
            return;
        }

        var visualByAddress = new Dictionary<string, FurnitureVisualData>();
        foreach (var group in settings.groups.Where(g => g != null))
        {
            foreach (var entry in group.entries)
            {
                var visual = AssetDatabase.LoadAssetAtPath<FurnitureVisualData>(AssetDatabase.GUIDToAssetPath(entry.guid));
                if (visual != null)
                    visualByAddress[entry.address] = visual;
            }
        }

        int errorCount = 0;
        int rowCount = 0;
        foreach (string csvPath in CSV_PATHS)
        {
            if (!File.Exists(csvPath))
                continue;

            string[] lines = File.ReadAllLines(csvPath);
            string[] headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            int colTid = System.Array.IndexOf(headers, "Tid");
            int colVisual = System.Array.IndexOf(headers, VISUAL_PATH_COLUMN);
            if (colVisual < 0)
            {
                Debug.LogError($"[FurnitureVisualDataTool] {csvPath}에 {VISUAL_PATH_COLUMN} 컬럼이 없습니다.");
                errorCount++;
                continue;
            }

            // 0행=컬럼명, 1행=타입, 2행=설명(@...). Tid가 숫자인 행만 데이터로 본다.
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                if (values.Length <= colVisual || !int.TryParse(values[colTid].Trim(), out int tid))
                    continue;

                rowCount++;
                string address = values[colVisual].Trim();
                if (string.IsNullOrEmpty(address))
                {
                    Debug.LogError($"[FurnitureVisualDataTool] {Path.GetFileName(csvPath)} Tid={tid}: VisualPath가 비어 있습니다.");
                    errorCount++;
                    continue;
                }

                if (!visualByAddress.TryGetValue(address, out var visual))
                {
                    Debug.LogError($"[FurnitureVisualDataTool] {Path.GetFileName(csvPath)} Tid={tid}: '{address}' 주소의 아트 설정이 없습니다.");
                    errorCount++;
                    continue;
                }

                if (visual.Prefab == null || !visual.Prefab.RuntimeKeyIsValid())
                {
                    Debug.LogError($"[FurnitureVisualDataTool] {Path.GetFileName(csvPath)} Tid={tid}: '{visual.name}'에 프리팹이 지정되지 않았습니다.", visual);
                    errorCount++;
                }
                else if (settings.FindAssetEntry(visual.Prefab.AssetGUID) == null)
                {
                    Debug.LogError($"[FurnitureVisualDataTool] {Path.GetFileName(csvPath)} Tid={tid}: '{visual.name}'의 프리팹이 Addressables에 등록돼 있지 않습니다.", visual);
                    errorCount++;
                }
            }
        }

        if (errorCount == 0)
            Debug.Log($"[FurnitureVisualDataTool] VisualPath 검사 통과 ({rowCount}행).");
        else
            Debug.LogError($"[FurnitureVisualDataTool] VisualPath 검사 실패 {errorCount}건 ({rowCount}행). 위 로그를 확인하세요.");
    }

    // 주소 = FurnitureVisual/{에셋명}. 이미 같은 주소면 아무것도 하지 않는다.
    // 임포트 처리(OnPostprocessAllAssets) 중에는 SaveAssets를 부르지 않는다(설정은 dirty로 남겨 다음 저장 때 기록).
    private static bool Register(string assetPath, bool save)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return false;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        string address = ADDRESS_PREFIX + Path.GetFileNameWithoutExtension(assetPath);
        var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
        if (entry.address == address)
            return true;

        entry.SetAddress(address, false);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        if (save)
            AssetDatabase.SaveAssets();
        return true;
    }
}
