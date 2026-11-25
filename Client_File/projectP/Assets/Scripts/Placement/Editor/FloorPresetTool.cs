using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

// 층 기본 배치(FloorPresetData) 에셋을 만들거나 이름을 바꾸면 Addressables 주소 FloorPreset/{에셋명}으로 자동 등록한다.
// 폴더와 상관없이 타입으로 판단한다. 런타임은 FloorPreset/FP_{층Tid}로 찾으므로 에셋 이름은 FP_{층Tid}여야 한다.
public class FloorPresetTool : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets.Concat(movedAssets))
        {
            if (!path.EndsWith(".asset") || AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(FloorPresetData))
                continue;

            Register(path);
        }
    }

    [MenuItem("Tools/Furniture/Register Floor Preset Addressables")]
    public static void RegisterAll()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:FloorPresetData"))
        {
            if (Register(AssetDatabase.GUIDToAssetPath(guid)))
                count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FloorPresetTool] 층 기본 배치 {count}개를 Addressables에 등록했습니다.");
    }

    // 주소 = FloorPreset/{에셋명}. 이미 같은 주소면 아무것도 하지 않는다.
    // 임포트 처리 중에는 SaveAssets를 부르지 않는다(설정은 dirty로 남겨 다음 저장 때 기록).
    private static bool Register(string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return false;

        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        if (!assetName.StartsWith(FloorPresetData.ASSET_PREFIX))
            Debug.LogWarning($"[FloorPresetTool] '{assetPath}': 런타임은 {FloorPresetData.ASSET_PREFIX}{{층Tid}} 이름으로 찾습니다. 이름을 확인하세요.");

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        string address = FloorPresetData.ADDRESS_PREFIX + assetName;
        var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
        if (entry.address == address)
            return true;

        entry.SetAddress(address, false);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        return true;
    }
}
