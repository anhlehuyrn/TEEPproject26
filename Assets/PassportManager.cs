using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassportManager : MonoBehaviour
{
    [Header("User Profile")]
    public Image avatarImage;
    public Sprite[] avatarList; // Kéo thả các ảnh avatar có sẵn vào đây
    public TMP_InputField nameInput; // Ô để người dùng nhập tên

    [Header("Journey Progress")]
    public Image progressFillBar; // Kéo thanh ngang màu vàng (đã set Fill Horizontal) vào đây
    public TextMeshProUGUI progressPercentageText; // Chữ 66%
    public TextMeshProUGUI stampsCollectedText; // Số lượng tem
    public TextMeshProUGUI regionsExploredText; // Số lượng vùng

    [Header("Stamp Status (Demo)")]
    // Giả lập 9 con tem. Bạn có thể tự tick vào ô trong Inspector để test tiến độ
    public bool[] unlockedStamps = new bool[9];

    private const int TOTAL_STAMPS = 9;

    private void OnEnable()
    {
        LoadUserProfile();
        SyncPassportData();
    }

    public void SyncPassportData()
    {
        int unlockedCount = 0;

        // Đếm số lượng tem đã thu thập
        foreach (bool isUnlocked in unlockedStamps)
        {
            if (isUnlocked) unlockedCount++;
        }

        // --- CẬP NHẬT TIẾN ĐỘ ---
        float progress = (float)unlockedCount / TOTAL_STAMPS;
        
        if (progressFillBar != null) progressFillBar.fillAmount = progress;
        if (progressPercentageText != null) progressPercentageText.text = Mathf.RoundToInt(progress * 100) + "%";
        
        // --- CẬP NHẬT THỐNG KÊ ---
        if (stampsCollectedText != null) stampsCollectedText.text = unlockedCount.ToString();

        // Giả lập tính Vùng (Region): Cứ có tem của vùng nào thì cộng vùng đó. 
        // Ở đây giả lập đơn giản: Cứ 3 tem là 1 vùng.
        int regions = Mathf.CeilToInt((float)unlockedCount / 3);
        if (regionsExploredText != null) regionsExploredText.text = regions.ToString();
    }

    // --- XỬ LÝ AVATAR & TÊN ---
    private void LoadUserProfile()
    {
        // Tải Tên
        if (nameInput != null)
        {
            nameInput.text = PlayerPrefs.GetString("UserName", "E. Montgomery");
            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(delegate { SaveName(); });
        }

        // Tải Avatar
        if (avatarImage != null && avatarList.Length > 0)
        {
            int savedAvatarIndex = PlayerPrefs.GetInt("UserAvatar", 0);
            // Đảm bảo index không bị vượt quá giới hạn mảng
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

    // Gắn hàm này vào sự kiện OnClick() của nút Avatar
    public void CycleAvatar()
    {
        if (avatarList == null || avatarList.Length == 0 || avatarImage == null) return;

        int currentIndex = PlayerPrefs.GetInt("UserAvatar", 0);
        currentIndex++; // Chuyển sang ảnh tiếp theo
        
        if (currentIndex >= avatarList.Length) currentIndex = 0; // Quay lại ảnh đầu nếu hết danh sách

        avatarImage.sprite = avatarList[currentIndex];
        PlayerPrefs.SetInt("UserAvatar", currentIndex);
        PlayerPrefs.Save();
    }
}