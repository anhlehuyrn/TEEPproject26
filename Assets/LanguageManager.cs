using UnityEngine;
using TMPro;
using Fungus;

public class LanguageManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_Dropdown languageDropdown;
    
    [Header("Fungus")]
    public Localization fungusLocalization;

    private readonly string[] languageCodes = { "en", "zh-TW", "ml", "vi" };

    private void Start()
    {
        int savedLangIndex = PlayerPrefs.GetInt("AppLanguage", 0);

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.value = savedLangIndex;
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        // False: Khi mới mở App, chỉ đổi chữ, KHÔNG reset NPC
        ApplyLanguage(savedLangIndex, false);
    }

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("AppLanguage", index);
        PlayerPrefs.Save();
        
        // True: Khi user chủ động bấm đổi ngôn ngữ, ép NPC reset để nói lại
        ApplyLanguage(index, true);
    }

    private void ApplyLanguage(int index, bool forceResetNPC)
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
    }
}