using UnityEngine;
using System.Collections;

public class MultilingualAvatarAudio : MonoBehaviour
{
    [Header("File âm thanh theo ngôn ngữ")]
    public AudioClip englishClip;
    public AudioClip chineseClip;
    public AudioClip malayalamClip;
    public AudioClip vietnameseClip;

    [Header("Cài đặt")]
    [Tooltip("Thời gian chờ trước khi nói (Tính bằng giây)")]
    public float delayBeforeSpeak = 2.0f;

    [Header("Hoạt ảnh (Animation)")]
    public Animator avatarAnimator;
    [Tooltip("Tên biến Bool trong Animator để kích hoạt nói")]
    public string talkParameterName = "Talk";

    // Cái loa độc lập dành riêng cho giọng nói
    private AudioSource voiceSource;

    private void Awake()
    {
        // Tự động thêm AudioSource nếu chưa có
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        
        // Tự động tìm Animator trên nhân vật nếu quên kéo thả
        if (avatarAnimator == null)
        {
            avatarAnimator = GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        // Khi nhân vật hiện ra, bắt đầu đếm ngược
        StartCoroutine(PlayVoiceRoutine());
    }

    private IEnumerator PlayVoiceRoutine()
    {
        // 1. Chờ hiệu ứng ánh sáng
        yield return new WaitForSeconds(delayBeforeSpeak);

        // 2. Chọn ngôn ngữ
        int currentLanguageIndex = PlayerPrefs.GetInt("SelectedLanguageIndex", 0);
        AudioClip clipToPlay = englishClip;

        switch (currentLanguageIndex)
        {
            case 0: clipToPlay = englishClip; break;
            case 1: clipToPlay = chineseClip; break;
            case 2: clipToPlay = malayalamClip; break;
            case 3: clipToPlay = vietnameseClip; break;
        }

        // 3. Phát giọng nói VÀ bật hoạt ảnh
        if (clipToPlay != null && voiceSource != null)
        {
            voiceSource.clip = clipToPlay;
            voiceSource.Play(); 
            
            // Ra lệnh cho nhân vật cử động
            if (avatarAnimator != null)
            {
                avatarAnimator.SetBool(talkParameterName, true);
            }

            Debug.Log("[AvatarAudio] Đang phát Audio và Animation ngôn ngữ: " + currentLanguageIndex);

            // 4. Đo thời lượng của file âm thanh và chờ cho đến khi nó phát xong
            yield return new WaitForSeconds(clipToPlay.length);

            // 5. Khi phát xong, ra lệnh cho nhân vật ngừng cử động miệng
            if (avatarAnimator != null)
            {
                avatarAnimator.SetBool(talkParameterName, false);
            }
        }
    }

    private void OnDisable()
    {
        // Tắt tiếng VÀ tắt hoạt ảnh nếu người dùng ấn chuyển Tab đột ngột
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
        
        if (avatarAnimator != null)
        {
            avatarAnimator.SetBool(talkParameterName, false);
        }
    }
}