using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Structure for JSON translation deserialization
[System.Serializable]
public class TranslationItem
{
    public string key;
    public string en;
    public string zh;
    public string ml;
    public string vi;
}

[System.Serializable]
public class TranslationData
{
    public List<TranslationItem> items;
}

public class AppController : MonoBehaviour
{
    private const string TraditionalChineseGeneratedFontPath = "OLA_Fonts/OLA_NotoSansTC_Language SDF";
    private const string MalayalamGeneratedFontPath = "OLA_Fonts/OLA_NotoSansMalayalam_Language SDF";

    [Header("Localization Settings")]
    public TextAsset jsonFile; // JSON file assigned in Inspector (or auto-loaded from Resources)
    private readonly Dictionary<string, TranslationItem> dictionary = new Dictionary<string, TranslationItem>();

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

    public enum Language
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

    private void Awake()
    {
        AutoFindUnassignedReferences();
    }

    private void OnEnable()
    {
        AutoFindUnassignedReferences();
        LoadJSON();
        LoadGeneratedLanguageFonts();
        ConfigureLanguageDropdown();
        UpdateUI();
    }

    private void Start()
    {
        AutoFindUnassignedReferences();
        LoadJSON();
        LoadGeneratedLanguageFonts();
        CacheOriginalTextKeys();
        CacheCardTexts();
        ConfigureFontFallbacks();
        ConfigureLanguageDropdown();
        ConfigureLocationDropdown(GetSelectedLanguage());

        if (locationDropdown != null)
        {
            locationDropdown.onValueChanged.RemoveAllListeners();
            locationDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
        }

        if (languageDropdown != null)
        {
            // Chỉ tháo/lắp đúng "nhân viên" của mình, không đụng chạm ai khác
            languageDropdown.onValueChanged.RemoveListener(OnDropdownLanguageChanged);
            languageDropdown.onValueChanged.AddListener(OnDropdownLanguageChanged);
        }
    }

    private void AutoFindUnassignedReferences()
    {
        if (languageDropdown == null)
        {
            GameObject obj = GameObject.Find("LanguageDropdown") ?? GameObject.Find("Dropdown_Language");
            if (obj != null) languageDropdown = obj.GetComponent<TMP_Dropdown>();
            if (languageDropdown == null) languageDropdown = FindObjectOfType<TMP_Dropdown>(true);
        }

        if (locationDropdown == null)
        {
            GameObject obj = GameObject.Find("LocationDropdown") ?? GameObject.Find("Dropdown_Location");
            if (obj != null) locationDropdown = obj.GetComponent<TMP_Dropdown>();
        }

        if (pageAll == null) pageAll = GameObject.Find("Page_All");
        if (pageTaiwan == null) pageTaiwan = GameObject.Find("Page_Taiwan");
        if (pageKerala == null) pageKerala = GameObject.Find("Page_Kerala");
        if (pageVietnam == null) pageVietnam = GameObject.Find("Page_Vietnam");

        if (narrativeTitle == null)
        {
            GameObject obj = GameObject.Find("NarrativeTitle") ?? GameObject.Find("Text_NarrativeTitle");
            if (obj != null) narrativeTitle = obj.GetComponent<TextMeshProUGUI>();
        }

        if (narrativeBody == null)
        {
            GameObject obj = GameObject.Find("NarrativeBody") ?? GameObject.Find("Text_NarrativeBody");
            if (obj != null) narrativeBody = obj.GetComponent<TextMeshProUGUI>();
        }
    }

