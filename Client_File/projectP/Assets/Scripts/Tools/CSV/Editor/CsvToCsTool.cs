#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CsvToCsTool : OdinEditorWindow
{
    [MenuItem("Tools/CSV to CS")]
    private static void OpenWindow() => GetWindow<CsvToCsTool>().Show();

    [Button("Assets/CSV 전체 변환")]
    public void ConvertAllCsvToCs()
    {
        string csvFolder = Path.Combine(Application.dataPath, "CSV");
        if (!Directory.Exists(csvFolder))
        {
            Logger.Error($"CSV 폴더를 찾을 수 없습니다: {csvFolder}");
            return;
        }

        string[] files = Directory.GetFiles(csvFolder, "*.csv");
        if (files.Length == 0)
        {
            Logger.Warning("Assets/CSV 폴더에 CSV 파일이 없습니다.");
            return;
        }

        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            if (lines.Length < 4)
            {
                Logger.Warning($"형식 오류로 건너뜀: {file}");
                continue;
            }
            ProcessCsvFile(file, lines);
        }

        RebuildTableEnumFile();

        AssetDatabase.Refresh();
        Logger.Log($"전체 변환 완료! ({files.Length}개 파일 처리)");
    }

    // ──────────────────────────────────────────────
    // Core
    // ──────────────────────────────────────────────

    private void ProcessCsvFile(string filePath, string[] lines)
    {
        string className = Path.GetFileNameWithoutExtension(filePath);
        string[] headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();
        string[] types   = lines[1].Split(',').Select(s => s.Trim()).ToArray();

        SaveFile(className + "Row.cs",   GenerateRowCode(className, headers, types));
        SaveFile(className + "Table.cs", GenerateGroupCode(className, headers, types));
    }

    // Assets/CSV 전체를 스캔해 TableEnum.cs 하나로 재생성
    private void RebuildTableEnumFile()
    {
        string csvFolder = Path.Combine(Application.dataPath, "CSV");
        if (!Directory.Exists(csvFolder)) return;

        var enumMap  = new Dictionary<string, List<string>>();
        var enumSeen = new Dictionary<string, HashSet<string>>();

        foreach (string file in Directory.GetFiles(csvFolder, "*.csv"))
        {
            string[] lines = File.ReadAllLines(file);
            if (lines.Length < 4) continue;

            string[] types = lines[1].Split(',').Select(s => s.Trim()).ToArray();

            for (int col = 0; col < types.Length; col++)
            {
                if (!types[col].StartsWith("Enum")) continue;

                string enumName = types[col].Replace("Enum(", "").Replace(")", "").Trim();
                if (!enumMap.ContainsKey(enumName))
                {
                    enumMap[enumName]  = new List<string>();
                    enumSeen[enumName] = new HashSet<string>();
                }

                for (int row = 3; row < lines.Length; row++)
                {
                    string[] values = lines[row].Split(',');
                    if (col >= values.Length) continue;
                    string val = values[col].Trim();
                    if (!string.IsNullOrEmpty(val) && enumSeen[enumName].Add(val))
                        enumMap[enumName].Add(val);
                }
            }
        }

        if (enumMap.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("namespace CTable");
        sb.AppendLine("{");
        foreach (var kvp in enumMap)
        {
            sb.AppendLine($"    public enum {kvp.Key}");
            sb.AppendLine("    {");
            sb.AppendLine("        " + string.Join(",\n        ", kvp.Value));
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        // 마지막 빈 줄 제거 후 namespace 닫기
        while (sb.Length > 0 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r'))
            sb.Length--;
        sb.AppendLine();
        sb.Append("}");

        SaveFile("TableEnum.cs", sb.ToString());
    }

    // ──────────────────────────────────────────────
    // Code generators
    // ──────────────────────────────────────────────

    private string GenerateRowCode(string className, string[] headers, string[] types)
    {
        var fields = new StringBuilder();
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].StartsWith("@")) continue;

            string type = types[i];
            if (type.StartsWith("["))         type = "List<int>";
            else if (type.StartsWith("Enum")) type = type.Replace("Enum(", "").Replace(")", "").Trim();

            fields.AppendLine($"        public {type} {headers[i]};");
        }

        return $@"using System;
using System.Collections.Generic;

namespace CTable
{{
    [Serializable]
    public class {className}Row : TableBaseRow
    {{
        public override int key => Tid;
{fields}    }}
}}";
    }

    private string GenerateGroupCode(string className, string[] headers, string[] types)
    {
        var parseLines = new StringBuilder();
        for (int colIdx = 0; colIdx < headers.Length; colIdx++)
        {
            if (headers[colIdx].StartsWith("@")) continue;

            string parseLine = BuildParseStatement(headers[colIdx], types[colIdx], colIdx);
            parseLines.AppendLine($"                {parseLine}");
        }

        return $@"using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{{
    public class {className}Table : TableBaseGroup<{className}Row>
    {{
        public override void Load(string[] lines)
        {{
            for (int i = 3; i < lines.Length; i++)
            {{
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < {headers.Length}) continue;

                var row = new {className}Row();
{parseLines}
                AddRow(row.Tid, row);
            }}
        }}
    }}
}}";
    }

    private string BuildParseStatement(string fieldName, string rawType, int colIdx)
    {
        string col = $"values[{colIdx}].Trim()";

        if (rawType == "int")
            return $"row.{fieldName} = int.TryParse({col}, out int _{fieldName}) ? _{fieldName} : 0;";

        if (rawType == "float")
            return $"row.{fieldName} = float.TryParse({col}, out float _{fieldName}) ? _{fieldName} : 0f;";

        if (rawType == "long")
            return $"row.{fieldName} = long.TryParse({col}, out long _{fieldName}) ? _{fieldName} : 0;";

        if (rawType == "bool")
            return $"row.{fieldName} = bool.TryParse({col}, out bool _{fieldName}) && _{fieldName};";

        if (rawType == "string")
            return $"row.{fieldName} = {col};";

        if (rawType.StartsWith("Enum"))
        {
            string enumType = rawType.Replace("Enum(", "").Replace(")", "").Trim();
            return $"row.{fieldName} = ({enumType})Enum.Parse(typeof({enumType}), {col});";
        }

        if (rawType.StartsWith("["))
            return $"row.{fieldName} = string.IsNullOrEmpty({col}) ? new List<int>() : {col}.Split('|').Select(int.Parse).ToList();";

        return $"row.{fieldName} = {col};";
    }

    // ──────────────────────────────────────────────
    // File I/O
    // ──────────────────────────────────────────────

    private void SaveFile(string fileName, string content)
    {
        string folderPath = Application.dataPath + "/CTable";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, fileName), content, Encoding.UTF8);
    }
}
#endif
