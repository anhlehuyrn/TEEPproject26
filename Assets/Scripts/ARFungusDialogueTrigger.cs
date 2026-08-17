using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Fungus;

public class ARFungusDialogueTrigger : MonoBehaviour
{
    [Serializable]
    public class ImageDialogue
    {
        public string imageName;
        public string blockName;
        public bool playOnlyOnce = true;
        [HideInInspector] public bool hasPlayed;
    }

    // --- BỔ SUNG: C# Events để báo cho UI biết ---
    public static event Action<string> OnDialogueStarted;
    public static event Action<string> OnDialogueEnded;

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private List<ImageDialogue> imageDialogues = new List<ImageDialogue>();
    
    [Header("Settings")]
    [SerializeField] private bool stopDialogueWhenTrackingLost = true;
    [SerializeField] private bool hideSayDialogWhenTrackingLost = true;
    [SerializeField] private bool restartDialogueWhenTrackingFound = true;
    [SerializeField] private float trackingStableDelay = 0.15f;
    [SerializeField] private int resetWaitFrames = 2;
    
    // --- BỔ SUNG: Thời gian khoan hồng chống nhấp nháy AR ---
    [SerializeField, Tooltip("Thời gian chờ trước khi ngắt hội thoại do mất dấu (giây)")] 
    private float lostTrackingGracePeriod = 0.8f;

    private readonly Dictionary<TrackableId, ImageDialogue> activeDialogues = new Dictionary<TrackableId, ImageDialogue>();
    private readonly Dictionary<TrackableId, Coroutine> pendingDialogues = new Dictionary<TrackableId, Coroutine>();
    private readonly Dictionary<TrackableId, TrackingState> trackingStates = new Dictionary<TrackableId, TrackingState>();
    
