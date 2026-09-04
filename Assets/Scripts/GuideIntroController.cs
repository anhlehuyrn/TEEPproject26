using UnityEngine;
using UnityEngine.UI;

public class GuideIntroController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Component Image hiển thị ảnh Intro")]
    public Image introImage;

    [Tooltip("GameObject Panel/Ảnh hướng dẫn cần ẩn hiện (Intro).")]
    public GameObject guidePanel;

    [Header("Intro Sprites (Theo 4 ngôn ngữ)")]
    [Tooltip("0: English Intro")]
    public Sprite enIntro;

    [Tooltip("1: Traditional Chinese Intro (cnIntro)")]
    public Sprite cnIntro;

    [Tooltip("2: Malayalam Intro (klIntro)")]
    public Sprite klIntro;

    [Tooltip("3: Vietnamese Intro (vnIntro)")]
    public Sprite vnIntro;

    [Header("Behavior Settings")]
    [Tooltip("Tự động hiện ảnh hướng dẫn ngay khi vừa vào trang Scan")]
    public bool autoShowOnStart = true;

    [Tooltip("Tự động đóng bảng hướng dẫn khi bấm vào vùng ngoài bức ảnh")]
    public bool closeOnOutsideClick = true;

    [Tooltip("Tự động đóng bảng hướng dẫn khi bấm trực tiếp vào bức ảnh")]
    public bool closeOnImageClick = true;

    private float openedTimestamp = 0f;

    private void Awake()
    {
        if (introImage == null)
        {
            introImage = GetComponentInChildren<Image>(true);
        }

        if (guidePanel == null)
        {
            if (introImage != null)
            {
                guidePanel = introImage.gameObject;
            }
        }
    }

    private void OnEnable()
    {
        UpdateGuideImage();
    }

    private void Start()
    {
        UpdateGuideImage();
        if (autoShowOnStart)
        {
            OpenGuide();
        }
    }

    private void Update()
    {
        if (!closeOnOutsideClick && !closeOnImageClick) return;

        GameObject target = GetTargetPanel();
        if (target == null || !target.activeInHierarchy) return;

        // Bỏ qua trong 0.2s đầu sau khi vừa mở để tránh click xuyên từ nút bấm
        if (Time.unscaledTime - openedTimestamp < 0.2f) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleScreenClick(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleScreenClick(Input.GetTouch(0).position);
        }
    }

    private void HandleScreenClick(Vector2 screenPosition)
    {
        if (introImage == null || !introImage.gameObject.activeInHierarchy) return;

        Canvas parentCanvas = introImage.GetComponentInParent<Canvas>();
        Camera eventCam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCam = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
        }

        RectTransform imgRect = introImage.rectTransform;
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(imgRect, screenPosition, eventCam);

        if (!isInside && closeOnOutsideClick)
        {
            CloseGuide();
        }
        else if (isInside && closeOnImageClick)
        {
            CloseGuide();
        }
    }

    private GameObject GetTargetPanel()
    {
        if (guidePanel != null) return guidePanel;
        if (introImage != null) return introImage.gameObject;
        return null;
    }

    /// <summary>
    /// Cập nhật Sprite của ảnh Intro theo ngôn ngữ hiện tại trong PlayerPrefs
    /// </summary>
    public void UpdateGuideImage()
    {
        int langIndex = PlayerPrefs.GetInt("AppLanguage", 0);
        SetLanguage(langIndex);
    }

    /// <summary>
    /// Gán Sprite Intro theo chỉ số ngôn ngữ (0: en, 1: zh/cn, 2: ml/kl, 3: vi/vn)
    /// </summary>
    public void SetLanguage(int langIndex)
    {
        if (introImage == null) return;

        Sprite selectedSprite = null;
        switch (langIndex)
        {
            case 0:
                selectedSprite = enIntro;
                break;
            case 1:
                selectedSprite = cnIntro;
                break;
            case 2:
                selectedSprite = klIntro;
                break;
            case 3:
                selectedSprite = vnIntro;
                break;
            default:
                selectedSprite = enIntro;
                break;
        }

        if (selectedSprite != null)
        {
            introImage.sprite = selectedSprite;
        }
    }

    /// <summary>
    /// Bật/Tắt panel hướng dẫn (nút Guide icon (i) luôn hiển thị)
    /// </summary>
    public void ToggleGuide()
    {
        GameObject target = GetTargetPanel();
        if (target != null)
        {
            bool nextState = !target.activeSelf;
            if (nextState)
            {
                OpenGuide();
            }
            else
            {
                CloseGuide();
            }
        }
    }

    /// <summary>
    /// Mở bảng hướng dẫn
    /// </summary>
    public void OpenGuide()
    {
        UpdateGuideImage();
        openedTimestamp = Time.unscaledTime;
        GameObject target = GetTargetPanel();
        if (target != null) target.SetActive(true);
    }

    /// <summary>
    /// Đóng bảng hướng dẫn khi nhấn nút đóng hoặc chạm vào ngoài ảnh
    /// </summary>
    public void CloseGuide()
    {
        GameObject target = GetTargetPanel();
        if (target != null) target.SetActive(false);
    }
}
