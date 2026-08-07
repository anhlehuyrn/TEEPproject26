using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

public class AiNpcQuestionController : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverBaseUrl = "http://127.0.0.1:8787";

    [Header("NPC Context")]
    [SerializeField] private string npcName = "Culture Guide NPC";
    [SerializeField] public string targetName = "Culture";
    [TextArea(2, 5)]
    [SerializeField] public string lessonContext = "You are explaining the culture (festival, traditional costume and food) of Taiwan, Vietnam and Kerala to students.";

    [Serializable]
    public class TargetRagContext
    {
        public string targetImageName;
        public string npcName;
        [TextArea(2, 5)]
        public string lessonContext;
    }

    [Header("Dynamic RAG Knowledge Base")]
    [SerializeField] private List<TargetRagContext> targetRagContexts = new List<TargetRagContext>();

    [Header("UI & State References")]
    [SerializeField] private Button askButton;
    [SerializeField] private Button exploreArtworkButton;
    [SerializeField] private Text answerText;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject[] answerPanelObjects;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource answerAudioSource;
    [SerializeField] private int maxRecordingSeconds = 30;
    [SerializeField] private int sampleRate = 44100;

    [Header("Vision & AI Settings")]
    [SerializeField] private bool sendTargetImageToAi = true;
    [SerializeField] private XRReferenceImageLibrary referenceImageLibraryForVision;
    [SerializeField] private Texture2D fallbackTargetImageForVision;
    [SerializeField] private int visionImageJpegQuality = 75;

    [Header("UI Formatting")]
    [SerializeField] private int buttonTextMaxFontSize = 40;
    [SerializeField] private int buttonTextMinFontSize = 18;
    [SerializeField] private Vector2 buttonTextPadding = new Vector2(14, 8);
    [SerializeField] private bool makeAnswerTextScrollable = true;
    [SerializeField] private float answerScrollPadding = 30f;
    [SerializeField] private float answerScrollSensitivity = 24f;
    [SerializeField] private float typewriterSpeed = 0.03f; // Tốc độ gõ chữ

    [Header("Spawner Reference")]
    [SerializeField] private ARFloatingItemSpawner floatingItemSpawner;

    private string currentScannedTargetName;
    private bool hasVisibleAnswer = false;
    private bool isBusy = false;
    private bool isExploring = false;
    private bool isRecording = false;
    private AudioClip recordingClip;
    
    private Text askButtonText;
    private TMP_Text askButtonTmpText;
    private Text exploreArtworkButtonText;
    private TMP_Text exploreArtworkButtonTmpText;
    
    private RectTransform answerTextRect;
    private ScrollRect answerScrollRect;
    private RectTransform answerViewportRect;
    private RectTransform answerContentRect;

    public bool HasVisibleAnswer => hasVisibleAnswer;

    public void SetCurrentScannedTargetName(string scannedTargetName)
    {
        if (!string.IsNullOrWhiteSpace(scannedTargetName))
        {
            currentScannedTargetName = scannedTargetName;
            UpdateRagContextForTarget(scannedTargetName);
        }
    }

    public void UpdateRagContextForTarget(string scannedTargetName)
    {
        if (string.IsNullOrWhiteSpace(scannedTargetName)) return;

        currentScannedTargetName = scannedTargetName;

        if (targetRagContexts != null)
        {
            foreach (TargetRagContext ragCtx in targetRagContexts)
            {
                if (ragCtx != null && ragCtx.targetImageName == scannedTargetName)
                {
                    this.npcName = ragCtx.npcName;
                    this.lessonContext = ragCtx.lessonContext;
                    Debug.Log($"[RAG Context Switched] NPC: {npcName} | Target: {scannedTargetName}");
                    return;
                }
            }
        }

        switch (scannedTargetName)
        {
            case "DongHo":
                this.npcName = "Dong Ho Guide";
                this.lessonContext = "You are explaining Dong Ho folk painting to students, focusing on woodblock printing, natural colors, and symbols of prosperity like 'The Mice's Wedding'. Keep it concise and engaging.";
                break;
            case "VN_cloth":
                this.npcName = "Vietnamese Costume Guide";
                this.lessonContext = "You are explaining traditional Vietnamese clothing like the 'Ao Tu Than' (Four-panel dress) to students, highlighting its elegance and history in Northern Vietnam.";
                break;
            case "VN_food":
                this.npcName = "Vietnamese Cuisine Guide";
                this.lessonContext = "You are explaining 'Banh Phu The', a traditional Vietnamese sweet cake symbolizing love, loyalty, and balance in marriage customs.";
                break;
            case "VN_fest":
                this.npcName = "Vietnamese Festival Guide";
                this.lessonContext = "You are explaining the Dong Ky Firecracker Festival in Vietnam. Focus on grand processions, village traditions, community unity, and wishes for prosperity.";
                break;
            case "KL_cloth":
                this.npcName = "Kerala Costume Guide";
                this.lessonContext = "You are explaining the Kasavu Saree, a traditional white-and-gold elegant attire from Kerala, India. It symbolizes Kerala's heritage, simplicity, and grace.";
                break;
            case "KL_food":
                this.npcName = "Kerala Cuisine Guide";
                this.lessonContext = "You are explaining 'Kerala Sadya', a grand vegetarian feast served on a banana leaf. Describe its rich flavors, various side dishes, and how it celebrates togetherness.";
                break;
            case "KL_fest":
                this.npcName = "Kerala Festival Guide";
                this.lessonContext = "You are explaining the Onam harvest festival in Kerala. Mention King Mahabali, joy, colors, Pookkalam (flower carpets), and unity.";
                break;
            case "TW_cloth":
                this.npcName = "Taiwanese Costume Guide";
                this.lessonContext = "You are explaining the traditional clothing of the Paiwan indigenous people in Taiwan. Discuss its ancient style, elegance, beadwork, and aesthetics.";
                break;
            case "TW_food":
                this.npcName = "Taiwanese Cuisine Guide";
                this.lessonContext = "You are explaining Taiwanese Beef Noodle Soup to students. Mention its rich broth, tender beef, noodles, and how it represents generations of culinary tradition.";
                break;
            case "TW_fest":
                this.npcName = "Taiwanese Festival Guide";
                this.lessonContext = "You are explaining the Taiwan Lantern Festival. Describe the glowing lanterns lighting up the night sky, community celebrations, and making wishes.";
                break;
            default:
                break;
        }
    }

    private void Awake()
    {
        if (answerAudioSource == null) answerAudioSource = GetComponent<AudioSource>();
        ConfigureAnswerText();

        if (askButtonText == null && askButton != null) askButtonText = askButton.GetComponentInChildren<Text>();
        if (askButtonTmpText == null && askButton != null) askButtonTmpText = askButton.GetComponentInChildren<TMP_Text>();
        if (exploreArtworkButtonText == null && exploreArtworkButton != null) exploreArtworkButtonText = exploreArtworkButton.GetComponentInChildren<Text>();
        if (exploreArtworkButtonTmpText == null && exploreArtworkButton != null) exploreArtworkButtonTmpText = exploreArtworkButton.GetComponentInChildren<TMP_Text>();

        ConfigureButtonText(askButtonText);
        ConfigureButtonText(askButtonTmpText);
        ConfigureButtonText(exploreArtworkButtonText);
        ConfigureButtonText(exploreArtworkButtonTmpText);

        HideAnswerPanels();
        SetAskButtonLabel("Ask AI");
        SetExploreArtworkButtonLabel("Explore Artwork");
    }

    public void ToggleRecording()
    {
        if (isBusy || isExploring) return;
        if (isRecording) StopRecordingAndAsk();
        else StartRecording();
    }

    public void StartRecording()
    {
        if (isBusy || isRecording || isExploring) return;

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            SetStatus("Please allow microphone permission, then tap Ask again.");
            return;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            SetStatus("No microphone found.");
            return;
        }

        ClearCurrentAnswer();
        recordingClip = Microphone.Start(null, false, maxRecordingSeconds, sampleRate);
        isRecording = true;
        SetStatus("Recording...");
        SetAskButtonLabel("Stop Recording");
        SetExploreArtworkButtonEnabled(false);
    }

    public void StopRecordingAndAsk()
    {
        if (!isRecording || recordingClip == null) return;

        int samplePosition = Microphone.GetPosition(null);
        Microphone.End(null);
        isRecording = false;

        if (samplePosition <= 0)
        {
            SetStatus("No voice recorded. Try again.");
            SetAskButtonLabel("Ask AI");
            SetExploreArtworkButtonEnabled(true);
            return;
        }

        AudioClip trimmedClip = TrimClip(recordingClip, samplePosition);
        byte[] wavBytes = WavUtility.FromAudioClip(trimmedClip);

        SetAskButtonLabel("Sending");
        SetExploreArtworkButtonEnabled(false);
        StartCoroutine(SendQuestionRoutine(wavBytes));
    }

    public void ToggleExploreArtwork()
    {
        if (isBusy || isRecording) return;
        if (isExploring) StopExploringArtwork();
        else StartExploringArtwork();
    }

    public void ResetQuestionSession()
    {
        StopAllCoroutines();
        if (isRecording) Microphone.End(null);

        isRecording = false;
        isBusy = false;
        isExploring = false;
        recordingClip = null;

        StopAnswerAudio();

        if (answerText != null)
        {
            answerText.text = "";
            UpdateAnswerTextLayout();
        }

        HideAnswerPanels();
        if (floatingItemSpawner != null) floatingItemSpawner.HideFloatingItems();

        SetStatus("");
        SetButtonEnabled(true);
        SetExploreArtworkButtonEnabled(true);
        SetAskButtonLabel("Ask AI");
        SetExploreArtworkButtonLabel("Explore Artwork");
    }

    private IEnumerator SendQuestionRoutine(byte[] wavBytes)
    {
        isBusy = true;
        SetButtonEnabled(false);
        SetExploreArtworkButtonEnabled(false);
        HideAnswerPanels();
        SetStatus("Sending question...");

        AskRequest request = new AskRequest
        {
            audioBase64 = Convert.ToBase64String(wavBytes),
            mimeType = "audio/wav",
            npcName = npcName,
            targetName = GetActiveTargetName(),
            context = lessonContext
        };

        AddVisionImageToRequest(request);

        string json = JsonUtility.ToJson(request);
        byte[] body = Encoding.UTF8.GetBytes(json);
        string askUrl = CombineUrl(serverBaseUrl, "/ask");

        using (UnityWebRequest webRequest = new UnityWebRequest(askUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus("AI request failed: " + webRequest.error);
                FinishBusyState();
                yield break;
            }

            AskResponse response = JsonUtility.FromJson<AskResponse>(webRequest.downloadHandler.text);

            if (!string.IsNullOrEmpty(response.error))
            {
                SetStatus("AI error: " + response.error);
                FinishBusyState();
                yield break;
            }

            // --- BẮT ĐẦU HIỆU ỨNG TỰ ĐỘNG GÕ CHỮ & PHÁT ÂM THANH ---
            ShowAnswerPanels();
            SetStatus("AI is replying...");

            if (!string.IsNullOrEmpty(response.audioUrl))
            {
                // Bắt đầu tải và phát âm thanh song song
                StartCoroutine(DownloadAndPlayAudio(CombineUrl(serverBaseUrl, response.audioUrl)));
            }

            if (answerText != null)
            {
                ConfigureAnswerText();
                string prefixText = "<b>User:</b>   " + response.transcript + "\n\n<b>AI:</b>   ";
                // Kích hoạt hiệu ứng gõ chữ và chờ nó hoàn thành
                yield return StartCoroutine(TypewriterAnswer(prefixText, response.reply));
            }
            // --- KẾT THÚC HIỆU ỨNG ---
        }
        
        SetStatus("Ready");
        FinishBusyState();
    }

    // Coroutine tạo hiệu ứng gõ từng chữ một
    private IEnumerator TypewriterAnswer(string prefix, string aiReply)
    {
        string currentText = prefix;
        answerText.text = currentText;
        UpdateAnswerTextLayout();

        foreach (char c in aiReply)
        {
            currentText += c;
            answerText.text = currentText;
            UpdateAnswerTextLayout();
            
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private IEnumerator DownloadAndPlayAudio(string audioUrl)
    {
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();
            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus("Audio download failed: " + audioRequest.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            if (answerAudioSource != null && clip != null)
            {
                answerAudioSource.clip = clip;
                answerAudioSource.Play();
            }
        }
    }

    private void AddVisionImageToRequest(AskRequest request)
    {
        if (!sendTargetImageToAi) return;

        Texture2D imageForVision = FindReferenceImageTexture(request.targetName) ?? fallbackTargetImageForVision;
        if (imageForVision == null)
        {
            SetStatus("No target image texture found. AI will answer without image context.");
            return;
        }

        Texture2D readableTexture = CreateReadableTextureCopy(imageForVision);
        byte[] jpgBytes = readableTexture.EncodeToJPG(visionImageJpegQuality);
        Destroy(readableTexture);

        request.imageBase64 = Convert.ToBase64String(jpgBytes);
        request.imageMimeType = "image/jpeg";
    }

    private string GetActiveTargetName()
    {
        if (!string.IsNullOrWhiteSpace(currentScannedTargetName)) return currentScannedTargetName;
        return targetName;
    }

    private Texture2D FindReferenceImageTexture(string referenceImageName)
    {
        if (referenceImageLibraryForVision == null || string.IsNullOrWhiteSpace(referenceImageName)) return null;

        for (int i = 0; i < referenceImageLibraryForVision.count; i++)
        {
            XRReferenceImage referenceImage = referenceImageLibraryForVision[i];
            if (referenceImage.name == referenceImageName && referenceImage.texture != null)
            {
                return referenceImage.texture;
            }
        }
        return null;
    }

    private Texture2D CreateReadableTextureCopy(Texture2D source)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);

        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;

        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(temporary);
        return copy;
    }

    private AudioClip TrimClip(AudioClip sourceClip, int samplePosition)
    {
        int channels = sourceClip.channels;
        float[] sourceData = new float[samplePosition * channels];
        sourceClip.GetData(sourceData, 0);

        AudioClip trimmedClip = AudioClip.Create("StudentQuestion", samplePosition, channels, sourceClip.frequency, false);
        trimmedClip.SetData(sourceData, 0);
        return trimmedClip;
    }

    private void FinishBusyState()
    {
        isBusy = false;
        SetButtonEnabled(true);
        SetExploreArtworkButtonEnabled(true);
        SetAskButtonLabel("Ask AI");
    }

    private void SetButtonEnabled(bool enabled) => SetAskButtonEnabled(enabled);

    private void SetAskButtonEnabled(bool enabled)
    {
        if (askButton != null) askButton.interactable = enabled;
    }

    private void SetExploreArtworkButtonEnabled(bool enabled)
    {
        if (exploreArtworkButton != null) exploreArtworkButton.interactable = enabled;
    }

    private void SetAskButtonLabel(string label)
    {
        if (askButtonText != null)
        {
            askButtonText.text = label;
            ConfigureButtonText(askButtonText);
        }
        if (askButtonTmpText != null)
        {
            askButtonTmpText.text = label;
            ConfigureButtonText(askButtonTmpText);
        }
    }

    private void SetExploreArtworkButtonLabel(string label)
    {
        if (exploreArtworkButtonText != null)
        {
            exploreArtworkButtonText.text = label;
            ConfigureButtonText(exploreArtworkButtonText);
        }
        if (exploreArtworkButtonTmpText != null)
        {
            exploreArtworkButtonTmpText.text = label;
            ConfigureButtonText(exploreArtworkButtonTmpText);
        }
    }

    private void ConfigureButtonText(Text buttonLabel)
    {
        if (buttonLabel == null) return;
        int maxSize = Mathf.Max(1, buttonTextMaxFontSize);
        int minSize = Mathf.Clamp(buttonTextMinFontSize, 1, maxSize);

        buttonLabel.fontSize = maxSize;
        buttonLabel.resizeTextForBestFit = true;
        buttonLabel.resizeTextMinSize = minSize;
        buttonLabel.resizeTextMaxSize = maxSize;
        buttonLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        buttonLabel.verticalOverflow = VerticalWrapMode.Truncate;
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        ApplyButtonTextPadding(buttonLabel.rectTransform);
    }

    private void ConfigureButtonText(TMP_Text buttonLabel)
    {
        if (buttonLabel == null) return;
        float maxSize = Mathf.Max(1f, buttonTextMaxFontSize);
        float minSize = Mathf.Clamp(buttonTextMinFontSize, 1f, maxSize);

        buttonLabel.fontSize = maxSize;
        buttonLabel.enableAutoSizing = true;
        buttonLabel.fontSizeMin = minSize;
        buttonLabel.fontSizeMax = maxSize;
        buttonLabel.enableWordWrapping = true;
        buttonLabel.overflowMode = TextOverflowModes.Truncate;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        ApplyButtonTextPadding(buttonLabel.rectTransform);
    }

    private void ApplyButtonTextPadding(RectTransform labelRect)
    {
        if (labelRect == null) return;
        if (labelRect.anchorMin != Vector2.zero || labelRect.anchorMax != Vector2.one) return;

        float horizontalPadding = Mathf.Max(0f, buttonTextPadding.x);
        float verticalPadding = Mathf.Max(0f, buttonTextPadding.y);
        labelRect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        labelRect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private void StartExploringArtwork()
    {
        ClearCurrentAnswer();
        isExploring = true;
        SetAskButtonEnabled(false);
        SetExploreArtworkButtonEnabled(true);
        SetExploreArtworkButtonLabel("Exploring");

        if (floatingItemSpawner != null) floatingItemSpawner.ShowFloatingItems();
    }

    private void ClearCurrentAnswer()
    {
        StopAnswerAudio();
        HideAnswerPanels();
        if (answerText != null)
        {
            answerText.text = "";
            UpdateAnswerTextLayout();
        }
    }

    private void StopExploringArtwork()
    {
        isExploring = false;
        SetAskButtonEnabled(true);
        SetExploreArtworkButtonEnabled(true);
        SetExploreArtworkButtonLabel("Explore Artwork");

        if (floatingItemSpawner != null) floatingItemSpawner.HideFloatingItems();
    }

    public void StopAnswerAudio()
    {
        if (answerAudioSource == null) return;
        answerAudioSource.Stop();
        answerAudioSource.clip = null;
    }

    private void ShowAnswerPanels()
    {
        hasVisibleAnswer = true;
        SetAnswerPanelsVisible(true);
        SetupAnswerScrollView();
        UpdateAnswerTextLayout();
        StartCoroutine(UpdateAnswerTextLayoutNextFrame());
    }

    private void HideAnswerPanels()
    {
        hasVisibleAnswer = false;
        SetAnswerPanelsVisible(false);
    }

    private void ConfigureAnswerText()
    {
        if (answerText == null) return;
        answerText.supportRichText = true;
        answerText.horizontalOverflow = HorizontalWrapMode.Wrap;
        answerText.verticalOverflow = VerticalWrapMode.Overflow;
        answerText.alignment = TextAnchor.UpperLeft;
        answerTextRect = answerText.rectTransform;
    }

    private void SetupAnswerScrollView()
    {
        if (!makeAnswerTextScrollable || answerText == null || answerTextRect == null) return;

        answerScrollRect = answerText.GetComponentInParent<ScrollRect>();
        if (answerScrollRect != null)
        {
            answerViewportRect = answerScrollRect.viewport != null ? answerScrollRect.viewport : answerScrollRect.GetComponent<RectTransform>();
            answerContentRect = answerScrollRect.content != null ? answerScrollRect.content : answerTextRect;
            return;
        }

        RectTransform originalParent = answerTextRect.parent as RectTransform;
        if (originalParent == null) return;

        int originalSiblingIndex = answerTextRect.GetSiblingIndex();
        Vector2 originalAnchorMin = answerTextRect.anchorMin;
        Vector2 originalAnchorMax = answerTextRect.anchorMax;
        Vector2 originalPivot = answerTextRect.pivot;
        Vector2 originalAnchoredPosition = answerTextRect.anchoredPosition;
        Vector2 originalSizeDelta = answerTextRect.sizeDelta;

        GameObject viewportObject = new GameObject("Answer Scroll View", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
        answerViewportRect = viewportObject.GetComponent<RectTransform>();
        answerViewportRect.SetParent(originalParent, false);
        answerViewportRect.SetSiblingIndex(originalSiblingIndex);
        answerViewportRect.anchorMin = originalAnchorMin;
        answerViewportRect.anchorMax = originalAnchorMax;
        answerViewportRect.pivot = originalPivot;
        answerViewportRect.anchoredPosition = originalAnchoredPosition;
        answerViewportRect.sizeDelta = originalSizeDelta;

        GameObject contentObject = new GameObject("Answer Scroll Content", typeof(RectTransform));
        answerContentRect = contentObject.GetComponent<RectTransform>();
        answerContentRect.SetParent(answerViewportRect, false);
        answerContentRect.anchorMin = new Vector2(0f, 1f);
        answerContentRect.anchorMax = new Vector2(1f, 1f);
        answerContentRect.pivot = new Vector2(0.5f, 1f);
        answerContentRect.anchoredPosition = Vector2.zero;
        answerContentRect.sizeDelta = Vector2.zero;

        answerTextRect.SetParent(answerContentRect, false);
        answerTextRect.anchorMin = new Vector2(0f, 1f);
        answerTextRect.anchorMax = new Vector2(1f, 1f);
        answerTextRect.pivot = new Vector2(0.5f, 1f);
        answerTextRect.anchoredPosition = Vector2.zero;
        answerTextRect.sizeDelta = new Vector2(-answerScrollPadding, 0f);

        answerScrollRect = viewportObject.GetComponent<ScrollRect>();
        answerScrollRect.content = answerContentRect;
        answerScrollRect.viewport = answerViewportRect;
        answerScrollRect.horizontal = false;
        answerScrollRect.vertical = true;
        answerScrollRect.movementType = ScrollRect.MovementType.Clamped;
        answerScrollRect.scrollSensitivity = answerScrollSensitivity;
        answerScrollRect.inertia = true;

        Scrollbar verticalScrollbar = CreateAnswerScrollbar(answerViewportRect);
        answerScrollRect.verticalScrollbar = verticalScrollbar;
        answerScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        UpdateAnswerTextLayout();
    }

    private Scrollbar CreateAnswerScrollbar(RectTransform parent)
    {
        GameObject scrollbarObject = new GameObject("Answer Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.SetParent(parent, false);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(10f, 0f);

        Image scrollbarImage = scrollbarObject.GetComponent<Image>();
        scrollbarImage.color = new Color(1f, 1f, 1f, 0.18f);

        GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
        RectTransform slidingAreaRect = slidingAreaObject.GetComponent<RectTransform>();
        slidingAreaRect.SetParent(scrollbarRect, false);
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.SetParent(slidingAreaRect, false);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.65f);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        return scrollbar;
    }

    private void UpdateAnswerTextLayout()
    {
        if (answerText == null || answerTextRect == null) return;
        if (!makeAnswerTextScrollable || answerViewportRect == null || answerContentRect == null) return;

        Canvas.ForceUpdateCanvases();
        float viewportHeight = Mathf.Max(1f, answerViewportRect.rect.height);
        float contentHeight = Mathf.Max(viewportHeight, answerText.preferredHeight + answerScrollPadding);

        answerContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        answerTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        answerTextRect.offsetMin = new Vector2(0f, answerTextRect.offsetMin.y);
        answerTextRect.offsetMax = new Vector2(-answerScrollPadding, answerTextRect.offsetMax.y);

        // GHÌM THANH CUỘN XUỐNG ĐÁY MỖI KHI CHỮ ĐƯỢC CẬP NHẬT
        if (answerScrollRect != null) answerScrollRect.verticalNormalizedPosition = 0f; 
    }

    private IEnumerator UpdateAnswerTextLayoutNextFrame()
    {
        yield return null;
        UpdateAnswerTextLayout();
    }

    private void SetAnswerPanelsVisible(bool visible)
    {
        foreach (GameObject panelObject in answerPanelObjects ?? new GameObject[0])
        {
            if (panelObject != null) panelObject.SetActive(visible);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl)) return path;
        if (string.IsNullOrEmpty(path)) return baseUrl;
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    [Serializable]
    private class AskRequest
    {
        public string audioBase64;
        public string mimeType;
        public string npcName;
        public string targetName;
        public string context;
        public string imageBase64;
        public string imageMimeType;
    }

    [Serializable]
    private class AskResponse
    {
        public string transcript;
        public string reply;
        public string audioUrl;
        public string error;
    }
}

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = ConvertFloatSamplesToPcm16(samples);
        byte[] wav = new byte[44 + pcmData.Length];

        WriteAscii(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + pcmData.Length);
        WriteAscii(wav, 8, "WAVE");
        WriteAscii(wav, 12, "fmt ");
        WriteInt(wav, 16, 16);
        WriteShort(wav, 20, 1);
        WriteShort(wav, 22, (short)clip.channels);
        WriteInt(wav, 24, clip.frequency);
        WriteInt(wav, 28, clip.frequency * clip.channels * 2);
        WriteShort(wav, 32, (short)(clip.channels * 2));
        WriteShort(wav, 34, 16);
        WriteAscii(wav, 36, "data");
        WriteInt(wav, 40, pcmData.Length);
        Buffer.BlockCopy(pcmData, 0, wav, 44, pcmData.Length);

        return wav;
    }

    private static byte[] ConvertFloatSamplesToPcm16(float[] samples)
    {
        byte[] pcmData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            float sample = Mathf.Clamp(samples[i], -1f, 1f);
            short value = (short)(sample * short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(value);
            pcmData[i * 2] = bytes[0];
            pcmData[i * 2 + 1] = bytes[1];
        }
        return pcmData;
    }

    private static void WriteAscii(byte[] buffer, int offset, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }

    private static void WriteShort(byte[] buffer, int offset, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }
}