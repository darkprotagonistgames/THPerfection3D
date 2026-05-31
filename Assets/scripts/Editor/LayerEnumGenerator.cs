using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically generates a Layers enum from the project's layer names.
/// Triggers on every TagManager asset change (layer rename, add, remove).
/// </summary>
[InitializeOnLoad]
public static class LayerEnumGenerator
{
    private const string OutputPath = "Assets/Scripts/Layers.cs";
    private const string EnumName = "Layers";

    static LayerEnumGenerator()
    {
        GenerateEnum();
    }

    /// <summary>
    /// Generates the Layers enum file at the output path.
    /// </summary>
    public static void GenerateEnum()
    {
        var sb = new StringBuilder();

        sb.AppendLine("// AUTO-GENERATED — do not edit manually.");
        sb.AppendLine("// Re-generated every time layer names change in Project Settings.");
        sb.AppendLine();
        sb.AppendLine($"public enum {EnumName}");
        sb.AppendLine("{");

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);

            if (string.IsNullOrEmpty(layerName))
                continue;

            string sanitized = SanitizeIdentifier(layerName);
            sb.AppendLine($"    {sanitized} = {i},");
        }

        sb.AppendLine("}");

        string fullPath = Path.GetFullPath(OutputPath);
        string existing = File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
        string generated = sb.ToString();

        if (existing == generated)
            return;

        File.WriteAllText(fullPath, generated);
        AssetDatabase.ImportAsset(OutputPath);
        Debug.Log($"[LayerEnumGenerator] '{OutputPath}' updated.");
    }

    private static string SanitizeIdentifier(string name)
    {
        var result = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (i == 0 && char.IsDigit(c))
                result.Append('_');

            result.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return result.ToString();
    }
}

/// <summary>
/// Watches for changes to TagManager.asset and re-triggers enum generation.
/// </summary>
public class LayerEnumPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (path == "ProjectSettings/TagManager.asset")
            {
                LayerEnumGenerator.GenerateEnum();
                return;
            }
        }
    }
}
