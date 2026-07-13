using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CsvClassGenerator
{
    public static void ConvertCsvToCs(string csvFilePath)
    {
        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 4) // �ּ� ������ ����� �ʿ�
        {
            Logger.Error("CSV ���� ������ �ùٸ��� �ʽ��ϴ�.");
            return;
        }

        string className = Path.GetFileNameWithoutExtension(csvFilePath);
        string[] headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();
        string[] types = lines[1].Split(',').Select(s => s.Trim()).ToArray();

        // 1. Ŭ���� �ڵ� ���� �� ����
        string code = GenerateClassCode(className, headers, types);
        SaveFile(className + ".cs", code);

        // 2. Enum ���� ó�� (Enum(Name) ���� ����)
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i].StartsWith("Enum"))
            {
                string enumName = types[i].Replace("Enum(", "").Replace(")", "");
                GenerateEnumFile(enumName, lines, i); // ������ �࿡�� �� ����
            }
        }

        AssetDatabase.Refresh();
    }

    private static void GenerateEnumFile(string enumName, string[] lines, int colIndex)
    {
        // ������ ��(3��° �ε�������)���� ����ũ�� �� ����
        HashSet<string> enumValues = new HashSet<string>();
        for (int i = 3; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (colIndex < values.Length)
            {
                enumValues.Add(values[colIndex].Trim());
            }
        }

        string enumCode = $"public enum {enumName}\n{{\n    " + string.Join(",\n    ", enumValues) + "\n}";
        SaveFile(enumName + ".cs", enumCode);
    }

    private static string GenerateClassCode(string className, string[] headers, string[] types)
    {
        string fields = "";
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].StartsWith("@")) continue;

            string type = types[i];
            // Ÿ�� ��ȯ (List �� Enum)
            if (type.StartsWith("[")) type = "List<int>";
            else if (type.StartsWith("Enum")) type = type.Replace("Enum(", "").Replace(")", "");

            fields += $"    public {type} {headers[i]};\n";
        }

        return $@"using System;
using System.Collections.Generic;

namespace CTable
{{
[Serializable]
public class {className} 
{{
public override int key {{ get {{ return {fields[0]}; }} }}
{fields}
}}
}}";
    }

    private static void SaveFile(string fileName, string content)
    {
        string folderPath = Application.dataPath + "/CTable";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, fileName), content);
    }
}