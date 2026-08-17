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

    [Serializable]
    private class FolderRule
    {
        public string FolderPath = "";
        public string Extensions = "";
        public string GroupName = "";
        public eAddressMode AddressMode = eAddressMode.ResourcesRelativePath;
        public string CustomPrefix = "";
        public string LabelsInput = "";
        public bool MoveOutOfResources = false;
        public string MoveTargetRoot = "Assets/AddressableAssets";
    }

    [Serializable]
    private class FileEntry
    {
        public string AssetPath = "";
        public string OwnerFolderPath = "";
        public bool Include = true;
        public string GroupName = "";
        public string Address = "";
        public string LabelsInput = "";
        public bool IsOverridden = false;
    }

    [Serializable]
    private class ConfigData
    {
        public List<FolderRule> FolderRules = new List<FolderRule>();
        public List<FileEntry> FileEntries = new List<FileEntry>();
    }

    private const string CONFIG_PATH = "Assets/Editor/AddressableSetupToolConfig.json";

    private ConfigData mConfig = new ConfigData();
    private string[] mGroupNames = new string[0];

    private Vector2 mFolderScroll;
    private Vector2 mFileScroll;

    [MenuItem("Tools/Addressables/어드레서블 세팅 툴")]
    public static void Open()
    {
        var window = GetWindow<AddressableSetupTool>("어드레서블 세팅 툴");
        window.minSize = new Vector2(520, 650);
        window.LoadConfig();
        window.RefreshGroupNames();
    }

    private void OnEnable()
    {
        LoadConfig();
        RefreshGroupNames();
    }

    private void OnDisable()
    {
        SaveConfig();
    }

    private void OnGUI()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("AddressableAssetSettings를 찾을 수 없습니다. Window > Asset Management > Addressables > Groups 에서 먼저 초기화하세요.", MessageType.Error);
            return;
        }

        DrawFolderRules();
        EditorGUILayout.Space(8);
        DrawFileTable();
        EditorGUILayout.Space(12);

        int includeCount = mConfig.FileEntries.Count(e => e.Include);
        using (new EditorGUI.DisabledScope(includeCount == 0))
        {
            if (GUILayout.Button($"체크된 {includeCount}개 파일을 어드레서블로 등록", GUILayout.Height(32)))
            {
                RegisterAll(settings);
            }
        }
    }

    private void DrawFolderRules()
    {
        EditorGUILayout.LabelField("등록된 폴더", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ 폴더 추가"))
                mConfig.FolderRules.Add(new FolderRule());
            if (GUILayout.Button("전체 스캔", GUILayout.Width(100)))
                ScanAll();
            if (GUILayout.Button("설정 저장", GUILayout.Width(80)))
                SaveConfig();
        }

        string groupHint = mGroupNames.Length > 0 ? string.Join(", ", mGroupNames) : "(없음)";
        EditorGUILayout.HelpBox($"기존 그룹: {groupHint}\n그룹 이름을 새로 입력하면 등록 시 자동 생성됩니다.", MessageType.Info);

        mFolderScroll = EditorGUILayout.BeginScrollView(mFolderScroll, GUILayout.Height(240));
        FolderRule toRemove = null;
        foreach (var rule in mConfig.FolderRules)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var folderAsset = string.IsNullOrEmpty(rule.FolderPath)
                        ? null
                        : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rule.FolderPath);

                    var newFolderAsset = EditorGUILayout.ObjectField("폴더", folderAsset, typeof(UnityEngine.Object), false);
                    if (newFolderAsset != folderAsset)
                    {
                        string path = AssetDatabase.GetAssetPath(newFolderAsset);
                        if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                            rule.FolderPath = path;
                    }

                    if (GUILayout.Button("x", GUILayout.Width(22)))
                        toRemove = rule;
                }

                rule.Extensions = EditorGUILayout.TextField("확장자 필터 (콤마, 비우면 전체)", rule.Extensions);
                rule.GroupName = EditorGUILayout.TextField("그룹 이름", rule.GroupName);
                rule.AddressMode = (eAddressMode)EditorGUILayout.EnumPopup("주소 규칙", rule.AddressMode);
                if (rule.AddressMode == eAddressMode.CustomPrefix)
                    rule.CustomPrefix = EditorGUILayout.TextField("접두사", rule.CustomPrefix);
                rule.LabelsInput = EditorGUILayout.TextField("라벨 (콤마 구분)", rule.LabelsInput);
                rule.MoveOutOfResources = EditorGUILayout.ToggleLeft("Resources 폴더면 아래 경로로 이동", rule.MoveOutOfResources);
                using (new EditorGUI.DisabledScope(!rule.MoveOutOfResources))
                    rule.MoveTargetRoot = EditorGUILayout.TextField("이동 대상 루트", rule.MoveTargetRoot);
            }
        }
        EditorGUILayout.EndScrollView();

        if (toRemove != null)
        {
            mConfig.FolderRules.Remove(toRemove);
            mConfig.FileEntries.RemoveAll(e => e.OwnerFolderPath == toRemove.FolderPath);
            SaveConfig();
        }
    }

    private void DrawFileTable()
    {
        EditorGUILayout.LabelField($"파일 목록 (총 {mConfig.FileEntries.Count}개)", EditorStyles.boldLabel);

        mFileScroll = EditorGUILayout.BeginScrollView(mFileScroll, GUILayout.Height(260));
        foreach (var rule in mConfig.FolderRules)
        {
            var entries = mConfig.FileEntries
                .Where(e => e.OwnerFolderPath == rule.FolderPath)
                .OrderBy(e => e.AssetPath)
                .ToList();

            if (entries.Count == 0)
                continue;

            EditorGUILayout.LabelField(rule.FolderPath, EditorStyles.miniBoldLabel);

            foreach (var entry in entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    entry.Include = EditorGUILayout.Toggle(entry.Include, GUILayout.Width(18));
                    EditorGUILayout.LabelField(Path.GetFileName(entry.AssetPath), GUILayout.Width(150));

                    EditorGUI.BeginChangeCheck();
                    string newGroup = EditorGUILayout.TextField(entry.GroupName, GUILayout.Width(110));
                    string newAddress = EditorGUILayout.TextField(entry.Address);
                    string newLabels = EditorGUILayout.TextField(entry.LabelsInput, GUILayout.Width(110));
                    if (EditorGUI.EndChangeCheck())
                    {
                        entry.GroupName = newGroup;
                        entry.Address = newAddress;
                        entry.LabelsInput = newLabels;
                        entry.IsOverridden = newGroup != rule.GroupName
                            || newAddress != BuildAddress(rule, entry.AssetPath)
                            || newLabels != rule.LabelsInput;
                    }

                    using (new EditorGUI.DisabledScope(!entry.IsOverridden))
                    {
                        if (GUILayout.Button("리셋", GUILayout.Width(45)))
                            ApplyRuleDefaults(rule, entry);
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanAll()
    {
        var previousByPath = mConfig.FileEntries
            .GroupBy(e => e.AssetPath)
            .ToDictionary(g => g.Key, g => g.First());

        var newEntries = new List<FileEntry>();

        foreach (var rule in mConfig.FolderRules)
        {
            if (string.IsNullOrEmpty(rule.FolderPath) || !AssetDatabase.IsValidFolder(rule.FolderPath))
                continue;

            var extensions = new HashSet<string>(
                rule.Extensions.Split(',').Select(e => e.Trim().TrimStart('.')).Where(e => e.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var guids = AssetDatabase.FindAssets("", new[] { rule.FolderPath });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;

                string ext = Path.GetExtension(assetPath).TrimStart('.');
                if (ext.Equals("cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (extensions.Count > 0 && !extensions.Contains(ext))
                    continue;

                if (previousByPath.TryGetValue(assetPath, out var existing) && existing.OwnerFolderPath == rule.FolderPath)
                {
                    newEntries.Add(existing);
                }
                else
                {
                    var entry = new FileEntry
                    {
                        AssetPath = assetPath,
                        OwnerFolderPath = rule.FolderPath,
                    };
                    ApplyRuleDefaults(rule, entry);
                    newEntries.Add(entry);
                }
            }
        }

        mConfig.FileEntries = newEntries;
        SaveConfig();
    }

    private void ApplyRuleDefaults(FolderRule rule, FileEntry entry)
    {
        entry.GroupName = rule.GroupName;
        entry.Address = BuildAddress(rule, entry.AssetPath);
        entry.LabelsInput = rule.LabelsInput;
        entry.IsOverridden = false;
    }

    private void RegisterAll(AddressableAssetSettings settings)
    {
        int count = 0;
        var groupCache = new Dictionary<string, AddressableAssetGroup>();

        foreach (var entry in mConfig.FileEntries)
        {
            if (!entry.Include || string.IsNullOrWhiteSpace(entry.GroupName))
                continue;

            string assetPath = entry.AssetPath;
            var rule = mConfig.FolderRules.FirstOrDefault(r => r.FolderPath == entry.OwnerFolderPath);

            if (rule != null && rule.MoveOutOfResources && assetPath.Contains("/Resources/"))
            {
                string moved = MoveOutOfResources(assetPath, rule.MoveTargetRoot);
                if (string.IsNullOrEmpty(moved))
                    continue;
                assetPath = moved;
                entry.AssetPath = moved;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                continue;

            if (!groupCache.TryGetValue(entry.GroupName, out var group))
            {
                group = GetOrCreateGroupByName(settings, entry.GroupName);
                groupCache[entry.GroupName] = group;
            }
            if (group == null)
                continue;

            var addrEntry = settings.CreateOrMoveEntry(guid, group, false, false);
            addrEntry.address = entry.Address;

            var labels = entry.LabelsInput.Split(',').Select(l => l.Trim()).Where(l => l.Length > 0);
            foreach (var label in labels)
            {
                if (!settings.GetLabels().Contains(label))
                    settings.AddLabel(label);
                addrEntry.SetLabel(label, true, false, false);
            }

            count++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
        SaveConfig();
        RefreshGroupNames();

        EditorUtility.DisplayDialog("완료", $"{count}개 에셋을 등록했습니다.", "확인");
        Repaint();
    }

    private AddressableAssetGroup GetOrCreateGroupByName(AddressableAssetSettings settings, string name)
    {
        var existing = settings.FindGroup(name);
        if (existing != null)
            return existing;

        return settings.CreateGroup(name, false, false, true, null,
            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
    }

    private void RefreshGroupNames()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        mGroupNames = settings != null
            ? settings.groups.Where(g => g != null && !g.ReadOnly).Select(g => g.Name).ToArray()
            : new string[0];
    }

    private string BuildAddress(FolderRule rule, string assetPath)
    {
        switch (rule.AddressMode)
        {
            case eAddressMode.FileName:
                return Path.GetFileNameWithoutExtension(assetPath);
            case eAddressMode.CustomPrefix:
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                return string.IsNullOrEmpty(rule.CustomPrefix) ? fileName : $"{rule.CustomPrefix.TrimEnd('/')}/{fileName}";
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

    private string MoveOutOfResources(string assetPath, string targetRoot)
    {
        const string marker = "/Resources/";
        int idx = assetPath.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return assetPath;

        string relative = assetPath.Substring(idx + marker.Length);
        string targetPath = $"{targetRoot.TrimEnd('/')}/{relative}";

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

    private void SaveConfig()
    {
        string dir = Path.GetDirectoryName(CONFIG_PATH);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(mConfig, true);
        File.WriteAllText(CONFIG_PATH, json);
        AssetDatabase.Refresh();
    }

    private void LoadConfig()
    {
        if (File.Exists(CONFIG_PATH))
        {
            string json = File.ReadAllText(CONFIG_PATH);
            mConfig = JsonUtility.FromJson<ConfigData>(json) ?? new ConfigData();
        }
        else
        {
            mConfig = new ConfigData();
        }
    }
}
