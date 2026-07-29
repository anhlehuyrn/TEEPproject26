using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class OlaLanguageFontBuilder
{
    private const string FontFolder = "Assets/Resources/OLA_Fonts";
    private const string TraditionalChineseSourcePath = "Assets/TextMesh Pro/Font Asset fallback/NotoSansTC-VariableFont_wght.ttf";
    private const string MalayalamSourcePath = "Assets/TextMesh Pro/Font Asset fallback/NotoSansMalayalam-VariableFont_wdth,wght.ttf";
    private const string TraditionalChineseAssetPath = FontFolder + "/OLA_NotoSansTC_Language SDF.asset";
    private const string MalayalamAssetPath = FontFolder + "/OLA_NotoSansMalayalam_Language SDF.asset";

    static OlaLanguageFontBuilder()
    {
        EditorApplication.delayCall += RebuildMissingLanguageFonts;
    }

    [MenuItem("OLA/Rebuild Language Fonts")]
    public static void RebuildLanguageFonts()
    {
        RebuildLanguageFonts(true);
    }

    private static void RebuildMissingLanguageFonts()
    {
        if (Application.isPlaying)
        {
            return;
        }

        bool hasTraditionalChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TraditionalChineseAssetPath) != null;
        bool hasMalayalamFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MalayalamAssetPath) != null;

        if (hasTraditionalChineseFont && hasMalayalamFont)
        {
            return;
        }

        RebuildLanguageFonts(false);
    }

    private static void RebuildLanguageFonts(bool force)
    {
        if (TMP_Settings.instance == null)
        {
            Debug.LogError("Cannot rebuild OLA language fonts because TextMesh Pro essentials are not imported.");
            return;
        }

        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "OLA_Fonts");

        if (force || AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TraditionalChineseAssetPath) == null)
        {
            BuildFont(TraditionalChineseSourcePath, TraditionalChineseAssetPath, TraditionalChineseCharacters);
        }

        if (force || AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MalayalamAssetPath) == null)
        {
            BuildFont(MalayalamSourcePath, MalayalamAssetPath, MalayalamCharacters + BuildUnicodeRange('\u0D00', '\u0D7F'));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("OLA language fonts rebuilt. Enter Play Mode and choose Traditional Chinese or Malayalam again.");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void BuildFont(string sourcePath, string assetPath, string characters)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
        {
            Debug.LogError("Cannot find source font: " + sourcePath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null || File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
        if (fontAsset == null)
        {
            Debug.LogError("Could not create TextMesh Pro font asset from: " + sourcePath);
            return;
        }

        fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        string missingCharacters;
        fontAsset.TryAddCharacters(characters, out missingCharacters);

        EditorUtility.SetDirty(fontAsset);

        if (!string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning("Some characters were not found in " + sourcePath + ": " + missingCharacters);
        }
    }

    private static string BuildUnicodeRange(char start, char end)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (char character = start; character <= end; character++)
        {
            builder.Append(character);
        }

        return builder.ToString();
    }

    private const string TraditionalChineseCharacters =
        "\u7E41\u9AD4\u4E2D\u6587" +
        "\u4E09\u5730\u4E00\u65C5\u7A0B" +
        "\u63A2\u7D22\u5580\u62C9\u62C9\u3001\u53F0\u7063\u8207\u8D8A\u5357\u4E4B\u9593\u7684\u6587\u5316\u9023\u7D50\uFF0C\u5C55\u958B\u4E00\u5834\u5171\u540C\u4EBA\u985E\u907A\u7522\u4E4B\u65C5\u3002" +
        "\u53F0\u7063\uFF1A\u9BAE\u6D3B\u50B3\u7D71" +
        "\u90FD\u5E02\u4E2D\u7684\u5EDF\u5B87\u8207\u5C71\u8C37\u611F\u53D7\u9748\u6027\u6B77\u53F2\u8207\u73FE\u4EE3\u751F\u6D3B\u7684\u7D50\u5408" +
        "\u5580\u62C9\u62C9\uFF1A\u4E0A\u5E1D\u7684\u570B\u5EA6" +
        "\u5BE7\u975C\u6C34\u9109\u9BAE\u660E\u50B3\u7D71\u5370\u5EA6\u5357\u90E8\u98A8\u666F\u53E4\u8001" +
        "\u8D8A\u5357\uFF1A\u9A30\u98DB\u4E4B\u9F8D" +
        "\u7A7F\u884C\u65BC\u58EF\u9E97\u77F3\u7070\u5CA9\u5C71\u8207\u6C11\u9593\u85DD\u8853\u611F\u53D7\u5C0D\u5B89\u7A69\u751F\u6D3B\u7684\u9858\u671B\u798F\u723E\u6469\u6C99\u591C\u5E02\u6DF1\u539A\u7FE0\u7DA0\u6C34\u57DF\u5347\u8D77" +
        "\u9996\u9801\u63A2\u7D22\u8B77\u7167\u8A62\u554F\u7576\u524D\u5730\u5340\u65C5\u7A0B\u9032\u5EA6\u96C6\u7AE0\u6536\u85CF" +
        "\u5C07\u76F8\u6A5F\u5C0D\u6E96\u4EFB\u4F55\u5716\u7247\u4F86\u767C\u73FE\u5167\u5BB9\u71C8\u6703\u539F\u4F4F\u6C11\u670D\u98FE" +
        "\u6771\u5947\u7BC0\u592B\u59BB\u9905\u56DB\u8EAB\u8863\u725B\u8089\u9EB5\u9078\u9805\u6A94\u6848\u5DF2\u7D93\u958B\u59CB\u7E7C\u7E8C\u5B8C\u6210\u6536\u96C6\u6240\u6709\u5370\u8A18" +
        "\u8272\u5F69\u73E0\u98FE\u7D0B\u6A23\u7DE8\u7E54\u50B3\u627F\u71C8\u706B\u665A\u8C61\u5FB5\u611B\u5FE0\u8AA0\u559C\u512A\u96C5\u56DB\u7247\u5F0F\u9577\u886B\u9E79\u9999\u76DB\u5927\u904A\u884C\u9F13\u6A02\u796D\u5100\u793E\u7FA4\u8A18\u61B6\u79AE\u656C\u6751\u838A\u5B88\u8B77\u795E";

    private const string MalayalamCharacters =
        "\u0D2E\u0D32\u0D2F\u0D3E\u0D33\u0D02" +
        "\u0D2E\u0D42\u0D28\u0D4D\u0D28\u0D4D \u0D28\u0D3E\u0D1F\u0D41\u0D15\u0D7E, \u0D12\u0D30\u0D41 \u0D2F\u0D3E\u0D24\u0D4D\u0D30" +
        "\u0D15\u0D47\u0D30\u0D33\u0D02, \u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B, \u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02 \u0D0E\u0D28\u0D4D\u0D28\u0D3F\u0D35\u0D2F\u0D41\u0D1F\u0D46 \u0D38\u0D3E\u0D02\u0D38\u0D4D\u0D15\u0D3E\u0D30\u0D3F\u0D15 \u0D2C\u0D28\u0D4D\u0D27\u0D02 \u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15." +
        "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B: \u0D1C\u0D40\u0D35\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D19\u0D4D\u0D19\u0D7E" +
        "\u0D28\u0D17\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D15\u0D4D\u0D37\u0D47\u0D24\u0D4D\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D1A\u0D47\u0D30\u0D41\u0D28\u0D4D\u0D28 \u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D28\u0D3F\u0D32\u0D46 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D02 \u0D15\u0D23\u0D4D\u0D1F\u0D46\u0D24\u0D4D\u0D24\u0D41\u0D15." +
        "\u0D15\u0D47\u0D30\u0D33\u0D02: \u0D26\u0D48\u0D35\u0D24\u0D4D\u0D24\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D4D\u0D35\u0D28\u0D4D\u0D24\u0D02 \u0D28\u0D3E\u0D1F\u0D4D" +
        "\u0D36\u0D3E\u0D28\u0D4D\u0D24\u0D2E\u0D3E\u0D2F \u0D15\u0D3E\u0D2F\u0D32\u0D41\u0D15\u0D33\u0D41\u0D02 \u0D28\u0D3F\u0D31\u0D1E\u0D4D\u0D1E \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D15\u0D47\u0D30\u0D33\u0D24\u0D4D\u0D24\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D02\u0D38\u0D4D\u0D15\u0D3E\u0D30\u0D02 \u0D15\u0D3E\u0D23\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28\u0D41." +
        "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02: \u0D09\u0D2F\u0D30\u0D41\u0D28\u0D4D\u0D28 \u0D28\u0D3E\u0D17\u0D02" +
        "\u0D1A\u0D41\u0D23\u0D4D\u0D23\u0D3E\u0D2E\u0D4D\u0D2A\u0D41\u0D15\u0D32\u0D4D\u0D32\u0D4D \u0D2E\u0D32\u0D15\u0D33\u0D41\u0D02 \u0D1C\u0D28\u0D15\u0D32\u0D2F\u0D41\u0D02 \u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D2E\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D2E\u0D3E\u0D27\u0D3E\u0D28 \u0D38\u0D4D\u0D35\u0D2A\u0D4D\u0D28\u0D02 \u0D15\u0D3E\u0D23\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28\u0D41." +
        "\u0D2B\u0D4B\u0D7C\u0D2E\u0D4B\u0D38 \u0D28\u0D48\u0D31\u0D4D\u0D31\u0D4D \u0D2E\u0D3E\u0D7C\u0D15\u0D4D\u0D15\u0D31\u0D4D\u0D31\u0D41\u0D15\u0D7E";
}
