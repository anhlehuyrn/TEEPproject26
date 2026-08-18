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
    
    [SerializeField, Tooltip("Grace period before terminating dialogue")] 
    private float lostTrackingGracePeriod = 2.5f;

    private readonly Dictionary<TrackableId, ImageDialogue> activeDialogues = new Dictionary<TrackableId, ImageDialogue>();
    private readonly Dictionary<TrackableId, Coroutine> pendingDialogues = new Dictionary<TrackableId, Coroutine>();
    private readonly Dictionary<TrackableId, TrackingState> trackingStates = new Dictionary<TrackableId, TrackingState>();
    private readonly Dictionary<TrackableId, Coroutine> lostTrackingCoroutines = new Dictionary<TrackableId, Coroutine>();
    
    private Coroutine monitorCompletionCoroutine;
    private Coroutine resetCoroutine;
    private Block[] cachedBlocks;

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = GetComponent<ARTrackedImageManager>();

        if (flowchart != null)
            cachedBlocks = flowchart.GetComponents<Block>();
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

        StopAllCoroutines();
        pendingDialogues.Clear();
        lostTrackingCoroutines.Clear();
        activeDialogues.Clear();
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage image in eventArgs.added) UpdateDialogueForImage(image);
        foreach (ARTrackedImage image in eventArgs.updated) UpdateDialogueForImage(image);
        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            HandleLostTracking(removedImage.Key, removedImage.Value.referenceImage.name);
        }
    }

    private void UpdateDialogueForImage(ARTrackedImage image)
    {
        trackingStates[image.trackableId] = image.trackingState;
        if (flowchart == null) return;

        ImageDialogue dialogue = FindDialogue(image.referenceImage.name);
        if (dialogue == null) return;

        if (image.trackingState == TrackingState.Tracking)
        {
            if (lostTrackingCoroutines.TryGetValue(image.trackableId, out Coroutine lostCoroutine))
            {
                if (lostCoroutine != null) StopCoroutine(lostCoroutine);
                lostTrackingCoroutines.Remove(image.trackableId);
            }
        }
        else
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
        yield return new WaitForSeconds(lostTrackingGracePeriod);
        
        lostTrackingCoroutines.Remove(trackableId);
        
        StopDialogueForImage(trackableId);
        
        OnDialogueEnded?.Invoke(imageName);
    }

    private ImageDialogue FindDialogue(string scannedImageName)
    {
        for (int i = 0; i < imageDialogues.Count; i++)
        {
            if (imageDialogues[i].imageName == scannedImageName) return imageDialogues[i];
        }
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

            OnDialogueStarted?.Invoke(dialogue.imageName);

            if (monitorCompletionCoroutine != null) StopCoroutine(monitorCompletionCoroutine);
            monitorCompletionCoroutine = StartCoroutine(MonitorDialogueCompletion(dialogue.imageName, trackableId));
        }
    }

    private IEnumerator MonitorDialogueCompletion(string imageName, TrackableId trackableId)
    {
        yield return new WaitForSeconds(0.25f); 
        
        while (IsFlowchartExecuting())
        {
            yield return null;
        }
        
        activeDialogues.Remove(trackableId);
        monitorCompletionCoroutine = null;
        OnDialogueEnded?.Invoke(imageName);
    }

    private bool IsFlowchartExecuting()
    {
        if (cachedBlocks == null || cachedBlocks.Length == 0) return false;
        for (int i = 0; i < cachedBlocks.Length; i++)
        {
            if (cachedBlocks[i] != null && cachedBlocks[i].IsExecuting()) return true;
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

        activeDialogues.Remove(trackableId);

        // BẢN VÁ LỖI CỐT LÕI: Chỉ giết kịch bản Fungus nếu KHÔNG CÒN bóng ma (ID) nào khác đang chạy
        if (activeDialogues.Count == 0 && pendingDialogues.Count == 0)
        {
            if (monitorCompletionCoroutine != null)
            {
                StopCoroutine(monitorCompletionCoroutine);
                monitorCompletionCoroutine = null;
            }

            if (stopDialogueWhenTrackingLost) StartResetCoroutine();
        }
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
        for (int i = 0; i < imageDialogues.Count; i++) imageDialogues[i].hasPlayed = false;
        activeDialogues.Clear();
        foreach (Coroutine pending in pendingDialogues.Values) StopCoroutine(pending);
        pendingDialogues.Clear();
        StartResetCoroutine();
    }
}