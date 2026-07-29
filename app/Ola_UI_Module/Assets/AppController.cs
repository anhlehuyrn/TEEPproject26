using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AppController : MonoBehaviour
{
    private const string TraditionalChineseGeneratedFontPath = "OLA_Fonts/OLA_NotoSansTC_Language SDF";
    private const string MalayalamGeneratedFontPath = "OLA_Fonts/OLA_NotoSansMalayalam_Language SDF";

    [Header("Dropdown Selectors")]
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown locationDropdown;

    [Header("Scroll Pages")]
    public GameObject pageAll;
    public GameObject pageTaiwan;
    public GameObject pageKerala;
    public GameObject pageVietnam;

    [Header("Narrative Texts")]
    public TextMeshProUGUI narrativeTitle;
    public TextMeshProUGUI narrativeBody;

    private CardTextRefs keralaCard;
    private CardTextRefs taiwanCard;
    private CardTextRefs vietnamCard;
    private readonly Dictionary<TextMeshProUGUI, string> originalTextKeys = new Dictionary<TextMeshProUGUI, string>();
    private readonly Dictionary<TextMeshProUGUI, TMP_FontAsset> originalTextFonts = new Dictionary<TextMeshProUGUI, TMP_FontAsset>();

    [Header("Language Font Fallbacks")]
    public TMP_FontAsset traditionalChineseFont;
    public TMP_FontAsset malayalamFont;

    private static readonly string[] LanguageOptions =
    {
        "English",
        "\u7E41\u9AD4\u4E2D\u6587",
        "\u0D2E\u0D32\u0D2F\u0D3E\u0D33\u0D02",
        "Ti\u1EBFng Vi\u1EC7t"
    };

    private enum Language
    {
        English = 0,
        TraditionalChinese = 1,
        Malayalam = 2,
        Vietnamese = 3
    }

    private struct CardTextRefs
    {
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;
    }

    private void Start()
    {
        LoadGeneratedLanguageFonts();
        CacheOriginalTextKeys();
        CacheCardTexts();
        ConfigureFontFallbacks();
        ConfigureLanguageDropdown();
        ConfigureLocationDropdown(Language.English);

        if (locationDropdown != null)
        {
            locationDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
        }

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
        }

        UpdateUI();
    }

    private void LoadGeneratedLanguageFonts()
    {
        TMP_FontAsset generatedTraditionalChineseFont = Resources.Load<TMP_FontAsset>(TraditionalChineseGeneratedFontPath);
        if (generatedTraditionalChineseFont != null)
        {
            traditionalChineseFont = generatedTraditionalChineseFont;
        }
        else
        {
            Debug.LogWarning("OLA Traditional Chinese font is missing. In Unity, stop Play Mode and run OLA > Rebuild Language Fonts.");
        }

        TMP_FontAsset generatedMalayalamFont = Resources.Load<TMP_FontAsset>(MalayalamGeneratedFontPath);
        if (generatedMalayalamFont != null)
        {
            malayalamFont = generatedMalayalamFont;
        }
        else
        {
            Debug.LogWarning("OLA Malayalam font is missing. In Unity, stop Play Mode and run OLA > Rebuild Language Fonts.");
        }
    }

    private void ConfigureFontFallbacks()
    {
        TMP_FontAsset[] fallbackFonts = { traditionalChineseFont, malayalamFont };

        foreach (TextMeshProUGUI text in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            AddFallbacksToFont(text.font, fallbackFonts);
        }

        foreach (TMP_FontAsset fallbackFont in fallbackFonts)
        {
            if (fallbackFont == null || TMP_Settings.fallbackFontAssets.Contains(fallbackFont))
            {
                continue;
            }

            TMP_Settings.fallbackFontAssets.Add(fallbackFont);
        }
    }

    private static void AddFallbacksToFont(TMP_FontAsset font, IEnumerable<TMP_FontAsset> fallbackFonts)
    {
        if (font == null)
        {
            return;
        }

        foreach (TMP_FontAsset fallbackFont in fallbackFonts)
        {
            if (fallbackFont == null || font.fallbackFontAssetTable.Contains(fallbackFont))
            {
                continue;
            }

            font.fallbackFontAssetTable.Add(fallbackFont);
        }
    }

    public void UpdateUI()
    {
        int locIndex = locationDropdown != null ? locationDropdown.value : 0;
        Language language = GetSelectedLanguage();

        if (pageAll != null) pageAll.SetActive(locIndex == 0);
        if (pageTaiwan != null) pageTaiwan.SetActive(locIndex == 1);
        if (pageKerala != null) pageKerala.SetActive(locIndex == 2);
        if (pageVietnam != null) pageVietnam.SetActive(locIndex == 3);

        ApplyLanguageFont(language);
        ApplyGlobalTranslations(language);
        ConfigureLocationDropdown(language);
        if (languageDropdown != null) languageDropdown.RefreshShownValue();
        ApplyCardTexts(language);

        if (narrativeTitle == null || narrativeBody == null)
        {
            return;
        }

        ApplyNarrativeText(locIndex, language);
    }

    private void CacheOriginalTextKeys()
    {
        originalTextKeys.Clear();
        originalTextFonts.Clear();

        foreach (TextMeshProUGUI text in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            originalTextFonts[text] = text.font;

            if (languageDropdown != null && text.transform.IsChildOf(languageDropdown.transform))
            {
                continue;
            }

            if (locationDropdown != null && text.transform.IsChildOf(locationDropdown.transform))
            {
                continue;
            }

            originalTextKeys[text] = NormalizeText(text.text);
        }
    }

    private void ApplyGlobalTranslations(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);

        foreach (KeyValuePair<TextMeshProUGUI, string> entry in originalTextKeys)
        {
            if (entry.Key == null)
            {
                continue;
            }

            string translatedText = TranslateText(entry.Value, language);
            if (string.IsNullOrEmpty(translatedText))
            {
                continue;
            }

            entry.Key.text = translatedText;
            if (font != null && entry.Value != "OLA")
            {
                entry.Key.font = font;
            }
            else if (originalTextFonts.ContainsKey(entry.Key))
            {
                entry.Key.font = originalTextFonts[entry.Key];
            }

            entry.Key.ForceMeshUpdate();
        }
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string normalized = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    private void CacheCardTexts()
    {
        keralaCard = FindCardTextRefs("Card_Kerala");
        taiwanCard = FindCardTextRefs("Card_Taiwan");
        vietnamCard = FindCardTextRefs("Card_Vietnam");
    }

    private static CardTextRefs FindCardTextRefs(string cardName)
    {
        GameObject card = GameObject.Find(cardName);
        if (card == null)
        {
            return new CardTextRefs();
        }

        Transform title = FindChildRecursive(card.transform, "Title");
        Transform description = FindChildRecursive(card.transform, "Description");

        return new CardTextRefs
        {
            Title = title != null ? title.GetComponent<TextMeshProUGUI>() : null,
            Description = description != null ? description.GetComponent<TextMeshProUGUI>() : null
        };
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private void ApplyCardTexts(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);

        SetCardText(keralaCard, GetCardTitle("Kerala", language), GetCardDescription("Kerala", language), font);
        SetCardText(taiwanCard, GetCardTitle("Taiwan", language), GetCardDescription("Taiwan", language), font);
        SetCardText(vietnamCard, GetCardTitle("Vietnam", language), GetCardDescription("Vietnam", language), font);
    }

    private static void SetCardText(CardTextRefs card, string title, string description, TMP_FontAsset font)
    {
        if (card.Title != null)
        {
            card.Title.text = title;
            if (font != null) card.Title.font = font;
            card.Title.ForceMeshUpdate();
        }

        if (card.Description != null)
        {
            card.Description.text = description;
            if (font != null) card.Description.font = font;
            card.Description.ForceMeshUpdate();
        }
    }

    private void ConfigureLanguageDropdown()
    {
        if (languageDropdown == null)
        {
            return;
        }

        int selectedValue = Mathf.Clamp(languageDropdown.value, 0, LanguageOptions.Length - 1);
        languageDropdown.options.Clear();

        foreach (string option in LanguageOptions)
        {
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }

        languageDropdown.value = selectedValue;
        languageDropdown.RefreshShownValue();
    }

    private void ConfigureLocationDropdown(Language language)
    {
        if (locationDropdown == null)
        {
            return;
        }

        int selectedValue = locationDropdown.value;
        string[] options =
        {
            TranslateLocationOption("All", language),
            TranslateLocationOption("Taiwan", language),
            TranslateLocationOption("India (Kerala)", language),
            TranslateLocationOption("Vietnam (Bac Ninh)", language)
        };

        locationDropdown.options.Clear();
        foreach (string option in options)
        {
            locationDropdown.options.Add(new TMP_Dropdown.OptionData(option));
        }

        locationDropdown.value = Mathf.Clamp(selectedValue, 0, options.Length - 1);
        locationDropdown.RefreshShownValue();
    }

    private Language GetSelectedLanguage()
    {
        if (languageDropdown == null)
        {
            return Language.English;
        }

        int value = Mathf.Clamp(languageDropdown.value, 0, LanguageOptions.Length - 1);
        return (Language)value;
    }

    private void ApplyLanguageFont(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);
        if (font == null)
        {
            RestoreOriginalFont(narrativeTitle);
            RestoreOriginalFont(narrativeBody);

            if (languageDropdown != null)
            {
                foreach (TextMeshProUGUI text in languageDropdown.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    RestoreOriginalFont(text);
                }
            }

            return;
        }

        if (narrativeTitle != null) narrativeTitle.font = font;
        if (narrativeBody != null) narrativeBody.font = font;

        if (languageDropdown == null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in languageDropdown.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.font = font;
            AddFallbacksToFont(text.font, new[] { traditionalChineseFont, malayalamFont });
        }

        languageDropdown.RefreshShownValue();
    }

    private void RestoreOriginalFont(TextMeshProUGUI text)
    {
        if (text != null && originalTextFonts.ContainsKey(text))
        {
            text.font = originalTextFonts[text];
        }
    }

    private TMP_FontAsset GetFontForLanguage(Language language)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                return traditionalChineseFont;
            case Language.Malayalam:
                return malayalamFont;
            default:
                return null;
        }
    }

    private void ApplyNarrativeText(int locationIndex, Language language)
    {
        string title;
        string body;

        switch (locationIndex)
        {
            case 1:
                GetTaiwanText(language, out title, out body);
                break;
            case 2:
                GetKeralaText(language, out title, out body);
                break;
            case 3:
                GetVietnamText(language, out title, out body);
                break;
            default:
                GetAllText(language, out title, out body);
                break;
        }

        narrativeTitle.text = title;
        narrativeBody.text = body;
        narrativeTitle.ForceMeshUpdate();
        narrativeBody.ForceMeshUpdate();
    }

    private static void GetAllText(Language language, out string title, out string body)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                title = "\u4E09\u5730\u4E00\u65C5\u7A0B";
                body = "\u63A2\u7D22\u5580\u62C9\u62C9\u3001\u53F0\u7063\u8207\u8D8A\u5357\u4E4B\u9593\u7684\u6587\u5316\u9023\u7D50\uFF0C\u5C55\u958B\u4E00\u5834\u5171\u540C\u4EBA\u985E\u907A\u7522\u4E4B\u65C5\u3002";
                break;
            case Language.Malayalam:
                title = "\u0D2E\u0D42\u0D28\u0D4D\u0D28\u0D4D \u0D28\u0D3E\u0D1F\u0D41\u0D15\u0D7E, \u0D12\u0D30\u0D41 \u0D2F\u0D3E\u0D24\u0D4D\u0D30";
                body = "\u0D15\u0D47\u0D30\u0D33\u0D02, \u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B, \u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02 \u0D0E\u0D28\u0D4D\u0D28\u0D3F\u0D35\u0D2F\u0D41\u0D1F\u0D46 \u0D38\u0D3E\u0D02\u0D38\u0D4D\u0D15\u0D3E\u0D30\u0D3F\u0D15 \u0D2C\u0D28\u0D4D\u0D27\u0D02 \u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15.";
                break;
            case Language.Vietnamese:
                title = "Ba v\u00F9ng \u0111\u1EA5t, m\u1ED9t h\u00E0nh tr\u00ECnh";
                body = "Kh\u00E1m ph\u00E1 nh\u1EEFng s\u1EE3i d\u00E2y v\u0103n h\u00F3a n\u1ED1i li\u1EC1n Kerala, \u0110\u00E0i Loan v\u00E0 Vi\u1EC7t Nam.";
                break;
            default:
                title = "Three Worlds, One Journey";
                body = "Trace the subtle threads connecting the backwaters of Kerala, the resilient gorges of Taiwan, and the timeless waters of Vietnam.";
                break;
        }
    }

    private static string GetCardTitle(string place, Language language)
    {
        switch (place)
        {
            case "Kerala":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u5580\u62C9\u62C9";
                    case Language.Malayalam: return "\u0D15\u0D47\u0D30\u0D33\u0D02";
                    case Language.Vietnamese: return "Kerala";
                    default: return "Kerala";
                }
            case "Taiwan":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u53F0\u7063";
                    case Language.Malayalam: return "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B";
                    case Language.Vietnamese: return "\u0110\u00E0i Loan";
                    default: return "Taiwan";
                }
            case "Vietnam":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u8D8A\u5357";
                    case Language.Malayalam: return "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02";
                    case Language.Vietnamese: return "Vi\u1EC7t Nam";
                    default: return "Vietnam";
                }
            default:
                return place;
        }
    }

    private static string TranslateText(string key, Language language)
    {
        if (language == Language.English)
        {
            return key;
        }

        switch (key)
        {
            case "Home":
                return Pick(language, "\u9996\u9801", "\u0D39\u0D4B\u0D02", "Trang ch\u1EE7");
            case "Explore":
                return Pick(language, "\u63A2\u7D22", "\u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15", "Kh\u00E1m ph\u00E1");
            case "Passport":
                return Pick(language, "\u8B77\u7167", "\u0D2A\u0D3E\u0D38\u0D4D\u0D2A\u0D4B\u0D7C\u0D1F\u0D4D", "H\u1ED9 chi\u1EBFu");
            case "Ask AI":
                return Pick(language, "\u8A62\u554F AI", "AI-\u0D2F\u0D4B\u0D1F\u0D4D \u0D1A\u0D4B\u0D26\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15", "H\u1ECFi AI");
            case "CURRENT REGION":
                return Pick(language, "\u7576\u524D\u5730\u5340", "\u0D28\u0D3F\u0D32\u0D35\u0D3F\u0D32\u0D46 \u0D2A\u0D4D\u0D30\u0D26\u0D47\u0D36\u0D02", "KHU V\u1EF0C HI\u1EC6N T\u1EA0I");
            case "Journey Progress":
                return Pick(language, "\u65C5\u7A0B\u9032\u5EA6", "\u0D2F\u0D3E\u0D24\u0D4D\u0D30\u0D3E \u0D2A\u0D41\u0D30\u0D4B\u0D17\u0D24\u0D3F", "Ti\u1EBFn tr\u00ECnh h\u00E0nh tr\u00ECnh");
            case "Stamp Collection":
                return Pick(language, "\u96C6\u7AE0\u6536\u85CF", "\u0D38\u0D4D\u0D31\u0D4D\u0D31\u0D3E\u0D2E\u0D4D\u0D2A\u0D4D \u0D36\u0D47\u0D16\u0D30\u0D02", "B\u1ED9 s\u01B0u t\u1EADp con d\u1EA5u");
            case "Point camera at any pictures to discover.":
                return Pick(language, "\u5C07\u76F8\u6A5F\u5C0D\u6E96\u4EFB\u4F55\u5716\u7247\u4F86\u767C\u73FE\u5167\u5BB9\u3002", "\u0D15\u0D23\u0D4D\u0D1F\u0D46\u0D24\u0D4D\u0D24\u0D3E\u0D7B \u0D15\u0D4D\u0D2F\u0D3E\u0D2E\u0D31 \u0D1A\u0D3F\u0D24\u0D4D\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D3F\u0D32\u0D47\u0D15\u0D4D\u0D15\u0D4D \u0D1A\u0D42\u0D23\u0D4D\u0D1F\u0D41\u0D15.", "H\u01B0\u1EDBng camera v\u00E0o h\u00ECnh \u1EA3nh \u0111\u1EC3 kh\u00E1m ph\u00E1.");
            case "Explore Kerela":
            case "Explore Kerala":
                return Pick(language, "\u63A2\u7D22\u5580\u62C9\u62C9", "\u0D15\u0D47\u0D30\u0D33\u0D02 \u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15", "Kh\u00E1m ph\u00E1 Kerala");
            case "Explore Taiwan":
                return Pick(language, "\u63A2\u7D22\u53F0\u7063", "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B \u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15", "Kh\u00E1m ph\u00E1 \u0110\u00E0i Loan");
            case "Explore Vietnam":
                return Pick(language, "\u63A2\u7D22\u8D8A\u5357", "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02 \u0D05\u0D28\u0D4D\u0D35\u0D47\u0D37\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D15", "Kh\u00E1m ph\u00E1 Vi\u1EC7t Nam");
            case "Kerala":
                return Pick(language, "\u5580\u62C9\u62C9", "\u0D15\u0D47\u0D30\u0D33\u0D02", "Kerala");
            case "Taiwan":
                return Pick(language, "\u53F0\u7063", "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B", "\u0110\u00E0i Loan");
            case "Vietnam":
                return Pick(language, "\u8D8A\u5357", "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02", "Vi\u1EC7t Nam");
            case "Taiwan Lantern Festival":
                return Pick(language, "\u53F0\u7063\u71C8\u6703", "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B \u0D32\u0D3E\u0D28\u0D4D\u0D31\u0D47\u0D7A \u0D09\u0D24\u0D4D\u0D38\u0D35\u0D02", "L\u1EC5 h\u1ED9i \u0111\u00E8n l\u1ED3ng \u0110\u00E0i Loan");
            case "Taiwan Indigenous Traditional Clothing":
                return Pick(language, "\u53F0\u7063\u539F\u4F4F\u6C11\u50B3\u7D71\u670D\u98FE", "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B \u0D06\u0D26\u0D3F\u0D35\u0D3E\u0D38\u0D3F \u0D35\u0D38\u0D4D\u0D24\u0D4D\u0D30\u0D02", "Trang ph\u1EE5c truy\u1EC1n th\u1ED1ng b\u1EA3n \u0111\u1ECBa \u0110\u00E0i Loan");
            case "Dong Ky Festival":
                return Pick(language, "\u6771\u5947\u7BC0", "\u0D21\u0D4B\u0D19\u0D4D \u0D15\u0D3F \u0D09\u0D24\u0D4D\u0D38\u0D35\u0D02", "L\u1EC5 h\u1ED9i \u0110\u1ED3ng K\u1EF5");
            case "Phu The Cake":
                return Pick(language, "\u592B\u59BB\u9905", "\u0D2B\u0D41 \u0D25\u0D47 \u0D15\u0D47\u0D15\u0D4D\u0D15\u0D4D", "B\u00E1nh phu th\u00EA");
            case "Vietnamese Four-part Dress":
                return Pick(language, "\u8D8A\u5357\u56DB\u8EAB\u8863", "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D2E\u0D40\u0D38\u0D4D \u0D28\u0D3E\u0D32\u0D4D \u0D2D\u0D3E\u0D17 \u0D35\u0D38\u0D4D\u0D24\u0D4D\u0D30\u0D02", "\u00C1o t\u1EE9 th\u00E2n");
            case "Beef Noodle":
                return Pick(language, "\u725B\u8089\u9EB5", "\u0D2C\u0D40\u0D2B\u0D4D \u0D28\u0D42\u0D21\u0D3F\u0D7D", "M\u00EC b\u00F2");
            case "Option A":
                return Pick(language, "\u9078\u9805 A", "\u0D13\u0D2A\u0D4D\u0D37\u0D7B A", "T\u00F9y ch\u1ECDn A");
            case "66%":
                return "66%";
            default:
                return TranslateLongText(key, language);
        }
    }

    private static string TranslateLocationOption(string key, Language language)
    {
        if (language == Language.English)
        {
            return key;
        }

        switch (key)
        {
            case "All":
                return Pick(language, "\u5168\u90E8", "\u0D0E\u0D32\u0D4D\u0D32\u0D3E\u0D02", "T\u1EA5t c\u1EA3");
            case "Taiwan":
                return Pick(language, "\u53F0\u7063", "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B", "\u0110\u00E0i Loan");
            case "India (Kerala)":
                return Pick(language, "\u5370\u5EA6\uFF08\u5580\u62C9\u62C9\uFF09", "\u0D07\u0D28\u0D4D\u0D24\u0D4D\u0D2F (\u0D15\u0D47\u0D30\u0D33\u0D02)", "\u1EA4n \u0110\u1ED9 (Kerala)");
            case "Vietnam (Bac Ninh)":
                return Pick(language, "\u8D8A\u5357\uFF08\u5317\u5BE7\uFF09", "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02 (\u0D2C\u0D3E\u0D15\u0D4D \u0D28\u0D3F\u0D28\u0D4D\u0D39\u0D4D)", "Vi\u1EC7t Nam (B\u1EAFc Ninh)");
            default:
                return key;
        }
    }

    private static string TranslateLongText(string key, Language language)
    {
        if (key.StartsWith("Your exploration of the archival collections is well underway."))
        {
            return Pick(language, "\u4F60\u7684\u6A94\u6848\u63A2\u7D22\u5DF2\u7D93\u958B\u59CB\u3002\u7E7C\u7E8C\u5B8C\u6210\u65C5\u7A0B\u4F86\u6536\u96C6\u6240\u6709\u5370\u8A18\u3002", "\u0D06\u0D7C\u0D15\u0D4D\u0D15\u0D48\u0D35\u0D4D \u0D36\u0D47\u0D16\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D3F\u0D32\u0D46 \u0D28\u0D3F\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D1F\u0D46 \u0D2F\u0D3E\u0D24\u0D4D\u0D30 \u0D24\u0D41\u0D1F\u0D19\u0D4D\u0D19\u0D3F. \u0D38\u0D4D\u0D31\u0D4D\u0D31\u0D3E\u0D2E\u0D4D\u0D2A\u0D41\u0D15\u0D7E \u0D36\u0D47\u0D16\u0D30\u0D3F\u0D15\u0D4D\u0D15\u0D3E\u0D7B \u0D2F\u0D3E\u0D24\u0D4D\u0D30 \u0D2A\u0D42\u0D7C\u0D24\u0D4D\u0D24\u0D3F\u0D2F\u0D3E\u0D15\u0D4D\u0D15\u0D41\u0D15.", "H\u00E0nh tr\u00ECnh kh\u00E1m ph\u00E1 t\u01B0 li\u1EC7u \u0111ang ti\u1EBFn tri\u1EC3n. Ho\u00E0n th\u00E0nh \u0111\u1EC3 s\u01B0u t\u1EADp \u0111\u1EE7 con d\u1EA5u.");
        }

        if (key.StartsWith("A vibrant expression of heritage"))
        {
            return Pick(language, "\u8272\u5F69\u3001\u73E0\u98FE\u8207\u7D0B\u6A23\u7DE8\u7E54\u51FA\u9BAE\u6D3B\u7684\u6587\u5316\u50B3\u627F\u3002", "\u0D28\u0D3F\u0D31\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D2E\u0D23\u0D3F\u0D15\u0D33\u0D41\u0D02 \u0D2A\u0D3E\u0D31\u0D4D\u0D31\u0D47\u0D23\u0D41\u0D15\u0D33\u0D41\u0D02 \u0D1A\u0D47\u0D7C\u0D28\u0D4D\u0D28 \u0D2A\u0D48\u0D24\u0D43\u0D15 \u0D05\u0D35\u0D24\u0D30\u0D23\u0D02.", "M\u00E0u s\u1EAFc, h\u1EA1t c\u01B0\u1EDDm v\u00E0 hoa v\u0103n d\u1EC7t n\u00EAn di s\u1EA3n s\u1ED1ng \u0111\u1ED9ng.");
        }

        if (key.StartsWith("Glowing lights and cultural traditions"))
        {
            return Pick(language, "\u71C8\u706B\u8207\u50B3\u7D71\u6587\u5316\u7167\u4EAE\u591C\u665A\u3002", "\u0D24\u0D3F\u0D33\u0D19\u0D4D\u0D19\u0D41\u0D28\u0D4D\u0D28 \u0D35\u0D46\u0D33\u0D3F\u0D1A\u0D4D\u0D1A\u0D35\u0D41\u0D02 \u0D38\u0D02\u0D38\u0D4D\u0D15\u0D3E\u0D30\u0D35\u0D41\u0D02 \u0D30\u0D3E\u0D24\u0D4D\u0D30\u0D3F\u0D2F\u0D46 \u0D24\u0D46\u0D33\u0D3F\u0D2F\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28\u0D41.", "\u00C1nh \u0111\u00E8n v\u00E0 truy\u1EC1n th\u1ED1ng v\u0103n h\u00F3a th\u1EAFp s\u00E1ng m\u00E0n \u0111\u00EAm.");
        }

        if (key.StartsWith("A sweet Vietnamese wedding cake"))
        {
            return Pick(language, "\u8C61\u5FB5\u611B\u8207\u5FE0\u8AA0\u7684\u8D8A\u5357\u559C\u9905\u3002", "\u0D38\u0D4D\u0D28\u0D47\u0D39\u0D35\u0D41\u0D02 \u0D35\u0D3F\u0D36\u0D4D\u0D35\u0D38\u0D4D\u0D24\u0D24\u0D2F\u0D41\u0D02 \u0D38\u0D42\u0D1A\u0D3F\u0D2A\u0D4D\u0D2A\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28 \u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D2E\u0D40\u0D38\u0D4D \u0D35\u0D3F\u0D35\u0D3E\u0D39 \u0D15\u0D47\u0D15\u0D4D\u0D15\u0D4D.", "M\u00F3n b\u00E1nh c\u01B0\u1EDBi Vi\u1EC7t Nam t\u01B0\u1EE3ng tr\u01B0ng cho t\u00ECnh y\u00EAu v\u00E0 th\u1EE7y chung.");
        }

        if (key.StartsWith("A graceful four-panel dress"))
        {
            return Pick(language, "\u512A\u96C5\u7684\u56DB\u7247\u5F0F\u9577\u886B\uFF0C\u5C55\u73FE\u8D8A\u5357\u50B3\u7D71\u3002", "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D2E\u0D40\u0D38\u0D4D \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D02 \u0D15\u0D3E\u0D23\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28 \u0D28\u0D3E\u0D32\u0D4D \u0D2D\u0D3E\u0D17 \u0D35\u0D38\u0D4D\u0D24\u0D4D\u0D30\u0D02.", "Chi\u1EBFc \u00E1o t\u1EE9 th\u00E2n duy\u00EAn d\u00E1ng, th\u1EC3 hi\u1EC7n truy\u1EC1n th\u1ED1ng Vi\u1EC7t Nam.");
        }

        if (key.StartsWith("A savory symbol of tradition and flavor."))
        {
            return Pick(language, "\u50B3\u7D71\u8207\u98A8\u5473\u7684\u9E79\u9999\u8C61\u5FB5\u3002", "\u0D30\u0D41\u0D1A\u0D3F\u0D2F\u0D41\u0D02 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D35\u0D41\u0D02 \u0D1A\u0D47\u0D7C\u0D28\u0D4D\u0D28 \u0D1A\u0D3F\u0D39\u0D4D\u0D28\u0D02.", "Bi\u1EC3u t\u01B0\u1EE3ng \u0111\u1EADm \u0111\u00E0 c\u1EE7a truy\u1EC1n th\u1ED1ng v\u00E0 h\u01B0\u01A1ng v\u1ECB.");
        }

        if (key.StartsWith("A grand procession honoring the village"))
        {
            return Pick(language, "\u76DB\u5927\u904A\u884C\u4EE5\u9F13\u6A02\u3001\u796D\u5100\u8207\u793E\u7FA4\u8A18\u61B6\u79AE\u656C\u6751\u838A\u5B88\u8B77\u795E\u3002", "\u0D1A\u0D46\u0D23\u0D4D\u0D1F\u0D2E\u0D47\u0D33\u0D35\u0D41\u0D02 \u0D06\u0D1A\u0D3E\u0D30\u0D35\u0D41\u0D02 \u0D38\u0D2E\u0D42\u0D39 \u0D13\u0D7C\u0D2E\u0D2F\u0D41\u0D02 \u0D1A\u0D47\u0D7C\u0D28\u0D4D\u0D28 \u0D17\u0D4D\u0D30\u0D3E\u0D2E\u0D26\u0D47\u0D35\u0D24\u0D2F\u0D4D\u0D15\u0D4D\u0D15\u0D41\u0D33\u0D4D\u0D33 \u0D35\u0D32\u0D3F\u0D2F \u0D0A\u0D30\u0D4D\u0D35\u0D32\u0D02.", "M\u1ED9t \u0111o\u00E0n r\u01B0\u1EDBc l\u1EDBn t\u00F4n vinh th\u00E0nh ho\u00E0ng v\u1EDBi tr\u1ED1ng, nghi l\u1EC5 v\u00E0 k\u00FD \u1EE9c c\u1ED9ng \u0111\u1ED3ng.");
        }

        return key;
    }

    private static string Pick(Language language, string traditionalChinese, string malayalam, string vietnamese)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                return traditionalChinese;
            case Language.Malayalam:
                return malayalam;
            case Language.Vietnamese:
                return vietnamese;
            default:
                return string.Empty;
        }
    }

    private static string GetCardDescription(string place, Language language)
    {
        switch (place)
        {
            case "Kerala":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u4E0A\u5E1D\u7684\u570B\u5EA6\u3002\u5BE7\u975C\u6C34\u9109\u8207\u53E4\u8001\u50B3\u7D71\u3002";
                    case Language.Malayalam: return "\u0D26\u0D48\u0D35\u0D24\u0D4D\u0D24\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D28\u0D3E\u0D1F\u0D4D. \u0D36\u0D3E\u0D28\u0D4D\u0D24 \u0D15\u0D3E\u0D2F\u0D32\u0D41\u0D15\u0D33\u0D41\u0D02 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D35\u0D41\u0D02.";
                    case Language.Vietnamese: return "V\u00F9ng \u0111\u1EA5t c\u1EE7a th\u1EA7n linh, v\u1EDBi s\u00F4ng n\u01B0\u1EDBc y\u00EAn b\u00ECnh.";
                    default: return "God's Own Country. A serene network of backwaters and ancient traditions.";
                }
            case "Taiwan":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u798F\u723E\u6469\u6C99\u3002\u5C71\u8C37\u3001\u591C\u5E02\u8207\u6DF1\u539A\u6587\u5316\u3002";
                    case Language.Malayalam: return "\u0D2B\u0D4B\u0D7C\u0D2E\u0D4B\u0D38. \u0D2E\u0D32\u0D2F\u0D3F\u0D1F\u0D41\u0D15\u0D4D\u0D15\u0D41\u0D15\u0D33\u0D41\u0D02 \u0D28\u0D48\u0D31\u0D4D\u0D31\u0D4D \u0D2E\u0D3E\u0D7C\u0D15\u0D4D\u0D15\u0D31\u0D4D\u0D31\u0D41\u0D15\u0D33\u0D41\u0D02.";
                    case Language.Vietnamese: return "\u0110\u1EA3o Formosa, v\u1EDBi h\u1EBBm n\u00FAi, ch\u1EE3 \u0111\u00EAm v\u00E0 v\u0103n h\u00F3a.";
                    default: return "Ilha Formosa. A land of dramatic gorges, night markets, and deep culture.";
                }
            case "Vietnam":
                switch (language)
                {
                    case Language.TraditionalChinese: return "\u9A30\u98DB\u4E4B\u9F8D\u3002\u77F3\u7070\u5CA9\u5C71\u5F9E\u7FE0\u7DA0\u6C34\u57DF\u5347\u8D77\u3002";
                    case Language.Malayalam: return "\u0D09\u0D2F\u0D30\u0D41\u0D28\u0D4D\u0D28 \u0D28\u0D3E\u0D17\u0D02. \u0D1A\u0D41\u0D23\u0D4D\u0D23\u0D3E\u0D2E\u0D4D\u0D2A\u0D41\u0D15\u0D32\u0D4D\u0D32\u0D4D \u0D2E\u0D32\u0D15\u0D7E.";
                    case Language.Vietnamese: return "R\u1ED3ng bay l\u00EAn. N\u00FAi \u0111\u00E1 v\u00F4i v\u01B0\u01A1n t\u1EEB l\u00E0n n\u01B0\u1EDBc xanh.";
                    default: return "Ascending Dragon. Limestone karsts rising from jade waters.";
                }
            default:
                return string.Empty;
        }
    }

    private static void GetTaiwanText(Language language, out string title, out string body)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                title = "\u53F0\u7063\uFF1A\u9BAE\u6D3B\u50B3\u7D71";
                body = "\u63A2\u7D22\u90FD\u5E02\u4E2D\u7684\u5EDF\u5B87\u8207\u5C71\u8C37\uFF0C\u611F\u53D7\u9748\u6027\u6B77\u53F2\u8207\u73FE\u4EE3\u751F\u6D3B\u7684\u7D50\u5408\u3002";
                break;
            case Language.Malayalam:
                title = "\u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D7B: \u0D1C\u0D40\u0D35\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D19\u0D4D\u0D19\u0D7E";
                body = "\u0D28\u0D17\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D15\u0D4D\u0D37\u0D47\u0D24\u0D4D\u0D30\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D1A\u0D47\u0D30\u0D41\u0D28\u0D4D\u0D28 \u0D24\u0D3E\u0D2F\u0D4D\u200C\u0D35\u0D3E\u0D28\u0D3F\u0D32\u0D46 \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D02 \u0D15\u0D23\u0D4D\u0D1F\u0D46\u0D24\u0D4D\u0D24\u0D41\u0D15.";
                break;
            case Language.Vietnamese:
                title = "\u0110\u00E0i Loan: truy\u1EC1n th\u1ED1ng s\u1ED1ng \u0111\u1ED9ng";
                body = "Kh\u00E1m ph\u00E1 nh\u1EEFng ng\u00F4i \u0111\u1EC1n v\u00E0 h\u1EBBm n\u00FAi, n\u01A1i l\u1ECBch s\u1EED t\u00E2m linh h\u00F2a c\u00F9ng \u0111\u00F4 th\u1ECB hi\u1EC7n \u0111\u1EA1i.";
                break;
            default:
                title = "Taiwan: Living Traditions";
                body = "Discover temples nestled between skyscrapers, a testament to the blend of spiritual history and rapid urban development.";
                break;
        }
    }

    private static void GetKeralaText(Language language, out string title, out string body)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                title = "\u5580\u62C9\u62C9\uFF1A\u4E0A\u5E1D\u7684\u570B\u5EA6";
                body = "\u63A2\u7D22\u5BE7\u975C\u7684\u6C34\u9109\u8207\u9BAE\u660E\u50B3\u7D71\uFF0C\u611F\u53D7\u5370\u5EA6\u5357\u90E8\u7684\u6587\u5316\u98A8\u666F\u3002";
                break;
            case Language.Malayalam:
                title = "\u0D15\u0D47\u0D30\u0D33\u0D02: \u0D26\u0D48\u0D35\u0D24\u0D4D\u0D24\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D4D\u0D35\u0D28\u0D4D\u0D24\u0D02 \u0D28\u0D3E\u0D1F\u0D4D";
                body = "\u0D36\u0D3E\u0D28\u0D4D\u0D24\u0D2E\u0D3E\u0D2F \u0D15\u0D3E\u0D2F\u0D32\u0D41\u0D15\u0D33\u0D41\u0D02 \u0D28\u0D3F\u0D31\u0D1E\u0D4D\u0D1E \u0D2A\u0D3E\u0D30\u0D2E\u0D4D\u0D2A\u0D30\u0D4D\u0D2F\u0D19\u0D4D\u0D19\u0D33\u0D41\u0D02 \u0D15\u0D47\u0D30\u0D33\u0D24\u0D4D\u0D24\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D02\u0D38\u0D4D\u0D15\u0D3E\u0D30\u0D02 \u0D15\u0D3E\u0D23\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28\u0D41.";
                break;
            case Language.Vietnamese:
                title = "Kerala: v\u00F9ng \u0111\u1EA5t c\u1EE7a th\u1EA7n linh";
                body = "Kh\u00E1m ph\u00E1 v\u00F9ng s\u00F4ng n\u01B0\u1EDBc y\u00EAn b\u00ECnh v\u00E0 nh\u1EEFng truy\u1EC1n th\u1ED1ng r\u1EF1c r\u1EE1 c\u1EE7a mi\u1EC1n nam \u1EA4n \u0110\u1ED9.";
                break;
            default:
                title = "Kerala: God's Own Country";
                body = "Explore the serene network of backwaters and vibrant traditions that shaped southern India.";
                break;
        }
    }

    private static void GetVietnamText(Language language, out string title, out string body)
    {
        switch (language)
        {
            case Language.TraditionalChinese:
                title = "\u8D8A\u5357\uFF1A\u9A30\u98DB\u4E4B\u9F8D";
                body = "\u7A7F\u884C\u65BC\u58EF\u9E97\u7684\u77F3\u7070\u5CA9\u5C71\u8207\u6C11\u9593\u85DD\u8853\u4E4B\u9593\uFF0C\u611F\u53D7\u5C0D\u5B89\u7A69\u751F\u6D3B\u7684\u9858\u671B\u3002";
                break;
            case Language.Malayalam:
                title = "\u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D02: \u0D09\u0D2F\u0D30\u0D41\u0D28\u0D4D\u0D28 \u0D28\u0D3E\u0D17\u0D02";
                body = "\u0D1A\u0D41\u0D23\u0D4D\u0D23\u0D3E\u0D2E\u0D4D\u0D2A\u0D41\u0D15\u0D32\u0D4D\u0D32\u0D4D \u0D2E\u0D32\u0D15\u0D33\u0D41\u0D02 \u0D1C\u0D28\u0D15\u0D32\u0D2F\u0D41\u0D02 \u0D35\u0D3F\u0D2F\u0D31\u0D4D\u0D31\u0D4D\u0D28\u0D3E\u0D2E\u0D3F\u0D28\u0D4D\u0D31\u0D46 \u0D38\u0D2E\u0D3E\u0D27\u0D3E\u0D28 \u0D38\u0D4D\u0D35\u0D2A\u0D4D\u0D28\u0D02 \u0D15\u0D3E\u0D23\u0D3F\u0D15\u0D4D\u0D15\u0D41\u0D28\u0D4D\u0D28\u0D41.";
                break;
            case Language.Vietnamese:
                title = "Vi\u1EC7t Nam: r\u1ED3ng bay l\u00EAn";
                body = "\u0110i qua n\u00FAi \u0111\u00E1 v\u00F4i h\u00F9ng v\u0129 v\u00E0 ngh\u1EC7 thu\u1EADt d\u00E2n gian, n\u01A1i ph\u1EA3n chi\u1EBFu kh\u00E1t v\u1ECDng v\u1EC1 cu\u1ED9c s\u1ED1ng b\u00ECnh y\u00EAn.";
                break;
            default:
                title = "Vietnam: Ascending Dragon";
                body = "Navigate through majestic limestone karsts and centuries of folk art, reflecting the desire for a peaceful life.";
                break;
        }
    }
}