    // Lưu trữ các bộ đếm thời gian khi mất dấu
    private readonly Dictionary<TrackableId, Coroutine> lostTrackingCoroutines = new Dictionary<TrackableId, Coroutine>();
    private Coroutine resetCoroutine;

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage image in eventArgs.added) UpdateDialogueForImage(image);
        foreach (ARTrackedImage image in eventArgs.updated) UpdateDialogueForImage(image);
        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            // Bắt đầu đếm ngược thời gian khoan hồng khi ảnh bị gỡ
            HandleLostTracking(removedImage.Key, removedImage.Value.referenceImage.name);
        }
    }

    private void UpdateDialogueForImage(ARTrackedImage image)
    {
        trackingStates[image.trackableId] = image.trackingState;
        if (flowchart == null) return;

        ImageDialogue dialogue = FindDialogue(image.referenceImage.name);
        if (dialogue == null) return;

        // Xử lý chống nhấp nháy Tracking (Hysteresis)
        if (image.trackingState == TrackingState.Tracking)
        {
            // Nếu ảnh được tìm lại trong thời gian khoan hồng, hủy bộ đếm tắt
            if (lostTrackingCoroutines.TryGetValue(image.trackableId, out Coroutine lostCoroutine))
            {
                if (lostCoroutine != null) StopCoroutine(lostCoroutine);
                lostTrackingCoroutines.Remove(image.trackableId);
            }
        }
        else // TrackingState.Limited hoặc None
        {
            HandleLostTracking(image.trackableId, image.referenceImage.name);
            return;
        }

        if (activeDialogues.ContainsKey(image.trackableId)) return;
        PlayDialogue(image.trackableId, dialogue);
    }

    private void HandleLostTracking(TrackableId trackableId, string imageName)
    {
        if (!lostTrackingCoroutines.ContainsKey(trackableId))
        {
            lostTrackingCoroutines[trackableId] = StartCoroutine(LostTrackingRoutine(trackableId, imageName));
        }
    }

    private IEnumerator LostTrackingRoutine(TrackableId trackableId, string imageName)
    {
        // Chờ thời gian khoan hồng (Grace Period)
        yield return new WaitForSeconds(lostTrackingGracePeriod);
        
        lostTrackingCoroutines.Remove(trackableId);
        StopDialogueForImage(trackableId);
        
        // Báo cho hệ thống biết hội thoại đã bị ngắt
        OnDialogueEnded?.Invoke(imageName); 
    }

    private ImageDialogue FindDialogue(string scannedImageName)
    {
        foreach (ImageDialogue dialogue in imageDialogues)
            if (dialogue.imageName == scannedImageName) return dialogue;
        return null;
    }

    private void PlayDialogue(TrackableId trackableId, ImageDialogue dialogue)
    {
        if (dialogue.playOnlyOnce && dialogue.hasPlayed) return;
        if (pendingDialogues.ContainsKey(trackableId)) return;
        pendingDialogues[trackableId] = StartCoroutine(PlayDialogueAfterReset(trackableId, dialogue));
    }

    private IEnumerator PlayDialogueAfterReset(TrackableId trackableId, ImageDialogue dialogue)
    {
        if (restartDialogueWhenTrackingFound)
        {
            StopResetCoroutine();
            yield return ResetFungusDialogueState();
        }

        if (trackingStableDelay > 0f) yield return new WaitForSeconds(trackingStableDelay);

        pendingDialogues.Remove(trackableId);

        if (!IsTracking(trackableId)) yield break;
        if (dialogue.playOnlyOnce && dialogue.hasPlayed) yield break;

        if (flowchart != null && flowchart.ExecuteIfHasBlock(dialogue.blockName))
        {
            dialogue.hasPlayed = true;
            activeDialogues[trackableId] = dialogue;

            // --- BỔ SUNG: Phát tín hiệu Bắt đầu ---
            OnDialogueStarted?.Invoke(dialogue.imageName);

            // Bắt đầu theo dõi để phát tín hiệu Kết thúc
            StartCoroutine(MonitorDialogueCompletion(dialogue.imageName));
        }
    }

    // --- BỔ SUNG: Theo dõi tiến trình Fungus ---
    private IEnumerator MonitorDialogueCompletion(string imageName)
    {
        yield return null; // Đợi 1 frame để flowchart khởi động
        
        while (IsFlowchartExecuting())
        {
            yield return null;
        }
        
        OnDialogueEnded?.Invoke(imageName);
    }

    private bool IsFlowchartExecuting()
    {
        if (flowchart == null) return false;
        Block[] blocks = flowchart.GetComponents<Block>();
        foreach (Block block in blocks)
        {
            if (block != null && block.IsExecuting()) return true;
        }
        return false;
    }

    private void StopDialogueForImage(TrackableId trackableId)
    {
        if (pendingDialogues.TryGetValue(trackableId, out Coroutine pendingDialogue))
        {
            StopCoroutine(pendingDialogue);
            pendingDialogues.Remove(trackableId);
        }

        if (!activeDialogues.Remove(trackableId) && pendingDialogue == null) return;

        if (stopDialogueWhenTrackingLost) StartResetCoroutine();
    }

    private IEnumerator ResetFungusDialogueState()
    {
        if (flowchart != null) flowchart.StopAllBlocks();
        SayDialog sayDialog = SayDialog.ActiveSayDialog;
        if (sayDialog != null) sayDialog.Stop();

        int waitFrames = Mathf.Max(1, resetWaitFrames);
        for (int i = 0; i < waitFrames; i++) yield return null;

        if (flowchart != null) flowchart.Reset(true, false);
        if (sayDialog != null)
        {
            sayDialog.Clear();
            if (hideSayDialogWhenTrackingLost) sayDialog.SetActive(false);
        }
    }

    private bool IsTracking(TrackableId trackableId)
    {
        return trackingStates.TryGetValue(trackableId, out TrackingState state) && state == TrackingState.Tracking;
    }

    private void StartResetCoroutine()
    {
        StopResetCoroutine();
        resetCoroutine = StartCoroutine(ResetFungusDialogueStateAndClearReference());
    }

    private void StopResetCoroutine()
    {
        if (resetCoroutine == null) return;
        StopCoroutine(resetCoroutine);
        resetCoroutine = null;
    }

    private IEnumerator ResetFungusDialogueStateAndClearReference()
    {
        yield return ResetFungusDialogueState();
        resetCoroutine = null;
    }

    public void ResetAllDialogues()
    {
        foreach (ImageDialogue dialogue in imageDialogues) dialogue.hasPlayed = false;
        activeDialogues.Clear();
        foreach (Coroutine pendingDialogue in pendingDialogues.Values) StopCoroutine(pendingDialogue);
        pendingDialogues.Clear();
        StartResetCoroutine();
    }
}