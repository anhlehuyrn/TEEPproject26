using UnityEngine;
using TMPro;

public class BottomNavManager : MonoBehaviour
{
    [Header("Các chữ trong thanh điều hướng")]
    public TextMeshProUGUI homeText;
    public TextMeshProUGUI exploreText;
    public TextMeshProUGUI passportText;

    [Header("Các Panel chính")]
    public GameObject homePanel;
    public GameObject explorePanel;
    public GameObject passportPanel;

    [Header("Các trang cuộn (Scroll Pages)")]
    public GameObject scrollPageTaiwan;
    public GameObject scrollPageVietnam;
    public GameObject scrollPageKerala;

    [Header("Nhân vật Avatar 3D")]
    public GameObject avatarHA; 

    [Header("Màu sắc")]
    public Color activeColor = new Color(0.45f, 0.36f, 0f, 1f); 
    public Color inactiveColor = new Color(0.06f, 0.06f, 0.06f, 1f);

    void Start()
    {
        if (PlayerPrefs.GetInt("OpenExploreTab", 0) == 1)
        {
            PlayerPrefs.SetInt("OpenExploreTab", 0);
            PlayerPrefs.Save();
            SetActiveTab(1);
        }
        else
        {
            SetActiveTab(0);
        }
    }

    public void SetActiveTab(int tabIndex)
    {
        // 1. Tắt màu đèn của tất cả các chữ
        if (homeText != null) homeText.color = inactiveColor;
        if (exploreText != null) exploreText.color = inactiveColor;
        if (passportText != null) passportText.color = inactiveColor;

        // 2. ẨN TẤT CẢ PANEL CHÍNH VÀ AVATAR
        if (homePanel != null) homePanel.SetActive(false);
        if (explorePanel != null) explorePanel.SetActive(false);
        if (passportPanel != null) passportPanel.SetActive(false);
        if (avatarHA != null) avatarHA.SetActive(false);

        // 3. (BỔ SUNG QUAN TRỌNG) Ẩn tất cả các trang Scroll Page để làm sạch màn hình
        if (scrollPageTaiwan != null) scrollPageTaiwan.SetActive(false);
        if (scrollPageVietnam != null) scrollPageVietnam.SetActive(false);
        if (scrollPageKerala != null) scrollPageKerala.SetActive(false);

        // 4. Bật đúng Panel được yêu cầu
        switch (tabIndex)
        {
            case 0:
                if (homeText != null) homeText.color = activeColor;
                if (homePanel != null) homePanel.SetActive(true);
                break;
            case 1:
                if (exploreText != null) exploreText.color = activeColor;
                if (explorePanel != null) explorePanel.SetActive(true);
                // Mở tab Explore thì bật lại Avatar để nó chào hỏi
                if (avatarHA != null) avatarHA.SetActive(true); 
                break;
            case 2:
                if (passportText != null) passportText.color = activeColor;
                if (passportPanel != null) passportPanel.SetActive(true);
                break;
        }
    }
}