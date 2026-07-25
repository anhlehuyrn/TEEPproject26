using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassportManager : MonoBehaviour
{
    [Header("Journey Progress")]
    public Image progressCircle; // Kéo vòng tròn có Fill Method Radial 360 vào đây
    public TextMeshProUGUI progressText; // Chữ 66%
    public TextMeshProUGUI completedCountText; // Chữ số 2
    public TextMeshProUGUI remainingCountText; // Chữ số 1

    [Header("Stamp: Taiwan")]
    public GameObject taiwanLockedGroup; // Nhóm chứa ổ khóa
    public GameObject taiwanUnlockedGroup; // Nhóm chứa con tem
    public TextMeshProUGUI taiwanDateText; // Chữ hiển thị ngày

    [Header("Stamp: Kerala")]
    // (Làm tương tự cho Kerala và Vietnam)

    int totalCultures = 3;

    // Hàm OnEnable tự động chạy mỗi khi tab Passport được bật lên
    void OnEnable()
    {
        SyncPassportData();
    }

    public void SyncPassportData()
    {
        int unlockedCount = 0;

        // --- KIỂM TRA ĐÀI LOAN ---
        // Đọc dữ liệu xem đã scan chưa (mặc định là 0 nếu chưa bao giờ scan)
        if (PlayerPrefs.GetInt("Taiwan_Unlocked", 0) == 1)
        {
            unlockedCount++;
            taiwanLockedGroup.SetActive(false);
            taiwanUnlockedGroup.SetActive(true);
            taiwanDateText.text = "Archived " + PlayerPrefs.GetString("Taiwan_Date", "");
        }
        else
        {
            taiwanLockedGroup.SetActive(true);
            taiwanUnlockedGroup.SetActive(false);
        }

        // --- (Thêm code kiểm tra cho Kerala và Vietnam tương tự ở đây) ---

        // --- CẬP NHẬT VÒNG TRÒN TIẾN ĐỘ ---
        float progress = (float)unlockedCount / totalCultures;
        
        // Tự động gọt vòng tròn
        if (progressCircle != null) progressCircle.fillAmount = progress;
        
        // Cập nhật các con số
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100) + "%";
        if (completedCountText != null) completedCountText.text = unlockedCount.ToString();
        if (remainingCountText != null) remainingCountText.text = (totalCultures - unlockedCount).ToString();
    }
}