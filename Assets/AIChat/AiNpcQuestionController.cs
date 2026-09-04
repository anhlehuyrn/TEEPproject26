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
using UnityEngine.Video; 

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
    [SerializeField] private GameObject listeningObject;
    
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
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Spawner Reference")]
    [SerializeField] private ARFloatingItemSpawner floatingItemSpawner;

    [Serializable]
    public class TargetVideoMapping
    {
        public string targetImageName;
        public VideoClip videoClip;
    }
    
    [Header("Video Explore Settings")]
    [SerializeField] private GameObject videoPanel; 
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private List<TargetVideoMapping> targetVideos = new List<TargetVideoMapping>();

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
    private bool isScrollViewGenerated = false; // Cờ khóa chống sinh rác UI

    // Quản lý luồng (Coroutines) để chống dẫm nhau
    private Coroutine currentAskRoutine;
    private Coroutine currentTypewriterRoutine;
    private Coroutine currentAudioDownloadRoutine;
    private Coroutine lipSyncCoroutine;
    
    private Animator cachedNpcAnimator; // Cache lại Animator để đỡ tốn hiệu năng quét

    public bool HasVisibleAnswer => hasVisibleAnswer;

    private void Awake()
    {
        if (answerAudioSource == null) answerAudioSource = GetComponent<AudioSource>();
        ConfigureAnswerText();

        AutoFindListeningObject();

        if (askButton != null)
        {
            Text[] texts = askButton.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                if (t.gameObject != askButton.gameObject && (listeningObject == null || t.gameObject != listeningObject) && t.name != "Listen" && t.name != "Listening")
                {
                    askButtonText = t;
                    break;
                }
            }

            TMP_Text[] tmpTexts = askButton.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmpTexts)
            {
                if (t.gameObject != askButton.gameObject && (listeningObject == null || t.gameObject != listeningObject) && t.name != "Listen" && t.name != "Listening" && t.name != "ExploreButton")
                {
                    askButtonTmpText = t;
                    break;
                }
            }
        }
        if (exploreArtworkButton != null)
        {
            exploreArtworkButtonText = exploreArtworkButton.GetComponentInChildren<Text>();
            exploreArtworkButtonTmpText = exploreArtworkButton.GetComponentInChildren<TMP_Text>();
        }

        ConfigureButtonText(askButtonText);
        ConfigureButtonText(askButtonTmpText);
        ConfigureButtonText(exploreArtworkButtonText);
        ConfigureButtonText(exploreArtworkButtonTmpText);

        HideAnswerPanels();
        SetListeningVisible(false);
        SetAskButtonLabel("Ask AI");
        SetExploreArtworkButtonLabel("Explore Artwork");
    }

    public void SetCurrentScannedTargetName(string scannedTargetName)
    {
        if (isRecording || isBusy || hasVisibleAnswer || isExploring) return;

        if (!string.IsNullOrWhiteSpace(scannedTargetName))
        {
            currentScannedTargetName = scannedTargetName;
            UpdateRagContextForTarget(scannedTargetName);
            
            PlayerPrefs.SetInt("Stamp_" + scannedTargetName, 1);
            PlayerPrefs.Save();
        }
    }

    public void UpdateRagContextForTarget(string scannedTargetName)
    {
        if (string.IsNullOrWhiteSpace(scannedTargetName)) return;
        currentScannedTargetName = scannedTargetName;

        if (targetRagContexts != null && targetRagContexts.Count > 0)
        {
            foreach (TargetRagContext ragCtx in targetRagContexts)
            {
                if (ragCtx != null && ragCtx.targetImageName == scannedTargetName)
                {
                    this.npcName = ragCtx.npcName;
                    this.lessonContext = ragCtx.lessonContext;
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
                this.lessonContext = "You are a guide explaining traditional Vietnamese clothing like the 'Ao Tu Than' (Four-panel dress) to students. Focus on its elegance, its history in Northern Vietnam, and how it represents the beauty of Vietnamese women.";
                break;
            case "VN_food":
                this.npcName = "Vietnamese Cuisine Guide";
                this.lessonContext = "You are explaining 'Banh Phu The', a traditional Vietnamese sweet cake symbolizing love, loyalty, and balance in marriage customs.";
                break;
            case "VN_fest":
                this.npcName = "Vietnamese Festival Guide";
                this.lessonContext = "You are a guide explaining the Dong Ky Firecracker Festival in Vietnam. Focus on the vibrant village atmosphere, the grand firecracker processions, honoring tradition, community unity, and wishes for prosperity.";
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
                this.lessonContext = "You are a guide explaining Taiwanese Beef Noodle Soup to students. Mention its rich broth, tender beef, noodles, and how it is shaped by generations of culinary tradition.";
                break;
            case "TW_fest":
                this.npcName = "Taiwanese Festival Guide";
                this.lessonContext = "You are explaining the Taiwan Lantern Festival. Describe the glowing lanterns lighting up the night sky, community celebrations, and making wishes.";
                break;
        }
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
        StartCoroutine(StartRecordingRoutine());
    }

    private IEnumerator StartRecordingRoutine()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            SetStatus("Waiting for microphone permission...");
            float timeout = 10f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone) && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                SetListeningVisible(false);
                yield break;
            }
        }
