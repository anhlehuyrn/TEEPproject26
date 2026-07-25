using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AIContextSwitcher : MonoBehaviour
{
    public ARTrackedImageManager imageManager;
    public AiNpcQuestionController aiController;

    void OnEnable() => imageManager.trackedImagesChanged += OnImageChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnImageChanged;

    void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            UpdateAIContext(trackedImage.referenceImage.name);
        }
        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                UpdateAIContext(trackedImage.referenceImage.name);
            }
        }
    }

    void UpdateAIContext(string imageName)
    {
        // Hệ thống "RAG nội bộ" chuyển đổi ngữ cảnh dựa vào tên ảnh
        switch (imageName)
        {
            case "VN_cloth":
                aiController.targetName = "Áo Tứ Thân";
                aiController.lessonContext = "You are explaining the Vietnamese Four-part dress (Áo Tứ Thân). It is a traditional garment representing cultural heritage and elegance.";
                break;
            case "VN_food":
                aiController.targetName = "Bánh Phu Thê";
                aiController.lessonContext = "You are explaining Phu The Cake (Husband and Wife Cake). It is a traditional Vietnamese sweet treat symbolizing loyalty and love in weddings.";
                break;
            case "VN_fest":
                aiController.targetName = "Hội Đồng Kỵ";
                aiController.lessonContext = "You are explaining the Dong Ky Firecracker Festival. It is a traditional Vietnamese festival honoring tutelary gods with drums, rituals, and giant wooden firecrackers.";
                break;
            case "DongHo":
                aiController.targetName = "DongHo";
                aiController.lessonContext = "You are explaining the Dong Ho folk painting to students.";
                break;
        }
    }
}