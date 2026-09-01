using UnityEngine;

public class LocalizedAudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    
    // Tiền tố ngôn ngữ tương ứng với các Index: 0 (Anh), 1 (Đài Loan), 2 (Malayalam), 3 (Việt)
    private string[] langPrefixes = { "en", "cn", "kl", "vn" };

    // Fungus sẽ truyền tên gốc (baseFileName) vào đây
    public void PlayVoice(string baseFileName)
    {
        // Lấy ngôn ngữ hiện tại đang chọn (Đảm bảo key "AppLanguage" khớp với hệ thống của bạn)
        int langIndex = PlayerPrefs.GetInt("AppLanguage", 0);
        langIndex = Mathf.Clamp(langIndex, 0, langPrefixes.Length - 1);
        
        // Tự động ghép tiền tố với tên gốc (Ví dụ: "vn" + "VN_food" = "vnVN_food")
        string fullFileName = langPrefixes[langIndex] + baseFileName;

        // Tìm file trong thư mục Assets/Resources/VoiceOver/
        string path = $"VoiceOver/{fullFileName}";
        AudioClip clip = Resources.Load<AudioClip>(path);

        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy file âm thanh tại: Resources/{path}");
        }
    }
}