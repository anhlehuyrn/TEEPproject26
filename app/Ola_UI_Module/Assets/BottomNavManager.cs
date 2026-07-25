using UnityEngine;
using TMPro; // Thư viện để chỉnh màu chữ TMP

public class BottomNavManager : MonoBehaviour
{
    [Header("Các chữ trong thanh điều hướng")]
    public TextMeshProUGUI homeText;
    public TextMeshProUGUI exploreText;
    public TextMeshProUGUI passportText;

    [Header("Màu sắc")]
    // Đã thiết lập sẵn màu Nâu vàng (Sáng) và Đen xám (Tối) theo thiết kế của bạn
    public Color activeColor = new Color(0.45f, 0.36f, 0f, 1f); 
    public Color inactiveColor = new Color(0.06f, 0.06f, 0.06f, 1f);

    void Start()
    {
        // Khi mới bật App lên, mặc định chữ Home sẽ sáng
        SetActiveTab(0);
    }

    // Hàm này sẽ được gọi khi bạn bấm vào các nút
    public void SetActiveTab(int tabIndex)
    {
        // 1. Tắt đèn toàn bộ các chữ trước
        if (homeText != null) homeText.color = inactiveColor;
        if (exploreText != null) exploreText.color = inactiveColor;
        if (passportText != null) passportText.color = inactiveColor;

        // 2. Thắp sáng chữ tương ứng với tab đang mở (0 = Home, 1 = Explore, 2 = Passport)
        switch (tabIndex)
        {
            case 0:
                if (homeText != null) homeText.color = activeColor;
                break;
            case 1:
                if (exploreText != null) exploreText.color = activeColor;
                break;
            case 2:
                if (passportText != null) passportText.color = activeColor;
                break;
        }
    }
}