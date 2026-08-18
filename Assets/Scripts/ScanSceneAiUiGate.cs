using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ScanSceneAiUiGate : MonoBehaviour
{
    [Header("AR Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private string targetImageName;
    [SerializeField] private bool hideUiWhenTrackingLost = true;
    [SerializeField] private bool showOnlyOncePerScene = false; // Set to false to allow re-entry
    
    [SerializeField, Tooltip("Grace period before hiding UI when tracking is lost")] 
    private float lostTrackingGracePeriod = 0.8f;

    [Header("Fungus Dialogue Timing")]
    [SerializeField, Min(0f)] private float delayAfterDialogueEnds = 0.5f;

    [Header("AI UI")]
    [SerializeField] private GameObject[] readyUiObjects;
    [SerializeField] private GameObject[] answerUiObjects;
    [SerializeField] private GameObject[] aiUiObjects;
    [SerializeField] private Text[] textsToClearWhenHidden;
    [SerializeField] private AiNpcQuestionController[] aiQuestionControllers;

    private Coroutine revealUiCoroutine;
    private Coroutine lostTrackingCoroutine;
    private bool hasDialoguePlayedForCurrentTarget = false;
    private readonly HashSet<TrackableId> trackingTargetImages = new HashSet<TrackableId>();

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();

        CacheAiQuestionControllers();
        HideAiUi();
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

        ARFungusDialogueTrigger.OnDialogueStarted += HandleDialogueStarted;
        ARFungusDialogueTrigger.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

        ARFungusDialogueTrigger.OnDialogueStarted -= HandleDialogueStarted;
        ARFungusDialogueTrigger.OnDialogueEnded -= HandleDialogueEnded;

        StopRevealCoroutine();
        if (lostTrackingCoroutine != null) StopCoroutine(lostTrackingCoroutine);
        trackingTargetImages.Clear();
    }

    private void HandleDialogueStarted(string targetName)
    {
        if (!IsTargetImageName(targetName)) return;
        StopRevealCoroutine();
        HideAiUi();
    }

    private void HandleDialogueEnded(string targetName)
    {
        if (!IsTargetImageName(targetName)) return;
        hasDialoguePlayedForCurrentTarget = true;

        StopRevealCoroutine();
        if (delayAfterDialogueEnds > 0f)
        {
            revealUiCoroutine = StartCoroutine(ShowAiUiDelayed());
        }
        else
        {
            ShowAiUi();
        }
    }

    private IEnumerator ShowAiUiDelayed()
    {
        yield return new WaitForSeconds(delayAfterDialogueEnds);
        ShowAiUi();
        revealUiCoroutine = null;
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage image in eventArgs.added) UpdateUiForImage(image);
        foreach (ARTrackedImage image in eventArgs.updated) UpdateUiForImage(image);
        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            if (trackingTargetImages.Remove(removedImage.Key) && hideUiWhenTrackingLost && trackingTargetImages.Count == 0)
            {
                TriggerLostTrackingGracePeriod();
            }
        }
    }

    private void UpdateUiForImage(ARTrackedImage image)
    {
        if (!IsTargetImageName(image.referenceImage.name)) return;

        if (image.trackingState == TrackingState.Tracking)
        {
            trackingTargetImages.Add(image.trackableId);
            SetCurrentScannedTargetName(image.referenceImage.name);

            if (lostTrackingCoroutine != null)
            {
                StopCoroutine(lostTrackingCoroutine);
                lostTrackingCoroutine = null;
            }

            // CRITICAL FIX: If dialogue already played previously, restore AI UI on tracking re-acquisition
            if (hasDialoguePlayedForCurrentTarget && !HasVisibleAiAnswer() && !IsReadyUiActive())
            {
                ShowAiUi();
            }
        }
        else
        {
            trackingTargetImages.Remove(image.trackableId);
            if (hideUiWhenTrackingLost && trackingTargetImages.Count == 0)
            {
                TriggerLostTrackingGracePeriod();
            }
        }
    }

    private void TriggerLostTrackingGracePeriod()
    {
        if (lostTrackingCoroutine == null)
        {
            lostTrackingCoroutine = StartCoroutine(LostTrackingRoutine());
        }
    }

    private IEnumerator LostTrackingRoutine()
    {
        yield return new WaitForSeconds(lostTrackingGracePeriod);
        
        lostTrackingCoroutine = null;
        StopRevealCoroutine();
        ResetAiUiAndQuestionSession();
    }

    private bool IsTargetImageName(string imgName)
    {
        if (string.IsNullOrWhiteSpace(targetImageName)) return true;
        return imgName == targetImageName;
    }

    private void ShowAiUi()
    {
        SetReadyUiVisible(true);
        if (!HasVisibleAiAnswer()) SetAnswerUiVisible(false);
    }

    private void HideAiUi()
    {
        SetReadyUiVisible(false);
        SetAnswerUiVisible(false);

        if (textsToClearWhenHidden != null)
        {
            for (int i = 0; i < textsToClearWhenHidden.Length; i++)
            {
                if (textsToClearWhenHidden[i] != null) textsToClearWhenHidden[i].text = "";
            }
        }
    }

    private void ResetAiUiAndQuestionSession()
    {
        if (HasVisibleAiAnswer()) return; // Protect active session

        HideAiUi();
        ResetAiQuestionControllers();
    }

    private bool IsReadyUiActive()
    {
        GameObject[] objectsToSet = readyUiObjects != null && readyUiObjects.Length > 0 ? readyUiObjects : aiUiObjects;
        if (objectsToSet != null && objectsToSet.Length > 0 && objectsToSet[0] != null)
            return objectsToSet[0].activeSelf;
        return false;
    }

    private void SetReadyUiVisible(bool visible)
    {
        GameObject[] objectsToSet = readyUiObjects != null && readyUiObjects.Length > 0 ? readyUiObjects : aiUiObjects;
        if (objectsToSet == null) return;
        for (int i = 0; i < objectsToSet.Length; i++)
        {
            if (objectsToSet[i] != null) objectsToSet[i].SetActive(visible);
        }
    }

    private void SetAnswerUiVisible(bool visible)
    {
        if (answerUiObjects == null) return;
        for (int i = 0; i < answerUiObjects.Length; i++)
        {
            if (answerUiObjects[i] != null) answerUiObjects[i].SetActive(visible);
        }
    }

    private bool HasVisibleAiAnswer()
    {
        CacheAiQuestionControllers();
        for (int i = 0; i < aiQuestionControllers.Length; i++)
        {
            if (aiQuestionControllers[i] != null && aiQuestionControllers[i].HasVisibleAnswer) return true;
        }
        return false;
    }

    private void StopRevealCoroutine()
    {
        if (revealUiCoroutine == null) return;
        StopCoroutine(revealUiCoroutine);
        revealUiCoroutine = null;
    }

    private void CacheAiQuestionControllers()
    {
        if (aiQuestionControllers == null || aiQuestionControllers.Length == 0)
        {
            aiQuestionControllers = FindObjectsByType<AiNpcQuestionController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }

    private void ResetAiQuestionControllers()
    {
        CacheAiQuestionControllers();
        for (int i = 0; i < aiQuestionControllers.Length; i++)
        {
            if (aiQuestionControllers[i] != null) aiQuestionControllers[i].ResetQuestionSession();
        }
    }

    private void SetCurrentScannedTargetName(string scannedTargetName)
    {
        CacheAiQuestionControllers();
        for (int i = 0; i < aiQuestionControllers.Length; i++)
        {
            if (aiQuestionControllers[i] != null) aiQuestionControllers[i].SetCurrentScannedTargetName(scannedTargetName);
        }
    }
}