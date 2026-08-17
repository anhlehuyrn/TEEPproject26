using UnityEngine;
using UnityEngine.UI;
using System;

public class StampManager : MonoBehaviour
{
    [Serializable]
    public class Stamp
    {
        [Tooltip("Tên hình ảnh (Phải giống hệt lúc quét, VD: DongHo, VN_cloth)")]
        public string targetName; 
        
        [Tooltip("Vật thể chứa hình cái tem trên UI")]
        public GameObject stampObject; 
    }

    [Header("Danh sách 9 cái tem")]
    public Stamp[] stamps;

    // Hàm OnEnable tự động chạy mỗi khi trang Passport được bật lên
    private void OnEnable()
    {
        RefreshStamps();
    }

    public void RefreshStamps()
    {
        foreach (Stamp stamp in stamps)
        {
            // Đọc dữ liệu xem tem này đã được quét chưa (Mặc định là 0 - Chưa quét)
            bool isCollected = PlayerPrefs.GetInt("Stamp_" + stamp.targetName, 0) == 1;

            if (stamp.stampObject != null)
            {
                // Cách an toàn để ẩn/hiện mà không làm hỏng lưới GridLayout:
                // Tắt component Image thay vì tắt cả GameObject
                Image stampImage = stamp.stampObject.GetComponent<Image>();
                if (stampImage != null)
                {
                    stampImage.enabled = isCollected;
                }
                else
                {
                    stamp.stampObject.SetActive(isCollected);
                }
            }
        }
    }

    // Nút dùng để test: Xóa hết tem làm lại từ đầu
    public void ResetAllStampsForTesting()
    {
        foreach (Stamp stamp in stamps)
        {
            PlayerPrefs.SetInt("Stamp_" + stamp.targetName, 0);
        }
        PlayerPrefs.Save();
        RefreshStamps();
        Debug.Log("Đã xóa toàn bộ tem!");
    }
}