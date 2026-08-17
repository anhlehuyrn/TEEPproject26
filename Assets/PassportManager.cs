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
    [Tooltip("Điền tên 9 bức tranh vào đây (VD: DongHo, VN_cloth, KL_fest...) để hệ thống tự đếm")]
    public string[] stampTargetNames;

    private void OnEnable()
    {
        LoadUserProfile();
        SyncPassportData();
    }

    public void SyncPassportData()
    {
        int unlockedCount = 0;
        int totalStamps = stampTargetNames.Length;

        if (totalStamps == 0) return; // Tránh lỗi chia cho 0 nếu chưa nhập tên tranh

        // --- ĐỌC DỮ LIỆU THẬT TỪ PLAYERPREFS ---
        foreach (string targetName in stampTargetNames)
        {
            if (PlayerPrefs.GetInt("Stamp_" + targetName, 0) == 1)
            {
                unlockedCount++;
            }
        }

        // --- CẬP NHẬT TIẾN ĐỘ ---
        float progress = (float)unlockedCount / totalStamps;
        
        if (progressFillBar != null) progressFillBar.fillAmount = progress;
        if (progressPercentageText != null) progressPercentageText.text = Mathf.RoundToInt(progress * 100) + "%";
        
        // --- CẬP NHẬT THỐNG KÊ ---
        if (stampsCollectedText != null) stampsCollectedText.text = unlockedCount.ToString();

        // Giả lập tính Vùng (Region): Cứ có tem của vùng nào thì cộng vùng đó. 
        // Ở đây giả lập đơn giản: Cứ 3 tem là 1 vùng.
        int regions = Mathf.CeilToInt((float)unlockedCount / 3f);
        if (regionsExploredText != null) regionsExploredText.text = regions.ToString();
    }

    // --- XỬ LÝ AVATAR & TÊN (Giữ nguyên của bạn vì đã viết rất tốt) ---
    private void LoadUserProfile()
    {
        if (nameInput != null)
        {
            nameInput.text = PlayerPrefs.GetString("UserName", "E. Montgomery");
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(delegate { SaveName(); });
        }

        if (avatarImage != null && avatarList.Length > 0)
        {
            int savedAvatarIndex = PlayerPrefs.GetInt("UserAvatar", 0);
            savedAvatarIndex = Mathf.Clamp(savedAvatarIndex, 0, avatarList.Length - 1);
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

        int currentIndex = PlayerPrefs.GetInt("UserAvatar", 0);
        currentIndex++; 
        
        if (currentIndex >= avatarList.Length) currentIndex = 0; 

        avatarImage.sprite = avatarList[currentIndex];
        PlayerPrefs.SetInt("UserAvatar", currentIndex);
        PlayerPrefs.Save();
    }
}