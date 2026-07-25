using UnityEngine;
using TMPro;

public class AppController : MonoBehaviour
{
    [Header("Dropdown Selectors")]
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown locationDropdown;

    [Header("Scroll Pages (Giao diện Tạp chí)")]
    public GameObject pageAll;       // Kéo Home_Panel vào đây
    public GameObject pageTaiwan;    // Kéo ScrollPage_Taiwan vào đây
    public GameObject pageKerala;    // Kéo ScrollPage_Kerala vào đây
    public GameObject pageVietnam;   // Kéo ScrollPage_Vietnam vào đây

    [Header("Narrative Texts (Chữ trên Hero Banner)")]
    public TextMeshProUGUI narrativeTitle;
    public TextMeshProUGUI narrativeBody;

    void Start()
    {
        // Gọi chung 1 hàm UpdateUI cho mọi sự thay đổi để tránh xung đột logic
        if (locationDropdown != null)
            locationDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
            
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
        
        // Khởi chạy UI lần đầu tiên
        UpdateUI();
    }

    public void UpdateUI()
    {
        int locIndex = locationDropdown.value;
        int langIndex = languageDropdown.value;

        // --- BƯỚC 1: ĐIỀU KHIỂN TRANG CUỘN (MAGAZINE PAGES) ---
        // Chỉ bật duy nhất trang được chọn, tắt các trang còn lại
        if (pageAll != null) pageAll.SetActive(locIndex == 0);
        if (pageTaiwan != null) pageTaiwan.SetActive(locIndex == 1);
        if (pageKerala != null) pageKerala.SetActive(locIndex == 2);
        if (pageVietnam != null) pageVietnam.SetActive(locIndex == 3);


        // --- BƯỚC 2: CẬP NHẬT CHỮ THEO NGÔN NGỮ ---
        // langIndex -> 0: EN | 1: ZH-TW (Đài Loan) | 2: ML (Malayalam) | 3: VI (Tiếng Việt)
        
        if (narrativeTitle == null || narrativeBody == null) return; // Tránh lỗi vặt nếu quên kéo thả Text

        if (locIndex == 0) // Trạng thái: TẤT CẢ (ALL)
        {
            if (langIndex == 1) {
                narrativeTitle.text = "三個世界，一段旅程";
                narrativeBody.text = "探索喀拉拉邦的迴水、台灣的堅韌峽谷與越南的永恆水域之間的微妙聯繫。一場對人類共同遺產的精心探索。";
            } else if (langIndex == 2) {
                narrativeTitle.text = "മൂന്ന് ലോകങ്ങൾ, ഒരു യാത്ര";
                narrativeBody.text = "കേരളത്തിലെ കായലുകളും തായ്‌വാനിലെ മലയിടുക്കുകളും വിയറ്റ്നാമിലെ ജലാശയങ്ങളും തമ്മിലുള്ള ബന്ധം കണ്ടെത്തുക. മനുഷ്യ പൈതൃകത്തിന്റെ ഒരു അന്വേഷണം.";
            } else if (langIndex == 3) {
                narrativeTitle.text = "Ba Thế Giới, Một Hành Trình";
                narrativeBody.text = "Khám phá sự giao thoa văn hóa giữa Kerala, Đài Loan và Việt Nam. Một hành trình lưu giữ di sản nhân loại.";
            } else {
                narrativeTitle.text = "Three Worlds, One Journey";
                narrativeBody.text = "Trace the subtle threads connecting the backwaters of Kerala, the resilient gorges of Taiwan, and the timeless waters of Vietnam. A curated exploration of shared human heritage.";
            }
        }
        else if (locIndex == 1) // Trạng thái: TAIWAN
        {
            if (langIndex == 1) {
                narrativeTitle.text = "台灣：活著的傳統";
                narrativeBody.text = "探索隱藏在摩天大樓之間的寺廟，這是精神歷史與快速城市發展完美融合的證明。";
            } else if (langIndex == 2) {
                narrativeTitle.text = "തായ്‌വാൻ: ജീവിക്കുന്ന പാരമ്പര്യങ്ങൾ";
                narrativeBody.text = "ആകാശചുംബികളായ കെട്ടിടങ്ങൾക്കിടയിൽ സ്ഥിതിചെയ്യുന്ന ക്ഷേത്രങ്ങൾ കണ്ടെത്തുക.";
            } else if (langIndex == 3) {
                narrativeTitle.text = "Đài Loan: Truyền Thống Sống Động";
                narrativeBody.text = "Khám phá những ngôi đền ẩn mình giữa các tòa nhà chọc trời, minh chứng cho sự kết hợp hoàn hảo giữa lịch sử tâm linh và phát triển đô thị.";
            } else {
                narrativeTitle.text = "Taiwan: Living Traditions";
                narrativeBody.text = "Discover temples nestled between skyscrapers, a testament to the seamless blend of spiritual history and rapid urban development.";
            }
        }
        else if (locIndex == 2) // Trạng thái: KERALA
        {
            if (langIndex == 1) {
                narrativeTitle.text = "喀拉拉邦：上帝的國度";
                narrativeBody.text = "探索寧靜的迴水網絡和充滿活力的傳統，這些傳統塑造了印度南部的文化景觀。";
            } else if (langIndex == 2) {
                narrativeTitle.text = "കേരളം: ദൈവത്തിന്റെ സ്വന്തം നാട്";
                narrativeBody.text = "ദക്ഷിണേന്ത്യയുടെ സാംസ്കാരിക ഭൂപ്രകൃതിയെ രൂപപ്പെടുത്തിയ കായലുകളുടെയും ശാന്തമായ പാരമ്പര്യങ്ങളുടെയും ശൃംഖല പര്യവേക്ഷണം ചെയ്യുക.";
            } else if (langIndex == 3) {
                narrativeTitle.text = "Kerala: Vùng Đất Của Thượng Đế";
                narrativeBody.text = "Khám phá mạng lưới kênh rạch thanh bình và những truyền thống rực rỡ đã hình thành nên bức tranh văn hóa của miền nam Ấn Độ.";
            } else {
                narrativeTitle.text = "Kerala: God's Own Country";
                narrativeBody.text = "Explore the serene network of backwaters and vibrant traditions that have shaped the cultural landscape of southern India.";
            }
        }
        else if (locIndex == 3) // Trạng thái: VIETNAM
        {
            if (langIndex == 1) {
                narrativeTitle.text = "越南：昇龍之地";
                narrativeBody.text = "穿梭於雄偉的石灰岩喀斯特地貌和幾個世紀的民間藝術之中，反映了對和平生活的簡單渴望。";
            } else if (langIndex == 2) {
                narrativeTitle.text = "വിയറ്റ്നാം: ഉയരുന്ന ഡ്രാഗൺ";
                narrativeBody.text = "സമാധാനപരമായ ജീവിതത്തിനായുള്ള ലളിതമായ ആഗ്രഹങ്ങളെ പ്രതിഫലിപ്പിക്കുന്ന നൂറ്റാണ്ടുകൾ പഴക്കമുള്ള നാടോടി കലകളിലൂടെ സഞ്ചരിക്കുക.";
            } else if (langIndex == 3) {
                narrativeTitle.text = "Việt Nam: Rồng Bay Lên";
                narrativeBody.text = "Dạo bước giữa những dãy núi đá vôi hùng vĩ và nghệ thuật dân gian hàng thế kỷ, phản chiếu ước vọng giản dị về một cuộc sống bình yên.";
            } else {
                narrativeTitle.text = "Vietnam: Ascending Dragon";
                narrativeBody.text = "Navigate through majestic limestone karsts and centuries of folk art, reflecting the simple desires for a peaceful life.";
            }
        }
    }
}