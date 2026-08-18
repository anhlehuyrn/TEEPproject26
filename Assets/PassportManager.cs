using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassportManager : MonoBehaviour
{
    [Header("User Profile")]
    public Image avatarImage;
    public Sprite[] avatarList; 
    public TMP_InputField nameInput; 

    [Header("Journey Progress")]
    public Image progressFillBar; 
    public TextMeshProUGUI progressPercentageText; 
    public TextMeshProUGUI stampsCollectedText; 
    public TextMeshProUGUI regionsExploredText; 

    [Header("Real Stamp Data Connection")]
    public string[] stampTargetNames;

    private void OnEnable()
    {
        LoadUserProfile();
        SyncPassportData();
    }

    public void SyncPassportData()
    {
        if (stampTargetNames == null || stampTargetNames.Length == 0) return;

        int unlockedCount = 0;
        int totalStamps = stampTargetNames.Length;
        HashSet<string> exploredRegions = new HashSet<string>();

        foreach (string targetName in stampTargetNames)
        {
            if (string.IsNullOrWhiteSpace(targetName)) continue;

            if (PlayerPrefs.GetInt("Stamp_" + targetName, 0) == 1)
            {
                unlockedCount++;

                // Accurate Region Calculation based on Prefix (VN, KL, TW, DongHo)
                if (targetName.StartsWith("VN_") || targetName == "DongHo")
                    exploredRegions.Add("Vietnam");
                else if (targetName.StartsWith("KL_"))
                    exploredRegions.Add("Kerala");
                else if (targetName.StartsWith("TW_"))
                    exploredRegions.Add("Taiwan");
            }
        }

        float progress = (float)unlockedCount / totalStamps;
        
        if (progressFillBar != null) progressFillBar.fillAmount = progress;
        if (progressPercentageText != null) progressPercentageText.text = Mathf.RoundToInt(progress * 100) + "%";
        if (stampsCollectedText != null) stampsCollectedText.text = unlockedCount.ToString();
        if (regionsExploredText != null) regionsExploredText.text = exploredRegions.Count.ToString();
    }

    private void LoadUserProfile()
    {
        if (nameInput != null)
        {
            nameInput.text = PlayerPrefs.GetString("UserName", "Cultural Explorer");
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(delegate { SaveName(); });
        }

        if (avatarImage != null && avatarList != null && avatarList.Length > 0)
        {
            int savedAvatarIndex = Mathf.Clamp(PlayerPrefs.GetInt("UserAvatar", 0), 0, avatarList.Length - 1);
            avatarImage.sprite = avatarList[savedAvatarIndex];
        }
    }

    public void SaveName()
    {
        if (nameInput != null)
        {
            PlayerPrefs.SetString("UserName", nameInput.text);
            PlayerPrefs.Save();
        }
    }

    public void CycleAvatar()
    {
        if (avatarList == null || avatarList.Length == 0 || avatarImage == null) return;

        int currentIndex = (PlayerPrefs.GetInt("UserAvatar", 0) + 1) % avatarList.Length;
        avatarImage.sprite = avatarList[currentIndex];
        PlayerPrefs.SetInt("UserAvatar", currentIndex);
        PlayerPrefs.Save();
    }
}