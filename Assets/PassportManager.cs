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
        
        // Tự động đồng bộ ngôn ngữ cho toàn bộ tab Passport khi mở lên
        AppController app = FindObjectOfType<AppController>(true);
        if (app != null)
        {
            app.UpdateUI();
        }
    }

    public void SyncPassportData()
    {
        int unlockedCount = 0;

        // --- KIỂM TRA ĐÀI LOAN ---
        if (PlayerPrefs.GetInt("Taiwan_Unlocked", 0) == 1)
        {
            unlockedCount++;
            if (taiwanLockedGroup != null) taiwanLockedGroup.SetActive(false);
            if (taiwanUnlockedGroup != null) taiwanUnlockedGroup.SetActive(true);
            if (taiwanDateText != null) taiwanDateText.text = "Archived " + PlayerPrefs.GetString("Taiwan_Date", "");
        }
        else
        {
            if (taiwanLockedGroup != null) taiwanLockedGroup.SetActive(true);
            if (taiwanUnlockedGroup != null) taiwanUnlockedGroup.SetActive(false);
        }

        // --- CẬP NHẬT VÒNG TRÒN TIẾN ĐỘ ---
        float progress = (float)unlockedCount / totalCultures;
        
        if (progressCircle != null) progressCircle.fillAmount = progress;
        
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100) + "%";
        if (completedCountText != null) completedCountText.text = unlockedCount.ToString();
        if (remainingCountText != null) remainingCountText.text = (totalCultures - unlockedCount).ToString();
    }
}