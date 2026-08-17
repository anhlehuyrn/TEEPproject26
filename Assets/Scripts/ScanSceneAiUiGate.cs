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
    [SerializeField] private bool showOnlyOncePerScene = true;
    
    // --- BỔ SUNG: Thời gian khoan hồng chống nhấp nháy UI ---
    [SerializeField, Tooltip("Độ trễ trước khi giấu UI đi do rớt AR")] 
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
    private bool hasShownUi;
    private readonly HashSet<TrackableId> trackingTargetImages = new HashSet<TrackableId>();

    private void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        CacheAiQuestionControllersIfNeeded();
        HideAiUi();
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

        // --- BỔ SUNG: Đăng ký nhận tín hiệu Event từ Fungus ---
        ARFungusDialogueTrigger.OnDialogueStarted += HandleDialogueStarted;
        ARFungusDialogueTrigger.OnDialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

        // Hủy đăng ký Event
        ARFungusDialogueTrigger.OnDialogueStarted -= HandleDialogueStarted;
        ARFungusDialogueTrigger.OnDialogueEnded -= HandleDialogueEnded;

        StopRevealCoroutine();
        if (lostTrackingCoroutine != null) StopCoroutine(lostTrackingCoroutine);
        trackingTargetImages.Clear();
    }

    // Lắng nghe khi hội thoại bắt đầu -> Giấu UI
    private void HandleDialogueStarted(string targetName)
    {
        if (!IsTargetImageName(targetName)) return;
        HideAiUi();
    }

    // Lắng nghe khi hội thoại kết thúc -> Hiện UI lên
    private void HandleDialogueEnded(string targetName)
    {
        if (!IsTargetImageName(targetName)) return;
        if (showOnlyOncePerScene && hasShownUi) return;

        if (delayAfterDialogueEnds > 0f)
        {
            revealUiCoroutine = StartCoroutine(ShowAiUiDelayed());
        }
        else
        {
            ShowAiUi();
            hasShownUi = true;
        }
    }

    private IEnumerator ShowAiUiDelayed()
    {
        yield return new WaitForSeconds(delayAfterDialogueEnds);
        ShowAiUi();
        hasShownUi = true;
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

            // Tìm lại được hình ảnh -> Hủy lệnh ẩn UI
            if (lostTrackingCoroutine != null)
            {
                StopCoroutine(lostTrackingCoroutine);
                lostTrackingCoroutine = null;
            }
        }
        else // TrackingState.Limited hoặc None
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
        // Khoan hồng 0.8s trước khi dọn dẹp UI
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

        foreach (Text text in textsToClearWhenHidden ?? new Text[0])
        {
            if (text != null) text.text = "";
        }
    }

    private void ResetAiUiAndQuestionSession()
    {
        // --- BỔ SUNG QUAN TRỌNG: Bảo vệ Micro ---
        // Nếu người dùng đang tương tác với AI (bảng trả lời đang bật / đang ghi âm),
        // tuyệt đối KHÔNG ĐƯỢC RESET phiên hỏi đáp khi camera bị rớt AR.
        if (HasVisibleAiAnswer()) return;

        hasShownUi = false;
        HideAiUi();
        ResetAiQuestionControllers();
    }

    private void SetReadyUiVisible(bool visible)
    {
        GameObject[] objectsToSet = readyUiObjects != null && readyUiObjects.Length > 0 ? readyUiObjects : aiUiObjects;
        foreach (GameObject uiObject in objectsToSet ?? new GameObject[0])
            if (uiObject != null) uiObject.SetActive(visible);
    }

    private void SetAnswerUiVisible(bool visible)
    {
        foreach (GameObject uiObject in answerUiObjects ?? new GameObject[0])
            if (uiObject != null) uiObject.SetActive(visible);
    }

    private bool HasVisibleAiAnswer()
    {
        CacheAiQuestionControllersIfNeeded();
        foreach (AiNpcQuestionController controller in aiQuestionControllers)
        {
            if (controller != null && controller.HasVisibleAnswer) return true;
        }
        return false;
    }

    private void StopRevealCoroutine()
    {
        if (revealUiCoroutine == null) return;
        StopCoroutine(revealUiCoroutine);
        revealUiCoroutine = null;
    }

    private void CacheAiQuestionControllersIfNeeded()
    {
        if (aiQuestionControllers != null && aiQuestionControllers.Length > 0) return;
        aiQuestionControllers = FindObjectsOfType<AiNpcQuestionController>(true);
    }

    private void ResetAiQuestionControllers()
    {
        CacheAiQuestionControllersIfNeeded();
        foreach (AiNpcQuestionController controller in aiQuestionControllers)
        {
            if (controller != null) controller.ResetQuestionSession();
        }
    }

    private void SetCurrentScannedTargetName(string scannedTargetName)
    {
        CacheAiQuestionControllersIfNeeded();
        foreach (AiNpcQuestionController controller in aiQuestionControllers)
        {
            if (controller != null) controller.SetCurrentScannedTargetName(scannedTargetName);
        }
    }
}