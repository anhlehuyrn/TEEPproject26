using UnityEngine;
using TMPro;
using Fungus;
using System.Collections;

public class LanguageManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_Dropdown languageDropdown;
    
    [Header("Fungus")]
    public Localization fungusLocalization;

    private readonly string[] languageCodes = { "en", "zh-TW", "ml", "vi" };

    private IEnumerator Start()
    {
        // Chờ 1 khung hình để Fungus khởi tạo xong
        yield return null; 

        int savedLangIndex = PlayerPrefs.GetInt("AppLanguage", 0);

        if (languageDropdown != null)
        {
            // Bịt tai Dropdown -> Gán giá trị -> Mở lại tai (tránh gọi nhầm sự kiện)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            languageDropdown.value = savedLangIndex;
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        // Áp dụng ngôn ngữ (gọi trực tiếp hàm trong cùng file)
        ApplyLanguage(savedLangIndex, false);
    }

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("AppLanguage", index);
        PlayerPrefs.Save();
        
        ApplyLanguage(index, true);
    }

    // Đổi thành public để ARFungusDialogueTrigger có thể gọi
    public void ApplyLanguage(int index, bool forceResetNPC)
    {
        if (fungusLocalization != null && index >= 0 && index < languageCodes.Length)
        {
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

        GuideIntroController guideController = Object.FindFirstObjectByType<GuideIntroController>();
        if (guideController != null)
        {
            guideController.SetLanguage(index);
        }
    }

    // Hàm an toàn dành riêng cho việc đánh thức lại ngôn ngữ sau khi mất tracking
    public void RefreshCurrentLanguage()
    {
        int savedLangIndex = PlayerPrefs.GetInt("AppLanguage", 0);
        ApplyLanguage(savedLangIndex, false);
    }
}