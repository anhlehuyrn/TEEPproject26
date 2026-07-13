using System.Collections;
using System.Collections.Generic;
using Fungus;
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
    [SerializeField] private bool showOnlyOncePerScene = true;

    [Header("Fungus Dialogue")]
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private bool waitForFungusDialogueToFinish = true;
    [SerializeField, Min(0f)] private float waitForDialogueToStartSeconds = 2f;
    [SerializeField, Min(0f)] private float delayAfterDialogueEnds = 0.5f;

    [Header("AI UI")]
    [SerializeField] private GameObject[] aiUiObjects;
    [SerializeField] private Text[] textsToClearWhenHidden;
    [SerializeField] private AiNpcQuestionController[] aiQuestionControllers;

    private Coroutine revealUiCoroutine;
    private bool hasShownUi;
    private readonly HashSet<TrackableId> trackingTargetImages = new HashSet<TrackableId>();

    private void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = FindAnyObjectByType<ARTrackedImageManager>();
        }

        CacheAiQuestionControllersIfNeeded();

        HideAiUi();
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

        StopRevealCoroutine();
        trackingTargetImages.Clear();
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage image in eventArgs.added)
        {
            UpdateUiForImage(image);
        }

        foreach (ARTrackedImage image in eventArgs.updated)
        {
            UpdateUiForImage(image);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            if (trackingTargetImages.Remove(removedImage.Key) && hideUiWhenTrackingLost && trackingTargetImages.Count == 0)
            {
                StopRevealCoroutine();
                ResetAiUiAndQuestionSession();
            }
        }
    }

    private void UpdateUiForImage(ARTrackedImage image)
    {
        if (!IsTargetImage(image))
        {
            return;
        }

        if (image.trackingState != TrackingState.Tracking)
        {
            trackingTargetImages.Remove(image.trackableId);

            if (hideUiWhenTrackingLost && trackingTargetImages.Count == 0)
            {
                StopRevealCoroutine();
                ResetAiUiAndQuestionSession();
            }

            return;
        }

        trackingTargetImages.Add(image.trackableId);

        if (showOnlyOncePerScene && hasShownUi)
        {
            return;
        }

        if (revealUiCoroutine == null)
        {
            revealUiCoroutine = StartCoroutine(RevealAiUiAfterDialogueRoutine());
        }
    }

    private IEnumerator RevealAiUiAfterDialogueRoutine()
    {
        if (waitForFungusDialogueToFinish && flowchart != null)
        {
            float startTimer = 0f;
            while (!IsFlowchartExecuting() && startTimer < waitForDialogueToStartSeconds)
            {
                startTimer += Time.deltaTime;
                yield return null;
            }

            while (IsFlowchartExecuting())
            {
                yield return null;
            }
        }

        if (delayAfterDialogueEnds > 0f)
        {
            yield return new WaitForSeconds(delayAfterDialogueEnds);
        }

        ShowAiUi();
        hasShownUi = true;
        revealUiCoroutine = null;
    }

    private bool IsTargetImage(ARTrackedImage image)
    {
        if (string.IsNullOrWhiteSpace(targetImageName))
        {
            return true;
        }

        return image.referenceImage.name == targetImageName;
    }

    private bool IsFlowchartExecuting()
    {
        if (flowchart == null)
        {
            return false;
        }

        Block[] blocks = flowchart.GetComponents<Block>();
        foreach (Block block in blocks)
        {
            if (block != null && block.IsExecuting())
            {
                return true;
            }
        }

        return false;
    }

    private void ShowAiUi()
    {
        SetAiUiVisible(true);
    }

    private void HideAiUi()
    {
        SetAiUiVisible(false);

        foreach (Text text in textsToClearWhenHidden)
        {
            if (text != null)
            {
                text.text = "";
            }
        }
    }

    private void ResetAiUiAndQuestionSession()
    {
        hasShownUi = false;
        HideAiUi();
        ResetAiQuestionControllers();
    }

    private void SetAiUiVisible(bool visible)
    {
        foreach (GameObject uiObject in aiUiObjects)
        {
            if (uiObject != null)
            {
                uiObject.SetActive(visible);
            }
        }
    }

    private void StopRevealCoroutine()
    {
        if (revealUiCoroutine == null)
        {
            return;
        }

        StopCoroutine(revealUiCoroutine);
        revealUiCoroutine = null;
    }

    private void CacheAiQuestionControllersIfNeeded()
    {
        if (aiQuestionControllers != null && aiQuestionControllers.Length > 0)
        {
            return;
        }

        aiQuestionControllers = FindObjectsByType<AiNpcQuestionController>(FindObjectsSortMode.None);
    }

    private void ResetAiQuestionControllers()
    {
        CacheAiQuestionControllersIfNeeded();

        foreach (AiNpcQuestionController controller in aiQuestionControllers)
        {
            if (controller != null)
            {
                controller.ResetQuestionSession();
            }
        }
    }
}
