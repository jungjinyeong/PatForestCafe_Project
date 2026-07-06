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
        if (lines.Length < 4) // 최소 데이터 행까지 필요
        {
            Debug.LogError("CSV 파일 형식이 올바르지 않습니다.");
            return;
        }

        string className = Path.GetFileNameWithoutExtension(csvFilePath);
        string[] headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();
        string[] types = lines[1].Split(',').Select(s => s.Trim()).ToArray();

        // 1. 클래스 코드 생성 및 저장
        string code = GenerateClassCode(className, headers, types);
        SaveFile(className + ".cs", code);

        // 2. Enum 생성 처리 (Enum(Name) 패턴 감지)
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i].StartsWith("Enum"))
            {
                string enumName = types[i].Replace("Enum(", "").Replace(")", "");
                GenerateEnumFile(enumName, lines, i); // 데이터 행에서 값 추출
            }
        }

        AssetDatabase.Refresh();
    }

    private static void GenerateEnumFile(string enumName, string[] lines, int colIndex)
    {
        // 데이터 행(3번째 인덱스부터)에서 유니크한 값 추출
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
            // 타입 변환 (List 및 Enum)
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