using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AIContextSwitcher : MonoBehaviour
{
    private const string TraditionalChineseGeneratedFontPath = "OLA_Fonts/OLA_NotoSansTC_Language SDF";
    private const string MalayalamGeneratedFontPath = "OLA_Fonts/OLA_NotoSansMalayalam_Language SDF";

    [Header("Localization JSON")]
    public TextAsset jsonFile; // Lát nữa bạn kéo file JSON vào ô này trên Unity nhé!
    private Dictionary<string, TranslationItem> dictionary = new Dictionary<string, TranslationItem>();

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

    private readonly Dictionary<TextMeshProUGUI, string> originalTextKeys = new Dictionary<TextMeshProUGUI, string>();
    private readonly Dictionary<TextMeshProUGUI, TMP_FontAsset> originalTextFonts = new Dictionary<TextMeshProUGUI, TMP_FontAsset>();

    [Header("Language Font Fallbacks")]
    public TMP_FontAsset traditionalChineseFont;
    public TMP_FontAsset malayalamFont;

    private static readonly string[] LanguageOptions = { "English", "\u7E41\u9AD4\u4E2D\u6587", "\u0D2E\u0D32\u0D2F\u0D3E\u0D33\u0D02", "Ti\u1EBFng Vi\u1EC7t" };

    private enum Language { English = 0, TraditionalChinese = 1, Malayalam = 2, Vietnamese = 3 }

    private void Start()
    {
        LoadJSON(); // Tự động đọc file JSON
        LoadGeneratedLanguageFonts();
        CacheOriginalTextKeys(); // Tự thu thập toàn bộ Text trên màn hình
        ConfigureFontFallbacks();
        
        ConfigureLanguageDropdown();
        ConfigureLocationDropdown(GetSelectedLanguage());

        if (locationDropdown != null)
        {
            locationDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
        }

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(delegate { 
                // Ghi nhớ ngôn ngữ đã chọn để đồng bộ các trang
                PlayerPrefs.SetInt("AppLanguage", languageDropdown.value);
                PlayerPrefs.Save();
                UpdateUI(); 
            });
        }

        UpdateUI();
    }

    private void LoadJSON()
    {
        if (jsonFile != null)
        {
            TranslationData data = JsonUtility.FromJson<TranslationData>(jsonFile.text);
            foreach (var item in data.items)
            {
                dictionary[item.key] = item;
            }
        }
        else
        {
            Debug.LogWarning("Chưa gắn file JSON vào AppController!");
        }
    }

    private void LoadGeneratedLanguageFonts()
    {
        TMP_FontAsset generatedTraditionalChineseFont = Resources.Load<TMP_FontAsset>(TraditionalChineseGeneratedFontPath);
        if (generatedTraditionalChineseFont != null) traditionalChineseFont = generatedTraditionalChineseFont;
        
        TMP_FontAsset generatedMalayalamFont = Resources.Load<TMP_FontAsset>(MalayalamGeneratedFontPath);
        if (generatedMalayalamFont != null) malayalamFont = generatedMalayalamFont;
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
            if (fallbackFont != null && !TMP_Settings.fallbackFontAssets.Contains(fallbackFont))
                TMP_Settings.fallbackFontAssets.Add(fallbackFont);
        }
    }

    private static void AddFallbacksToFont(TMP_FontAsset font, IEnumerable<TMP_FontAsset> fallbackFonts)
    {
        if (font == null) return;
        foreach (TMP_FontAsset fallbackFont in fallbackFonts)
        {
            if (fallbackFont != null && !font.fallbackFontAssetTable.Contains(fallbackFont))
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
        ApplyGlobalTranslations(language); // Lệnh này dịch toàn bộ Card tự động!
        ConfigureLocationDropdown(language);
        
        if (languageDropdown != null) languageDropdown.RefreshShownValue();
        if (narrativeTitle != null && narrativeBody != null) ApplyNarrativeText(locIndex, language);
    }

    private void CacheOriginalTextKeys()
    {
        originalTextKeys.Clear();
        originalTextFonts.Clear();

        foreach (TextMeshProUGUI text in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            originalTextFonts[text] = text.font;
            if (languageDropdown != null && text.transform.IsChildOf(languageDropdown.transform)) continue;
            if (locationDropdown != null && text.transform.IsChildOf(locationDropdown.transform)) continue;

            originalTextKeys[text] = NormalizeText(text.text);
        }
    }

    private void ApplyGlobalTranslations(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);

        foreach (KeyValuePair<TextMeshProUGUI, string> entry in originalTextKeys)
        {
            if (entry.Key == null) continue;

            string translatedText = TranslateText(entry.Value, language);
            if (string.IsNullOrEmpty(translatedText)) continue;

            entry.Key.text = translatedText;
            if (font != null && entry.Value != "OLA") entry.Key.font = font;
            else if (originalTextFonts.ContainsKey(entry.Key)) entry.Key.font = originalTextFonts[entry.Key];
            
            entry.Key.ForceMeshUpdate();
        }
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string normalized = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        while (normalized.Contains("  ")) normalized = normalized.Replace("  ", " ");
        return normalized;
    }

    private string TranslateText(string key, Language language)
    {
        if (language == Language.English) return key;

        // Tra cứu vào cuốn từ điển JSON
        if (dictionary.ContainsKey(key))
        {
            var item = dictionary[key];
            switch (language)
            {
                case Language.TraditionalChinese: return item.zh;
                case Language.Malayalam: return item.ml;
                case Language.Vietnamese: return item.vi;
            }
        }
        return key; 
    }

    private void ConfigureLanguageDropdown()
    {
        if (languageDropdown == null) return;

        // Đọc ngôn ngữ từ bộ nhớ thiết bị để Đồng Bộ giữa các màn hình
        int savedValue = PlayerPrefs.GetInt("AppLanguage", 0);
        int selectedValue = Mathf.Clamp(savedValue, 0, LanguageOptions.Length - 1);
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
        if (locationDropdown == null) return;

        int selectedValue = locationDropdown.value;
        string[] options = {
            TranslateText("All", language),
            TranslateText("Taiwan", language),
            TranslateText("India (Kerala)", language),
            TranslateText("Vietnam (Bac Ninh)", language)
        };

        locationDropdown.options.Clear();
        foreach (string option in options) locationDropdown.options.Add(new TMP_Dropdown.OptionData(option));

        locationDropdown.value = Mathf.Clamp(selectedValue, 0, options.Length - 1);
        locationDropdown.RefreshShownValue();
    }

    private Language GetSelectedLanguage()
    {
        if (languageDropdown == null) return Language.English;
        return (Language)Mathf.Clamp(languageDropdown.value, 0, LanguageOptions.Length - 1);
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
                    RestoreOriginalFont(text);
            }
            return;
        }

        if (narrativeTitle != null) narrativeTitle.font = font;
        if (narrativeBody != null) narrativeBody.font = font;

        if (languageDropdown != null)
        {
            foreach (TextMeshProUGUI text in languageDropdown.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = font;
                AddFallbacksToFont(text.font, new[] { traditionalChineseFont, malayalamFont });
            }
            languageDropdown.RefreshShownValue();
        }
    }

    private void RestoreOriginalFont(TextMeshProUGUI text)
    {
        if (text != null && originalTextFonts.ContainsKey(text)) text.font = originalTextFonts[text];
    }

    private TMP_FontAsset GetFontForLanguage(Language language)
    {
        switch (language)
        {
            case Language.TraditionalChinese: return traditionalChineseFont;
            case Language.Malayalam: return malayalamFont;
            default: return null;
        }
    }

    private void ApplyNarrativeText(int locationIndex, Language language)
    {
        string titleKey = "Three Worlds, One Journey";
        string bodyKey = "Trace the subtle threads connecting the backwaters of Kerala, the resilient gorges of Taiwan, and the timeless waters of Vietnam. A curated exploration of shared human heritage.";

        switch (locationIndex)
        {
            case 1:
                titleKey = "Taiwan: Living Traditions";
                bodyKey = "Taiwan, Ilha Formosa, is a land of dramatic gorges, bustling night markets, and deep cultural traditions, where breathtaking natural landscapes meet vibrant urban life.";
                break;
            case 2:
                titleKey = "Kerala: God's Own Country";
                bodyKey = "Kerala, God’s Own Country, is a land of tranquil backwaters, lush tea plantations, and ancient temples, where natural beauty and vibrant traditions blend seamlessly.";
                break;
            case 3:
                titleKey = "Vietnam: Ascending Dragon";
                bodyKey = "Vietnam is a country of striking limestone karsts, jade-green waters, and vibrant cultural heritage, where breathtaking natural landscapes harmonize with bustling cities and timeless traditions.";
                break;
        }

        narrativeTitle.text = TranslateText(titleKey, language);
        narrativeBody.text = TranslateText(bodyKey, language);
        narrativeTitle.ForceMeshUpdate();
        narrativeBody.ForceMeshUpdate();
    }
}