    private void LoadJSON()
    {
        if (dictionary.Count > 0) return;

        if (jsonFile == null)
        {
            jsonFile = Resources.Load<TextAsset>("localization");
        }

        if (jsonFile != null)
        {
            TranslationData data = JsonUtility.FromJson<TranslationData>(jsonFile.text);
            if (data != null && data.items != null)
            {
                foreach (var item in data.items)
                {
                    if (string.IsNullOrEmpty(item.key)) continue;
                    
                    dictionary[item.key] = item;
                    string norm = NormalizeText(item.key);
                    if (!dictionary.ContainsKey(norm))
                    {
                        dictionary[norm] = item;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("localization.json not found in jsonFile or Resources/localization!");
        }
    }

    private void LoadGeneratedLanguageFonts()
    {
        if (traditionalChineseFont == null)
        {
            traditionalChineseFont = Resources.Load<TMP_FontAsset>(TraditionalChineseGeneratedFontPath);
        }
        
        if (malayalamFont == null)
        {
            malayalamFont = Resources.Load<TMP_FontAsset>(MalayalamGeneratedFontPath);
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
            if (fallbackFont == null || TMP_Settings.fallbackFontAssets.Contains(fallbackFont)) continue;
            TMP_Settings.fallbackFontAssets.Add(fallbackFont);
        }
    }

    private static void AddFallbacksToFont(TMP_FontAsset font, IEnumerable<TMP_FontAsset> fallbackFonts)
    {
        if (font == null) return;
        foreach (TMP_FontAsset fallbackFont in fallbackFonts)
        {
            if (fallbackFont == null || font.fallbackFontAssetTable.Contains(fallbackFont)) continue;
            font.fallbackFontAssetTable.Add(fallbackFont);
        }
    }

    public void UpdateUI()
    {
        AutoFindUnassignedReferences();
        int locIndex = locationDropdown != null ? locationDropdown.value : 0;
        Language language = GetSelectedLanguage();

        if (pageAll != null) pageAll.SetActive(locIndex == 0);
        if (pageTaiwan != null) pageTaiwan.SetActive(locIndex == 1);
        if (pageKerala != null) pageKerala.SetActive(locIndex == 2);
        if (pageVietnam != null) pageVietnam.SetActive(locIndex == 3);

        ApplyLanguageFont(language);
        CacheOriginalTextKeys();
        ApplyGlobalTranslations(language);
        ConfigureLocationDropdown(language);
        if (languageDropdown != null) languageDropdown.RefreshShownValue();
        ApplyCardTexts(language);

        if (narrativeTitle != null && narrativeBody != null)
        {
            ApplyNarrativeText(locIndex, language);
        }
    }

    private void CacheOriginalTextKeys()
    {
        foreach (TextMeshProUGUI text in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if (text == null) continue;

            if (!originalTextFonts.ContainsKey(text))
            {
                originalTextFonts[text] = text.font;
            }

            if (languageDropdown != null && text.transform.IsChildOf(languageDropdown.transform)) continue;
            if (locationDropdown != null && text.transform.IsChildOf(locationDropdown.transform)) continue;

            if (!originalTextKeys.ContainsKey(text))
            {
                originalTextKeys[text] = NormalizeText(text.text);
            }
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

    private void CacheCardTexts()
    {
        keralaCard = FindCardTextRefs("Card_Kerala");
        taiwanCard = FindCardTextRefs("Card_Taiwan");
        vietnamCard = FindCardTextRefs("Card_Vietnam");
    }

    private static CardTextRefs FindCardTextRefs(string cardName)
    {
        GameObject card = GameObject.Find(cardName);
        if (card == null) return new CardTextRefs();

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
            if (child.name == childName) return child;
            Transform match = FindChildRecursive(child, childName);
            if (match != null) return match;
        }
        return null;
    }

    private void ApplyCardTexts(Language language)
    {
        TMP_FontAsset font = GetFontForLanguage(language);

        SetCardText(keralaCard, TranslateText("Kerala", language), TranslateText("Kerala, God’s Own Country, is a land of tranquil backwaters, lush tea plantations, and ancient temples, where natural beauty and vibrant traditions blend seamlessly.", language), font);
        SetCardText(taiwanCard, TranslateText("Taiwan", language), TranslateText("Taiwan, Ilha Formosa, is a land of dramatic gorges, bustling night markets, and deep cultural traditions, where breathtaking natural landscapes meet vibrant urban life.", language), font);
        SetCardText(vietnamCard, TranslateText("Vietnam", language), TranslateText("Vietnam is a country of striking limestone karsts, jade-green waters, and vibrant cultural heritage, where breathtaking natural landscapes harmonize with bustling cities and timeless traditions.", language), font);
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
        if (languageDropdown == null) return;

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
        int savedValue = PlayerPrefs.GetInt("AppLanguage", 0);
        if (languageDropdown != null)
        {
            savedValue = languageDropdown.value;
        }
        return (Language)Mathf.Clamp(savedValue, 0, LanguageOptions.Length - 1);
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

    public string TranslateText(string key, Language language)
    {
        if (language == Language.English || string.IsNullOrEmpty(key)) return key;

        // 1. Check dictionary (exact key)
        if (dictionary.TryGetValue(key, out TranslationItem item))
        {
            string val = GetItemTranslation(item, language);
            if (!string.IsNullOrEmpty(val)) return val;
        }

        // 2. Check dictionary (normalized key)
        string normKey = NormalizeText(key);
        if (dictionary.TryGetValue(normKey, out item))
        {
            string val = GetItemTranslation(item, language);
            if (!string.IsNullOrEmpty(val)) return val;
        }

        // 3. Built-in C# Fallback dictionary (guarantees text translation even if JSON is not attached)
        return GetBuiltInFallback(normKey, language, key);
    }

    private static string GetItemTranslation(TranslationItem item, Language language)
    {
        switch (language)
        {
            case Language.TraditionalChinese: return item.zh;
            case Language.Malayalam: return item.ml;
            case Language.Vietnamese: return item.vi;
            default: return item.en;
        }
    }

    private static string GetBuiltInFallback(string normKey, Language language, string originalKey)
    {
        switch (normKey)
        {
            case "Home":
                return Pick(language, "首頁", "ഹോം", "Trang chủ");
            case "Explore":
                return Pick(language, "探索", "അന്വേഷിക്കുക", "Khám phá");
            case "Passport":
                return Pick(language, "護照", "പാസ്പോർട്ട്", "Hộ chiếu");
            case "Ask AI":
                return Pick(language, "詢問 AI", "AI-യോട് ചോദിക്കുക", "Hỏi AI");
            case "CURRENT REGION":
                return Pick(language, "當前地區", "നിലവിലെ പ്രദേശം", "KHU VỰC HIỆN TẠI");
            case "Journey Progress":
                return Pick(language, "旅程進度", "യാത്രാ പുരോഗതി", "Tiến trình hành trình");
            case "Stamp Collection":
                return Pick(language, "集章收藏", "സ്റ്റാമ്പ് ശേഖരം", "Bộ sưu tập con dấu");
            case "Point camera at any pictures to discover.":
                return Pick(language, "將相機對準任何圖片來發現內容。", "കണ്ടെത്താൻ ക്യാമറ ചിത്രങ്ങളിലേക്ക് ചൂണ്ടുക.", "Hướng camera vào hình ảnh để khám phá.");
            
            case "Kerala":
                return Pick(language, "喀拉拉邦", "കേരളം", "Kerala");
            case "Taiwan":
                return Pick(language, "台灣", "തായ്‌വാൻ", "Đài Loan");
            case "Vietnam":
                return Pick(language, "越南", "വിയറ്റ്നാം", "Việt Nam");
            case "All":
                return Pick(language, "全部", "എല്ലാം", "Tất cả");
            case "India (Kerala)":
                return Pick(language, "印度（喀拉拉邦）", "ഇന്ത്യ (കേരളം)", "Ấn Độ (Kerala)");
            case "Vietnam (Bac Ninh)":
                return Pick(language, "越南（北寧）", "വിയറ്റ്നാം (ബാക് നിൻഹ്)", "Việt Nam (Bắc Ninh)");

            case "Explore Kerala":
            case "Explore Kerela":
                return Pick(language, "探索喀拉拉", "കേരളം അന്വേഷിക്കുക", "Khám phá Kerala");
            case "Explore Taiwan":
                return Pick(language, "探索台灣", "തായ്‌വാൻ അന്വേഷിക്കുക", "Khám phá Đài Loan");
            case "Explore Vietnam":
                return Pick(language, "探索越南", "വിയറ്റ്നാം അന്വേഷിക്കുക", "Khám phá Việt Nam");

            case "Taiwan Lantern Festival":
                return Pick(language, "台灣燈會", "തായ്‌വാൻ ലാൻ്റേൺ ഉത്സവം", "Lễ hội đèn lồng Đài Loan");
            case "Taiwan Indigenous Traditional Clothing":
                return Pick(language, "台灣原住民傳統服飾", "തായ്‌വാൻ ആദിവാസി വസ്ത്രം", "Trang phục truyền thống bản địa Đài Loan");
            case "Dong Ky Festival":
                return Pick(language, "東奇節", "ഡോങ് കി ഉത്സവം", "Lễ hội Đồng Kỵ");
            case "Phu The Cake":
                return Pick(language, "夫妻餅", "ഫു തേ കേക്ക്", "Bánh phu thê");
            case "Vietnamese Four-part Dress":
                return Pick(language, "越南四身衣", "വിയറ്റ്നാമീസ് നാൽ ഭാഗ വസ്ത്രം", "Áo tứ thân");
            case "Beef Noodle":
                return Pick(language, "牛肉麵", "ബീഫ് നൂഡിൽ", "Mì bò");

            case "Three Worlds, One Journey":
                return Pick(language, "三個世界，一段旅程", "മൂന്ന് ലോകങ്ങൾ, ഒരു യാത്ര", "Ba Thế Giới, Một Hành Trình");
            case "Taiwan: Living Traditions":
                return Pick(language, "台灣：活著的傳統", "തായ്‌വാൻ: ജീവിക്കുന്ന പാരമ്പര്യങ്ങൾ", "Đài Loan: Truyền Thống Sống Động");
            case "Kerala: God's Own Country":
            case "Kerala: God’s Own Country":
                return Pick(language, "喀拉拉邦：上帝的國度", "കേരളം: ദൈവത്തിന്റെ സ്വന്തം നാട്", "Kerala: Vùng Đất Của Thượng Đế");
            case "Vietnam: Ascending Dragon":
                return Pick(language, "越南：昇龍之地", "വിയറ്റ്നാം: ഉയരുന്ന ഡ്രാഗൺ", "Việt Nam: Rồng Bay Lên");

            default:
                if (normKey.StartsWith("Your exploration of the archival collections is well underway"))
                {
                    return Pick(language,
                        "你的檔案探索已經開始。繼續完成旅程來收集所有印記。",
                        "ആർക്കൈവ് ശേഖരങ്ങളിലെ നിങ്ങളുടെ യാത്ര തുടങ്ങി. സ്റ്റാമ്പുകൾ ശേഖരിക്കാൻ യാത്ര പൂർത്തിയാക്കുക.",
                        "Hành trình khám phá tư liệu đang tiến triển. Hoàn thành để sưu tập đủ con dấu.");
                }
                if (normKey.StartsWith("A savory symbol of tradition and flavor"))
                {
                    return Pick(language,
                        "傳統與風味的咸香象徵。",
                        "രുചിയും പാരമ്പര്യവും ചേർന്ന ചിഹ്നം.",
                        "Biểu tượng đậm đà của truyền thống và hương vị.");
                }
                if (normKey.StartsWith("Glowing lights and cultural traditions"))
                {
                    return Pick(language,
                        "燈火與傳統文化照亮夜晚。",
                        "തിളങ്ങുന്ന വെളിച്ചവും സംസ്കാരവും രാത്രിയെ തെളിയിക്കുന്നു.",
                        "Ánh đèn và truyền thống văn hóa thắp sáng màn đêm.");
                }
                if (normKey.StartsWith("A vibrant expression of heritage"))
                {
                    return Pick(language,
                        "色彩、珠飾與紋樣編織出鮮活的文化傳承。",
                        "നിറങ്ങളും മണികളും പാറ്റേണുകളും ചേർന്ന പൈതൃക അവതരണം.",
                        "Màu sắc, hạt cườm và hoa văn dệt nên di sản sống động.");
                }
                if (normKey.StartsWith("A sweet Vietnamese wedding cake"))
                {
                    return Pick(language,
                        "象徵愛與忠誠的越南喜餅。",
                        "സ്നേഹവും വിശ്വസ്തതയും സൂചിപ്പിക്കുന്ന വിയറ്റ്നാമീസ് വിവാഹ കേക്ക്.",
                        "Món bánh cưới Việt Nam tượng trưng cho tình yêu và thủy chung.");
                }
                if (normKey.StartsWith("A graceful four-panel dress"))
                {
                    return Pick(language,
                        "優雅的四片式長衫，展現越南傳統。",
                        "വിയറ്റ്നാമീസ് പാരമ്പര്യം കാണിക്കുന്ന നാല് ഭാഗ വസ്ത്രം.",
                        "Chiếc áo tứ thân duyên dáng, thể hiện truyền thống Việt Nam.");
                }
                if (normKey.StartsWith("A grand procession honoring the village"))
                {
                    return Pick(language,
                        "盛大遊行以鼓樂、祭儀與社群記憶禮敬村莊守護神。",
                        "ചെണ്ടമേളവും ആചാരവും സമൂഹ ഓർമയും ചേർന്ന വലിയ ഊർവലം.",
                        "Một đoàn rước lớn tôn vinh thành hoàng với trống, nghi lễ và ký ức cộng đồng.");
                }
                if (normKey.StartsWith("Kerala, God") || normKey.StartsWith("Kerala,"))
                {
                    return Pick(language, 
                        "喀拉拉邦，上帝的國度，是一片擁有寧靜迴水、翠綠茶園和古老寺廟的土地，這裡的自然美景與充滿活力的傳統完美交融。",
                        "ദൈവത്തിന്റെ സ്വന്തം നാടായ കേരളം, ശാന്തമായ കായലുകളും സമൃദ്ധമായ തേയിലത്തോട്ടങ്ങളും പുരാതന ക്ഷേത്രങ്ങളുമുള്ള ഒരു നാടാണ്, ഇവിടെ പ്രകൃതിസൗന്ദര്യവും സജീവമായ പാരമ്പര്യങ്ങളും തടസ്സങ്ങളില്ലാതെ ലയിക്കുന്നു.",
                        "Kerala, Vùng Đất Của Thượng Đế, là xứ sở của những vùng sông nước thanh bình, những đồi chè xanh mướt và các ngôi đền cổ kính, nơi vẻ đẹp thiên nhiên và truyền thống rực rỡ hòa quyện liền mạch.");
                }
                if (normKey.StartsWith("Taiwan, Ilha Formosa") || normKey.StartsWith("Taiwan,"))
                {
                    return Pick(language, 
                        "台灣，福爾摩沙，這片土地擁有壯麗的峽谷、熙熙攘攘的夜市和深厚的文化傳統，令人驚嘆的自然景觀與充滿活力的都市生活在此交會。",
                        "മനോഹരമായ ദ്വീപായ തായ്‌വാൻ, അതിശയകരമായ മലയിടുക്കുകളുടെയും രാത്രി വിപണികളുടെയും നാടാണ്.",
                        "Đài Loan, hòn đảo Formosa xinh đẹp, là vùng đất của những hẻm núi hùng vĩ, những khu chợ đêm sầm uất và truyền thống văn hóa lâu đời, nơi cảnh quan thiên nhiên ngoạn mục giao thoa cùng nhịp sống đô thị sôi động.");
                }
                if (normKey.StartsWith("Vietnam is a country") || normKey.StartsWith("Vietnam is"))
                {
                    return Pick(language, 
                        "越南是一個擁有引人注目的石灰岩喀斯特地貌、翠綠水域和充滿活力的文化遺產的國家，令人驚嘆的自然景觀與繁華的城市和永恆的傳統在此和諧共存。",
                        "ശ്രദ്ധേയമായ ചുണ്ണാമ്പുകല്ല് പർവതങ്ങളുടെയും മരതകപ്പച്ച വെള്ളത്തിന്റെയും രാജ്യമാണ് വിയറ്റ്നാം.",
                        "Việt Nam là quốc gia của những dãy núi đá vôi nổi bật, làn nước xanh ngọc bích và di sản văn hóa rực rỡ, nơi cảnh quan thiên nhiên ngoạn mục hài hòa với những thành phố nhộn nhịp và truyền thống vượt thời gian.");
                }
                if (normKey.StartsWith("Trace the subtle threads"))
                {
                    return Pick(language,
                        "探索喀拉拉邦的迴水、台灣的堅韌峽谷與越南的永恆水域之間的微妙聯繫。一場對人類共同遺產的精心探索。",
                        "കേരളത്തിലെ കായലുകളും തായ്‌വാനിലെ മലയിടുക്കുകളും വിയറ്റ്നാമിലെ ജലാശയങ്ങളും തമ്മിലുള്ള ബന്ധം കണ്ടെത്തുക.",
                        "Khám phá sự giao thoa văn hóa giữa Kerala, Đài Loan và Việt Nam. Một hành trình lưu giữ di sản nhân loại.");
                }
                return originalKey;
        }
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

    private void OnDropdownLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("AppLanguage", index);
        PlayerPrefs.SetInt("SelectedLanguageIndex", index);
        PlayerPrefs.Save();
        UpdateUI(); 
    }
}