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
    
    [Header("Avatar Selection Popup")]
    public GameObject avatarPopupPanel; // Kéo thả Panel Popup vào đây

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
        if (avatarPopupPanel != null) avatarPopupPanel.SetActive(false); // Ẩn popup khi mới mở
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
                if (targetName.StartsWith("VN_") || targetName == "DongHo") exploredRegions.Add("Vietnam");
                else if (targetName.StartsWith("KL_")) exploredRegions.Add("Kerala");
                else if (targetName.StartsWith("TW_")) exploredRegions.Add("Taiwan");
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
            nameInput.onEndEdit.RemoveListener(OnNameEditEnded);
            nameInput.onEndEdit.AddListener(OnNameEditEnded);
        }

        if (avatarImage != null && avatarList != null && avatarList.Length > 0)
        {
            int savedAvatarIndex = Mathf.Clamp(PlayerPrefs.GetInt("UserAvatar", 0), 0, avatarList.Length - 1);
            avatarImage.sprite = avatarList[savedAvatarIndex];
        }
    }

    private void OnNameEditEnded(string newName) { SaveName(); }

    public void SaveName()
    {
        if (nameInput != null)
        {
            PlayerPrefs.SetString("UserName", nameInput.text);
            PlayerPrefs.Save();
        }
    }

    // --- CÁC HÀM MỚI ĐỂ QUẢN LÝ POPUP ---

    // 1. Mở bảng chọn Avatar
    public void OpenAvatarPopup()
    {
        if (avatarPopupPanel != null) avatarPopupPanel.SetActive(true);
    }

    // 2. Đóng bảng chọn Avatar
    public void CloseAvatarPopup()
    {
        if (avatarPopupPanel != null) avatarPopupPanel.SetActive(false);
    }

    // 3. Hàm gán cho 12 nút bấm con giáp
    public void SelectAvatar(int index)
    {
        if (avatarList == null || index < 0 || index >= avatarList.Length || avatarImage == null) return;

        // Đổi hình, lưu bộ nhớ và tự động đóng Popup
        avatarImage.sprite = avatarList[index];
        PlayerPrefs.SetInt("UserAvatar", index);
        PlayerPrefs.Save();
        CloseAvatarPopup();
    }
}