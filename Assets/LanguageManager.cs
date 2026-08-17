using UnityEngine;
using TMPro;
using Fungus;

public class LanguageManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_Dropdown languageDropdown;
    
    [Header("Fungus")]
    public Localization fungusLocalization;

    // Mã ngôn ngữ tương ứng với 4 tùy chọn trong Dropdown của bạn
    // 0: Tiếng Anh, 1: Tiếng Trung phồn thể, 2: Tiếng Malayalam, 3: Tiếng Việt
    private readonly string[] languageCodes = { "en", "zh-TW", "ml", "vi" };

    private void Start()
    {
        // Đọc ngôn ngữ đã lưu từ cuốn sổ tay PlayerPrefs (mặc định là 0 - Tiếng Anh)
        int savedLangIndex = PlayerPrefs.GetInt("AppLanguage", 0);

        // Cập nhật lại UI Dropdown cho đúng với ngôn ngữ đã lưu
        if (languageDropdown != null)
        {
            // Tạm thời tắt lắng nghe sự kiện để tránh lỗi vòng lặp khi gán value
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.value = savedLangIndex;
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        // Áp dụng ngôn ngữ vào Fungus
        ApplyLanguage(savedLangIndex);
    }

    public void OnLanguageChanged(int index)
    {
        // Lưu lựa chọn mới vào sổ tay
        PlayerPrefs.SetInt("AppLanguage", index);
        PlayerPrefs.Save();
        
        // Đổi ngôn ngữ ngay lập tức
        ApplyLanguage(index);
    }

    private void ApplyLanguage(int index)
    {
        if (fungusLocalization != null && index >= 0 && index < languageCodes.Length)
        {
            fungusLocalization.SetActiveLanguage(languageCodes[index]);
            Debug.Log("Đã đổi ngôn ngữ Fungus sang: " + languageCodes[index]);
        }
    }
}