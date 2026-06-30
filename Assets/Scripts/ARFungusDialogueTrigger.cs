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

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private List<ImageDialogue> imageDialogues = new List<ImageDialogue>();
    [SerializeField] private bool stopDialogueWhenTrackingLost = true;
    [SerializeField] private bool hideSayDialogWhenTrackingLost = true;
    [SerializeField] private bool restartDialogueWhenTrackingFound = true;
    [SerializeField] private float trackingStableDelay = 0.15f;
    [SerializeField] private int resetWaitFrames = 2;

    private readonly Dictionary<TrackableId, ImageDialogue> activeDialogues = new Dictionary<TrackableId, ImageDialogue>();
    private readonly Dictionary<TrackableId, Coroutine> pendingDialogues = new Dictionary<TrackableId, Coroutine>();
    private readonly Dictionary<TrackableId, TrackingState> trackingStates = new Dictionary<TrackableId, TrackingState>();
    private Coroutine resetCoroutine;

    private void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage image in eventArgs.added)
        {
            UpdateDialogueForImage(image);
        }

        foreach (ARTrackedImage image in eventArgs.updated)
        {
            UpdateDialogueForImage(image);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            StopDialogueForImage(removedImage.Key);
            trackingStates.Remove(removedImage.Key);
        }
    }

    private void UpdateDialogueForImage(ARTrackedImage image)
    {
        trackingStates[image.trackableId] = image.trackingState;

        if (flowchart == null)
        {
            return;
        }

        ImageDialogue dialogue = FindDialogue(image.referenceImage.name);
        if (dialogue == null)
        {
            return;
        }

        if (image.trackingState != TrackingState.Tracking)
        {
            StopDialogueForImage(image.trackableId);
            return;
        }

        if (activeDialogues.ContainsKey(image.trackableId))
        {
            return;
        }

        PlayDialogue(image.trackableId, dialogue);
    }

    private ImageDialogue FindDialogue(string scannedImageName)
    {
        foreach (ImageDialogue dialogue in imageDialogues)
        {
            if (dialogue.imageName != scannedImageName)
            {
                continue;
            }

            return dialogue;
        }

        return null;
    }

    private void PlayDialogue(TrackableId trackableId, ImageDialogue dialogue)
    {
        if (dialogue.playOnlyOnce && dialogue.hasPlayed)
        {
            return;
        }

        if (pendingDialogues.ContainsKey(trackableId))
        {
            return;
        }

        pendingDialogues[trackableId] = StartCoroutine(PlayDialogueAfterReset(trackableId, dialogue));
    }

    private IEnumerator PlayDialogueAfterReset(TrackableId trackableId, ImageDialogue dialogue)
    {
        if (restartDialogueWhenTrackingFound)
        {
            StopResetCoroutine();
            yield return ResetFungusDialogueState();
        }

        if (trackingStableDelay > 0f)
        {
            yield return new WaitForSeconds(trackingStableDelay);
        }

        pendingDialogues.Remove(trackableId);

        if (!IsTracking(trackableId))
        {
            yield break;
        }

        if (dialogue.playOnlyOnce && dialogue.hasPlayed)
        {
            yield break;
        }

        if (flowchart != null && flowchart.ExecuteIfHasBlock(dialogue.blockName))
        {
            dialogue.hasPlayed = true;
            activeDialogues[trackableId] = dialogue;
        }
    }

    private void StopDialogueForImage(TrackableId trackableId)
    {
        if (pendingDialogues.TryGetValue(trackableId, out Coroutine pendingDialogue))
        {
            StopCoroutine(pendingDialogue);
            pendingDialogues.Remove(trackableId);
        }

        if (!activeDialogues.Remove(trackableId) && pendingDialogue == null)
        {
            return;
        }

        if (stopDialogueWhenTrackingLost)
        {
            StartResetCoroutine();
        }
    }

    private IEnumerator ResetFungusDialogueState()
    {
        if (flowchart != null)
        {
            flowchart.StopAllBlocks();
        }

        SayDialog sayDialog = SayDialog.ActiveSayDialog;
        if (sayDialog != null)
        {
            sayDialog.Stop();
        }

        int waitFrames = Mathf.Max(1, resetWaitFrames);
        for (int i = 0; i < waitFrames; i++)
        {
            yield return null;
        }

        if (flowchart != null)
        {
            flowchart.Reset(true, false);
        }

        if (sayDialog != null)
        {
            sayDialog.Clear();

            if (hideSayDialogWhenTrackingLost)
            {
                sayDialog.SetActive(false);
            }
        }
    }

    private bool IsTracking(TrackableId trackableId)
    {
        return trackingStates.TryGetValue(trackableId, out TrackingState state)
            && state == TrackingState.Tracking;
    }

    private void StartResetCoroutine()
    {
        StopResetCoroutine();
        resetCoroutine = StartCoroutine(ResetFungusDialogueStateAndClearReference());
    }

    private void StopResetCoroutine()
    {
        if (resetCoroutine == null)
        {
            return;
        }

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
        foreach (ImageDialogue dialogue in imageDialogues)
        {
            dialogue.hasPlayed = false;
        }

        activeDialogues.Clear();

        foreach (Coroutine pendingDialogue in pendingDialogues.Values)
        {
            StopCoroutine(pendingDialogue);
        }

        pendingDialogues.Clear();
        StartResetCoroutine();
    }
}