#endif
        if (Microphone.devices.Length == 0)
        {
            SetListeningVisible(false);
            yield break;
        }

        ClearCurrentAnswer();
        recordingClip = Microphone.Start(null, false, maxRecordingSeconds, sampleRate);
        isRecording = true;
        SetListeningVisible(true);
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
        SetListeningVisible(false);

        if (samplePosition <= 0)
        {
            SetAskButtonLabel("Ask AI");
            SetExploreArtworkButtonEnabled(true);
            return;
        }

        byte[] wavBytes = WavUtility.FromAudioClipTrimmed(recordingClip, samplePosition);
        
        Destroy(recordingClip);
        recordingClip = null;

        SetAskButtonLabel("Sending...");
        SetExploreArtworkButtonEnabled(false);
        
        if (currentAskRoutine != null) StopCoroutine(currentAskRoutine);
        currentAskRoutine = StartCoroutine(SendQuestionRoutine(wavBytes));
    }

    public void ToggleExploreArtwork()
    {
        if (isBusy || isRecording) return;
        if (isExploring) StopExploringArtwork();
        else StartExploringArtwork();
    }

    private void StartExploringArtwork()
    {
        ClearCurrentAnswer();
        isExploring = true;
        SetButtonEnabled(false);
        SetExploreArtworkButtonLabel("Close Video");

        string activeTarget = GetActiveTargetName();
        VideoClip clipToPlay = null;

        foreach (var mapping in targetVideos)
        {
            if (mapping.targetImageName == activeTarget)
            {
                clipToPlay = mapping.videoClip;
                break;
            }
        }

        if (clipToPlay != null && videoPlayer != null && videoPanel != null)
        {
            videoPanel.SetActive(true);
            videoPlayer.clip = clipToPlay;
            videoPlayer.Play();
        }
        else
        {
            SetStatus("No video available for this target.");
        }
    }

    private void StopExploringArtwork()
    {
        isExploring = false;
        SetButtonEnabled(true);
        SetExploreArtworkButtonLabel("Explore Artwork");

        if (videoPlayer != null) videoPlayer.Stop();
        if (videoPanel != null) videoPanel.SetActive(false);
    }

    public void ResetQuestionSession()
    {
        if (currentAskRoutine != null) StopCoroutine(currentAskRoutine);
        if (currentTypewriterRoutine != null) StopCoroutine(currentTypewriterRoutine);
        
        if (isRecording) Microphone.End(null);

        isRecording = false;
        isBusy = false;
        isExploring = false;
        recordingClip = null;

        SetListeningVisible(false);
        ClearCurrentAnswer();
        
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoPanel != null) videoPanel.SetActive(false);

        SetStatus("");
        SetButtonEnabled(true);
        SetExploreArtworkButtonEnabled(true);
        SetAskButtonLabel("Ask AI");
        SetExploreArtworkButtonLabel("Explore Artwork");
    }

    private void AutoFindListeningObject()
    {
        if (listeningObject != null) return;

        if (askButton != null)
        {
            Transform listenChild = askButton.transform.Find("Listen");
            if (listenChild == null) listenChild = askButton.transform.Find("Listening");
            if (listenChild != null)
            {
                listeningObject = listenChild.gameObject;
                return;
            }
        }

        Transform selfListenChild = transform.Find("Listen");
        if (selfListenChild == null) selfListenChild = transform.Find("Listening");
        if (selfListenChild != null)
        {
            listeningObject = selfListenChild.gameObject;
            return;
        }

        GameObject foundListen = GameObject.Find("Listen");
        if (foundListen != null)
        {
            listeningObject = foundListen;
        }
    }

    public void SetListeningVisible(bool visible)
    {
        if (listeningObject == null)
        {
            AutoFindListeningObject();
        }

        if (listeningObject != null)
        {
            listeningObject.SetActive(visible);
        }
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

            ShowAnswerPanels();
            SetStatus("AI is replying...");

            if (!string.IsNullOrEmpty(response.audioUrl))
            {
                if (currentAudioDownloadRoutine != null) StopCoroutine(currentAudioDownloadRoutine);
                currentAudioDownloadRoutine = StartCoroutine(DownloadAndPlayAudio(CombineUrl(serverBaseUrl, response.audioUrl)));
            }

            if (answerText != null)
            {
                ConfigureAnswerText();
                string prefixText = "<b>User:</b>   " + response.transcript + "\n\n<b>AI:</b>   ";
                if (currentTypewriterRoutine != null) StopCoroutine(currentTypewriterRoutine);
                currentTypewriterRoutine = StartCoroutine(TypewriterAnswer(prefixText, response.reply));
            }
        }
        
        SetStatus("Ready");
        FinishBusyState();
    }

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
            if (audioRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                if (answerAudioSource != null && clip != null)
                {
                    if (answerAudioSource.clip != null)
                    {
                        AudioClip oldClip = answerAudioSource.clip;
                        answerAudioSource.Stop();
                        answerAudioSource.clip = null;
                        Destroy(oldClip);
                    }

                    answerAudioSource.clip = clip;
                    answerAudioSource.Play();

                    if (lipSyncCoroutine != null) StopCoroutine(lipSyncCoroutine);
                    lipSyncCoroutine = StartCoroutine(HandleNpcLipSync());
                }
            }
        }
    }

    private void AddVisionImageToRequest(AskRequest request)
    {
        if (!sendTargetImageToAi) return;
        Texture2D imageForVision = FindReferenceImageTexture(request.targetName) ?? fallbackTargetImageForVision;
        if (imageForVision == null) return;

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
                return referenceImage.texture;
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

    private void FinishBusyState()
    {
        isBusy = false;
        SetButtonEnabled(true);
        SetExploreArtworkButtonEnabled(true);
        SetAskButtonLabel("Ask AI");
    }

    private void SetButtonEnabled(bool enabled) { if (askButton != null) askButton.interactable = enabled; }
    private void SetExploreArtworkButtonEnabled(bool enabled) { if (exploreArtworkButton != null) exploreArtworkButton.interactable = enabled; }
    
    private void SetAskButtonLabel(string label)
    {
        if (askButtonText != null) { askButtonText.text = label; ConfigureButtonText(askButtonText); }
        if (askButtonTmpText != null) { askButtonTmpText.text = label; ConfigureButtonText(askButtonTmpText); }
    }
    
    private void SetExploreArtworkButtonLabel(string label)
    {
        if (exploreArtworkButtonText != null) { exploreArtworkButtonText.text = label; ConfigureButtonText(exploreArtworkButtonText); }
        if (exploreArtworkButtonTmpText != null) { exploreArtworkButtonTmpText.text = label; ConfigureButtonText(exploreArtworkButtonTmpText); }
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
    
    public void StopAnswerAudio() 
    { 
        if (currentAudioDownloadRoutine != null)
        {
            StopCoroutine(currentAudioDownloadRoutine);
            currentAudioDownloadRoutine = null;
        }

        if (answerAudioSource != null) 
        { 
            answerAudioSource.Stop(); 
            answerAudioSource.clip = null; 
        } 
        
        if (lipSyncCoroutine != null) 
        {
            StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = null;
        }

        SetAllAvatarAnimatorsTalk(false);
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
        if (isScrollViewGenerated) return; // Khóa chống tạo rác UI nhiều lần

        answerScrollRect = answerText.GetComponentInParent<ScrollRect>();
        if (answerScrollRect != null)
        {
            answerViewportRect = answerScrollRect.viewport != null ? answerScrollRect.viewport : answerScrollRect.GetComponent<RectTransform>();
            answerContentRect = answerScrollRect.content != null ? answerScrollRect.content : answerTextRect;
            isScrollViewGenerated = true;
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

        isScrollViewGenerated = true;
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

        if (answerScrollRect != null) answerScrollRect.verticalNormalizedPosition = 0f; 
    }

    private IEnumerator UpdateAnswerTextLayoutNextFrame()
    {
        yield return null;
        UpdateAnswerTextLayout();
    }
    
    private void SetAnswerPanelsVisible(bool visible) 
    { 
        foreach (var p in answerPanelObjects) if (p != null) p.SetActive(visible); 
    }
    
    private void SetStatus(string message) 
    { 
        if (statusText != null) statusText.text = message; 
    }
    
    private string CombineUrl(string baseUrl, string path) 
    { 
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'); 
    }

    private void SetAllAvatarAnimatorsTalk(bool isTalking)
    {
        Animator[] animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var anim in animators)
        {
            if (anim != null && anim.gameObject.activeInHierarchy && anim.GetComponentInParent<Canvas>() == null)
            {
                anim.SetBool("Talk", isTalking);
            }
        }
    }

    private IEnumerator HandleNpcLipSync()
    {
        SetAllAvatarAnimatorsTalk(true);

        while (answerAudioSource != null && answerAudioSource.isPlaying)
        {
            SetAllAvatarAnimatorsTalk(true);
            yield return new WaitForSeconds(0.08f);
        }

        SetAllAvatarAnimatorsTalk(false);
    }

    [Serializable] private class AskRequest { public string audioBase64; public string mimeType; public string npcName; public string targetName; public string context; public string imageBase64; public string imageMimeType; }
    [Serializable] private class AskResponse { public string transcript; public string reply; public string audioUrl; public string error; }
}

public static class WavUtility
{
    public static byte[] FromAudioClipTrimmed(AudioClip clip, int samplesRecordedPerChannel)
    {
        int channels = clip.channels;
        // Lấy đúng lượng data thay vì lấy toàn bộ
        int totalSamples = samplesRecordedPerChannel * channels;
        if (totalSamples <= 0) return new byte[44];

        float[] samples = new float[totalSamples];
        clip.GetData(samples, 0);

        byte[] wav = new byte[44 + (totalSamples * 2)];

        WriteAscii(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + (totalSamples * 2));
        WriteAscii(wav, 8, "WAVE");
        WriteAscii(wav, 12, "fmt ");
        WriteInt(wav, 16, 16);
        WriteShort(wav, 20, 1);
        WriteShort(wav, 22, (short)channels);
        WriteInt(wav, 24, clip.frequency);
        WriteInt(wav, 28, clip.frequency * channels * 2);
        WriteShort(wav, 32, (short)(channels * 2));
        WriteShort(wav, 34, 16);
        WriteAscii(wav, 36, "data");
        WriteInt(wav, 40, totalSamples * 2);

        int byteIndex = 44;
        for (int i = 0; i < totalSamples; i++)
        {
            short sampleVal = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            wav[byteIndex++] = (byte)(sampleVal & 0xFF);
            wav[byteIndex++] = (byte)((sampleVal >> 8) & 0xFF);
        }

        return wav;
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