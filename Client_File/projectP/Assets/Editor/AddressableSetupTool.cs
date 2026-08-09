using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public class AddressableSetupTool : EditorWindow
{
    private enum eAddressMode
    {
        ResourcesRelativePath,
        FileName,
        CustomPrefix,
    }

    private List<UnityEngine.Object> mSelectedAssets = new List<UnityEngine.Object>();
    private string[] mGroupNames = new string[0];
    private int mGroupIndex = 0;
    private string mNewGroupName = "";

    private eAddressMode mAddressMode = eAddressMode.ResourcesRelativePath;
    private string mCustomPrefix = "";
    private string mLabelsInput = "";

    private bool mMoveOutOfResources = false;
    private string mMoveTargetRoot = "Assets/AddressableAssets";

    private Vector2 mSelectedScroll;
    private Vector2 mEntryScroll;

    [MenuItem("Tools/Addressables/어드레서블 세팅 툴")]
    public static void Open()
    {
        var window = GetWindow<AddressableSetupTool>("어드레서블 세팅 툴");
        window.minSize = new Vector2(420, 500);
        window.RefreshSelection();
        window.RefreshGroupNames();
    }

    private void OnEnable()
    {
        RefreshSelection();
        RefreshGroupNames();
    }

    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void OnGUI()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("AddressableAssetSettings를 찾을 수 없습니다. Window > Asset Management > Addressables > Groups 에서 먼저 초기화하세요.", MessageType.Error);
            return;
        }

        DrawSelectedAssets();
        EditorGUILayout.Space(8);
        DrawGroupSelector(settings);
        EditorGUILayout.Space(8);
        DrawAddressOptions();
        EditorGUILayout.Space(8);
        DrawLabelOptions();
        EditorGUILayout.Space(8);
        DrawMoveOptions();
        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(mSelectedAssets.Count == 0))
        {
            if (GUILayout.Button($"선택한 {mSelectedAssets.Count}개 에셋을 어드레서블로 등록", GUILayout.Height(32)))
            {
                RegisterSelected(settings);
            }
        }

        EditorGUILayout.Space(12);
        DrawGroupEntries(settings);
    }

    private void DrawSelectedAssets()
    {
        EditorGUILayout.LabelField("선택된 에셋 (Project 창에서 선택)", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("현재 선택으로 새로고침"))
                RefreshSelection();
            if (GUILayout.Button("비우기", GUILayout.Width(60)))
                mSelectedAssets.Clear();
        }

        mSelectedScroll = EditorGUILayout.BeginScrollView(mSelectedScroll, GUILayout.Height(120));
        for (int i = mSelectedAssets.Count - 1; i >= 0; i--)
        {
            var asset = mSelectedAssets[i];
            if (asset == null)
            {
                mSelectedAssets.RemoveAt(i);
                continue;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("x", GUILayout.Width(22)))
                    mSelectedAssets.RemoveAt(i);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawGroupSelector(AddressableAssetSettings settings)
    {
        EditorGUILayout.LabelField("대상 그룹", EditorStyles.boldLabel);

        if (mGroupNames.Length == 0)
            RefreshGroupNames();

        mGroupIndex = EditorGUILayout.Popup("그룹", mGroupIndex, mGroupNames);

        if (IsNewGroupSelected())
        {
            mNewGroupName = EditorGUILayout.TextField("새 그룹 이름", mNewGroupName);
        }
    }

    private void DrawAddressOptions()
    {
        EditorGUILayout.LabelField("주소(Address) 규칙", EditorStyles.boldLabel);
        mAddressMode = (eAddressMode)EditorGUILayout.EnumPopup("규칙 선택", mAddressMode);

        switch (mAddressMode)
        {
            case eAddressMode.ResourcesRelativePath:
                EditorGUILayout.HelpBox("Resources 폴더 기준 상대 경로를 주소로 사용합니다. (예: Furniture/Furniture_Table)\n테이블의 PrefabPath 값과 동일한 규칙이라 기존 데이터와 그대로 호환됩니다.", MessageType.Info);
                break;
            case eAddressMode.FileName:
                EditorGUILayout.HelpBox("파일 이름만 주소로 사용합니다. (예: Furniture_Table)", MessageType.Info);
                break;
            case eAddressMode.CustomPrefix:
                mCustomPrefix = EditorGUILayout.TextField("접두사(Prefix)", mCustomPrefix);
                EditorGUILayout.HelpBox("접두사/파일이름 형태로 주소를 만듭니다. (예: UI/버튼이름)", MessageType.Info);
                break;
        }
    }

    private void DrawLabelOptions()
    {
        EditorGUILayout.LabelField("라벨 (선택)", EditorStyles.boldLabel);
        mLabelsInput = EditorGUILayout.TextField("콤마(,)로 구분", mLabelsInput);
    }

    private void DrawMoveOptions()
    {
        EditorGUILayout.LabelField("Resources 폴더 처리", EditorStyles.boldLabel);
        mMoveOutOfResources = EditorGUILayout.ToggleLeft("Resources 폴더 안에 있으면 아래 경로로 실제 이동", mMoveOutOfResources);
        using (new EditorGUI.DisabledScope(!mMoveOutOfResources))
        {
            mMoveTargetRoot = EditorGUILayout.TextField("이동 대상 루트", mMoveTargetRoot);
        }
        EditorGUILayout.HelpBox("Resources 폴더의 에셋은 항상 빌드에 포함되므로, 어드레서블로 관리하려면 폴더 밖으로 옮기는 것을 권장합니다.", MessageType.None);
    }

    private void DrawGroupEntries(AddressableAssetSettings settings)
    {
        var group = GetSelectedExistingGroup(settings);
        if (group == null)
            return;

        EditorGUILayout.LabelField($"'{group.Name}' 그룹의 등록된 에셋 ({group.entries.Count}개)", EditorStyles.boldLabel);

        mEntryScroll = EditorGUILayout.BeginScrollView(mEntryScroll, GUILayout.Height(160));
        AddressableAssetEntry toRemove = null;
        foreach (var entry in group.entries.OrderBy(e => e.address))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(entry.address, GUILayout.Width(180));
                EditorGUILayout.LabelField(entry.AssetPath, EditorStyles.miniLabel);
                if (GUILayout.Button("제거", GUILayout.Width(50)))
                    toRemove = entry;
            }
        }
        EditorGUILayout.EndScrollView();

        if (toRemove != null)
        {
            settings.RemoveAssetEntry(toRemove.guid);
            AssetDatabase.SaveAssets();
        }
    }

    private void RefreshSelection()
    {
        mSelectedAssets = Selection.objects
            .Where(o => o != null)
            .Where(o => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)))
            .Where(o => AssetDatabase.GetAssetPath(o).StartsWith("Assets/"))
            .Distinct()
            .ToList();
    }

    private void RefreshGroupNames()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var names = settings != null
            ? settings.groups.Where(g => g != null && !g.ReadOnly).Select(g => g.Name).ToList()
            : new List<string>();

        names.Add("+ 새 그룹 생성");
        mGroupNames = names.ToArray();

        if (mGroupIndex >= mGroupNames.Length)
            mGroupIndex = 0;
    }

    private bool IsNewGroupSelected()
    {
        return mGroupNames.Length > 0 && mGroupIndex == mGroupNames.Length - 1;
    }

    private AddressableAssetGroup GetSelectedExistingGroup(AddressableAssetSettings settings)
    {
        if (IsNewGroupSelected() || mGroupNames.Length == 0)
            return null;

        return settings.FindGroup(mGroupNames[mGroupIndex]);
    }

    private AddressableAssetGroup GetOrCreateTargetGroup(AddressableAssetSettings settings)
    {
        if (!IsNewGroupSelected())
            return settings.FindGroup(mGroupNames[mGroupIndex]);

        if (string.IsNullOrWhiteSpace(mNewGroupName))
        {
            EditorUtility.DisplayDialog("알림", "새 그룹 이름을 입력하세요.", "확인");
            return null;
        }

        var existing = settings.FindGroup(mNewGroupName);
        if (existing != null)
            return existing;

        return settings.CreateGroup(mNewGroupName, false, false, true, null,
            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
    }

    private void RegisterSelected(AddressableAssetSettings settings)
    {
        if (mSelectedAssets.Count == 0)
            return;

        var group = GetOrCreateTargetGroup(settings);
        if (group == null)
            return;

        var labels = mLabelsInput.Split(',')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        foreach (var label in labels)
        {
            if (!settings.GetLabels().Contains(label))
                settings.AddLabel(label);
        }

        int count = 0;
        foreach (var asset in mSelectedAssets)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                continue;

            if (mMoveOutOfResources && path.Contains("/Resources/"))
            {
                string movedPath = MoveOutOfResources(path);
                if (string.IsNullOrEmpty(movedPath))
                    continue;
                path = movedPath;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                continue;

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = BuildAddress(path);
            foreach (var label in labels)
                entry.SetLabel(label, true, false, false);

            count++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
        RefreshGroupNames();

        EditorUtility.DisplayDialog("완료", $"{count}개 에셋을 '{group.Name}' 그룹에 등록했습니다.", "확인");
        Repaint();
    }

    private string BuildAddress(string assetPath)
    {
        switch (mAddressMode)
        {
            case eAddressMode.FileName:
                return Path.GetFileNameWithoutExtension(assetPath);
            case eAddressMode.CustomPrefix:
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                return string.IsNullOrEmpty(mCustomPrefix) ? fileName : $"{mCustomPrefix.TrimEnd('/')}/{fileName}";
            case eAddressMode.ResourcesRelativePath:
            default:
                return GetResourcesRelativePath(assetPath);
        }
    }

    private string GetResourcesRelativePath(string assetPath)
    {
        const string marker = "/Resources/";
        int idx = assetPath.IndexOf(marker, StringComparison.Ordinal);

        string relative = idx >= 0
            ? assetPath.Substring(idx + marker.Length)
            : (assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath);

        int extIdx = relative.LastIndexOf('.');
        if (extIdx >= 0)
            relative = relative.Substring(0, extIdx);

        return relative;
    }

    private string MoveOutOfResources(string assetPath)
    {
        const string marker = "/Resources/";
        int idx = assetPath.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return assetPath;

        string relative = assetPath.Substring(idx + marker.Length);
        string targetPath = $"{mMoveTargetRoot.TrimEnd('/')}/{relative}";

        string targetDir = Path.GetDirectoryName(targetPath)?.Replace("\\", "/");
        if (!string.IsNullOrEmpty(targetDir))
            EnsureFolderExists(targetDir);

        string error = AssetDatabase.MoveAsset(assetPath, targetPath);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[AddressableSetupTool] 파일 이동 실패: {assetPath} -> {targetPath} ({error})");
            return null;
        }

        return targetPath;
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
