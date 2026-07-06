using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TableManager
{
    // 키: Row 타입 (e.g. CTable.ItemRow), 값: 해당 TableBaseGroup 인스턴스
    private readonly Dictionary<Type, TableBaseGroupBase> _groups = new Dictionary<Type, TableBaseGroupBase>();

    public void LoadAllTables()
    {
#if UNITY_EDITOR
        string csvFolder = Path.Combine(Application.dataPath, "CSV");
        if (!Directory.Exists(csvFolder))
        {
            Debug.LogWarning($"[TableManager] CSV 폴더를 찾을 수 없습니다: {csvFolder}");
            return;
        }

        foreach (string filePath in Directory.GetFiles(csvFolder, "*.csv"))
        {
            string tableName = Path.GetFileNameWithoutExtension(filePath);
            string[] lines = File.ReadAllLines(filePath);
            RegisterTable(tableName, lines);
        }
#else
        // 빌드 환경: Assets/Resources/CSV 폴더에 CSV 파일을 TextAsset으로 배치 필요
        TextAsset[] csvAssets = Resources.LoadAll<TextAsset>("CSV");
        foreach (TextAsset csv in csvAssets)
        {
            string[] lines = csv.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            RegisterTable(csv.name, lines);
        }
#endif
    }

    private void RegisterTable(string tableName, string[] lines)
    {
        // 생성된 그룹 클래스는 CTable 네임스페이스에 위치
        string groupClassName = $"CTable.{tableName}Group";
        Type groupType = FindType(groupClassName);

        if (groupType == null)
        {
            Debug.LogWarning($"[TableManager] 그룹 클래스를 찾을 수 없습니다: {groupClassName}");
            return;
        }

        var group = (TableBaseGroupBase)Activator.CreateInstance(groupType);
        group.Load(lines);

        // TableBaseGroup<T>의 T(Row 타입)를 리플렉션으로 추출해 키로 사용
        Type baseType = groupType.BaseType;
        if (baseType != null && baseType.IsGenericType)
        {
            Type rowType = baseType.GetGenericArguments()[0];
            _groups[rowType] = group;
            Debug.Log($"[TableManager] {tableName} 로드 완료 ({rowType.FullName})");
        }
    }

    private static Type FindType(string fullTypeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(fullTypeName);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>
    /// Tid로 Row를 반환합니다. 예: TableManager.Instance.Get&lt;CTable.ItemRow&gt;(1001)
    /// </summary>
    public T Get<T>(int tid) where T : TableBaseRow, new()
    {
        var group = GetGroup<T>();
        if (group != null)
        {
            return group.Get(tid);
        }
        Debug.LogWarning($"[TableManager] {typeof(T).FullName}에 해당하는 그룹을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// Row 타입에 해당하는 TableBaseGroup 전체를 반환합니다.
    /// </summary>
    public TableBaseGroup<T> GetGroup<T>() where T : TableBaseRow, new()
    {
        if (_groups.TryGetValue(typeof(T), out TableBaseGroupBase groupBase) &&
            groupBase is TableBaseGroup<T> group)
        {
            return group;
        }
        return null;
    }
}
