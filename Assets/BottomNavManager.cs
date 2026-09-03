using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BottomNavManager : MonoBehaviour
{
    [Header("Các chữ trong thanh điều hướng")]
    public TextMeshProUGUI homeText;
    public TextMeshProUGUI exploreText;
    public TextMeshProUGUI passportText;

    [Header("Các Icon hình ảnh (Image)")]
    public Image homeIconImage;
    public Image exploreIconImage;
    public Image passportIconImage;

    [Header("Icon khi KHÔNG chọn (Normal)")]
    public Sprite homeNormalSprite;
    public Sprite exploreNormalSprite;
    public Sprite passportNormalSprite;

    [Header("Icon khi ĐƯỢC CHỌN (Active - _p)")]
    public Sprite homeActiveSprite;
    public Sprite exploreActiveSprite;
    public Sprite passportActiveSprite;

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

    [Header("Màu sắc Chữ")]
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

        // 2. Reset tất cả các Icon về Sprite Normal mặc định
        if (homeIconImage != null && homeNormalSprite != null) homeIconImage.sprite = homeNormalSprite;
        if (exploreIconImage != null && exploreNormalSprite != null) exploreIconImage.sprite = exploreNormalSprite;
        if (passportIconImage != null && passportNormalSprite != null) passportIconImage.sprite = passportNormalSprite;

        // 3. ẨN TẤT CẢ PANEL CHÍNH VÀ AVATAR
        if (homePanel != null) homePanel.SetActive(false);
        if (explorePanel != null) explorePanel.SetActive(false);
        if (passportPanel != null) passportPanel.SetActive(false);
        if (avatarHA != null) avatarHA.SetActive(false);

        // 4. Ẩn tất cả các trang Scroll Page để làm sạch màn hình
        if (scrollPageTaiwan != null) scrollPageTaiwan.SetActive(false);
        if (scrollPageVietnam != null) scrollPageVietnam.SetActive(false);
        if (scrollPageKerala != null) scrollPageKerala.SetActive(false);

        // 5. Bật đúng Panel và đổi Icon Active tương ứng
        switch (tabIndex)
        {
            case 0:
                if (homeText != null) homeText.color = activeColor;
                if (homeIconImage != null && homeActiveSprite != null) homeIconImage.sprite = homeActiveSprite;
                
                // --- BẢN VÁ LỖI XUNG ĐỘT DROPDOWN ---
                // Reset dropdown Location về index 0 ("All") trước khi bật HomePanel
                AppController appCtrl = FindObjectOfType<AppController>(true);
                if (appCtrl != null && appCtrl.locationDropdown != null)
                {
                    appCtrl.locationDropdown.value = 0;
                }
                
                if (homePanel != null) homePanel.SetActive(true);
                break;

            case 1:
                if (exploreText != null) exploreText.color = activeColor;
                if (exploreIconImage != null && exploreActiveSprite != null) exploreIconImage.sprite = exploreActiveSprite;
                if (explorePanel != null) explorePanel.SetActive(true);
                // Mở tab Explore thì bật lại Avatar để nó chào hỏi
                if (avatarHA != null) avatarHA.SetActive(true); 
                break;

            case 2:
                if (passportText != null) passportText.color = activeColor;
                if (passportIconImage != null && passportActiveSprite != null) passportIconImage.sprite = passportActiveSprite;
                if (passportPanel != null) passportPanel.SetActive(true);
                break;
        }
    }
}