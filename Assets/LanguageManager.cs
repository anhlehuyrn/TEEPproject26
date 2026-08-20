using UnityEngine;
using TMPro;
using Fungus;
using System.Collections; // Dùng để gọi lệnh chờ (Coroutine)

public class LanguageManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_Dropdown languageDropdown;
    
    [Header("Fungus")]
    public Localization fungusLocalization;

    private readonly string[] languageCodes = { "en", "zh-TW", "ml", "vi" };

    // Đổi thành IEnumerator Start để dùng lệnh chờ
    private IEnumerator Start()
    {
        // Bí quyết ở đây: Chờ đúng 1 khung hình cho Fungus thức dậy và reset xong
        // Sau đó chúng ta mới áp đặt ngôn ngữ lên, đảm bảo không bị ghi đè!
        yield return null; 

        int savedLangIndex = PlayerPrefs.GetInt("AppLanguage", 0);

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            languageDropdown.value = savedLangIndex;
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        // Áp dụng ngôn ngữ lưu trong sổ tay
        ApplyLanguage(savedLangIndex, false);
    }

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("AppLanguage", index);
        PlayerPrefs.Save();
        
        ApplyLanguage(index, true);
    }

    private void ApplyLanguage(int index, bool forceResetNPC)
    {
        if (fungusLocalization != null && index >= 0 && index < languageCodes.Length)
        {
            // Chỉ dùng hàm SetActiveLanguage được phép của Fungus
            fungusLocalization.SetActiveLanguage(languageCodes[index]);

            if (forceResetNPC)
            {
                ARFungusDialogueTrigger arTrigger = Object.FindFirstObjectByType<ARFungusDialogueTrigger>();
                if (arTrigger != null)
                {
                    arTrigger.ResetAllDialogues(); 
                }
            }
        }
    }
}