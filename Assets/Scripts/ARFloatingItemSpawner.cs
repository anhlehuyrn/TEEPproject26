using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFloatingItemSpawner : MonoBehaviour
{
    [Serializable]
    public class TargetImageSet
    {
        [Tooltip("Must match the Reference Image Library name. Leave empty only if this set should be used as a fallback.")]
        public string targetImageName;
        public List<FloatingItem> floatingItems = new List<FloatingItem>();
    }

    [Serializable]
    public class FloatingItem
    {
        public string itemName;
        [TextArea(2, 5)] public string description;
        public Sprite transparentPngSprite;
        public List<Sprite> animatedSprites = new List<Sprite>();
        [Min(1f)] public float animationFps = 8f;

        [Header("Image Position")]
        public Vector2 imageLocalPositionMeters;
        [Min(0f)] public float imageSurfaceOffsetMeters = 0.005f;
        [Min(0.01f)] public float floatForwardMeters = 0.15f;

        [Header("Display")]
        [Min(0.01f)] public float widthMeters = 0.12f;
        public int sortingOrder = 20;

        [Header("Interaction")]
        [Min(0.1f)] public float minScaleMultiplier = 0.5f;
        [Min(0.1f)] public float maxScaleMultiplier = 2.5f;

        [Header("Description Text")]
        [Tooltip("Use familiar Unity-style values such as 16, 20, or 24. Values below 1 are treated as legacy world-size values.")]
        [Min(0.01f)] public float textFontSize = 16f;
        [Min(0.01f)] public float textBoxWidthMeters = 0.22f;
        [Min(0.01f)] public float textBoxHeightMeters = 0.12f;
        public float textLineSpacing = 0f;
        public Vector3 textLocalOffsetMeters = new Vector3(0f, -0.08f, 0f);

        [Header("Audio")]
        public AudioClip audioClip;
        [Range(0f, 1f)] public float audioVolume = 1f;
    }

    [Header("AR Image Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private bool hideWhenTrackingLost = true;
    [SerializeField] private bool showWhenTrackingFound = false;

    [Header("Target Image Sets")]
    [SerializeField] private List<TargetImageSet> targetImageSets = new List<TargetImageSet>();

    [Header("Animation")]
    [SerializeField] private float floatInSeconds = 0.7f;
    [SerializeField] private bool resetWhenTrackingFoundAgain = true;

    private const float BaseCameraDistanceMeters = 0.4f;
    private const float MinAutoScaleMultiplier = 0.5f;
    private const float MaxAutoScaleMultiplier = 8f;

    private readonly Dictionary<TrackableId, GameObject> spawnedRoots = new Dictionary<TrackableId, GameObject>();
    private readonly HashSet<TrackableId> trackingImages = new HashSet<TrackableId>();
    private bool explorationVisible;
    private Camera arCamera;

    private void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        }

        arCamera = Camera.main;
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
            UpdateFloatingItemsForImage(image);
        }

        foreach (ARTrackedImage image in eventArgs.updated)
        {
            UpdateFloatingItemsForImage(image);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            RemoveFloatingItems(removedImage.Key);
        }
    }

    private void UpdateFloatingItemsForImage(ARTrackedImage image)
    {
        TargetImageSet targetImageSet = FindTargetImageSet(image);
        if (targetImageSet == null || targetImageSet.floatingItems == null || targetImageSet.floatingItems.Count == 0)
        {
            return;
        }

        bool isTracking = image.trackingState == TrackingState.Tracking;
        GameObject root = GetOrCreateRoot(image, targetImageSet);
        bool shouldShow = isTracking && (showWhenTrackingFound || explorationVisible);

        if (hideWhenTrackingLost)
        {
            root.SetActive(shouldShow);
        }

        if (!isTracking)
        {
            trackingImages.Remove(image.trackableId);
            return;
        }

        bool wasTracking = trackingImages.Contains(image.trackableId);
        trackingImages.Add(image.trackableId);

        root.transform.SetPositionAndRotation(image.transform.position, image.transform.rotation);
        root.transform.localScale = Vector3.one * GetTrackedImageScaleMultiplier(image);

        if (resetWhenTrackingFoundAgain && !wasTracking)
        {
            foreach (FloatingItemInteractable interactable in root.GetComponentsInChildren<FloatingItemInteractable>(true))
            {
                if (shouldShow)
                {
                    interactable.ResetForTrackingFound();
                }
                else
                {
                    interactable.ResetForExplorationHidden();
                }
            }
        }
    }

    public void ShowFloatingItems()
    {
        explorationVisible = true;
        RefreshTrackedImages();
        ResetVisibleFloatingItemsForTrackingFound();
    }

    public void HideFloatingItems()
    {
        explorationVisible = false;

        foreach (GameObject root in spawnedRoots.Values)
        {
            if (root == null)
            {
                continue;
            }

            foreach (FloatingItemInteractable interactable in root.GetComponentsInChildren<FloatingItemInteractable>(true))
            {
                interactable.ResetForExplorationHidden();
            }

            root.SetActive(false);
        }
    }

    private void RefreshTrackedImages()
    {
        if (trackedImageManager == null)
        {
            return;
        }

        foreach (ARTrackedImage image in trackedImageManager.trackables)
        {
            UpdateFloatingItemsForImage(image);
        }
    }

    private void ResetVisibleFloatingItemsForTrackingFound()
    {
        foreach (GameObject root in spawnedRoots.Values)
        {
            if (root == null || !root.activeInHierarchy)
            {
                continue;
            }

            foreach (FloatingItemInteractable interactable in root.GetComponentsInChildren<FloatingItemInteractable>(true))
            {
                interactable.ResetForTrackingFound();
            }
        }
    }

    private TargetImageSet FindTargetImageSet(ARTrackedImage image)
    {
        if (image == null)
        {
            return null;
        }

        string scannedImageName = image.referenceImage.name;
        TargetImageSet fallbackSet = null;

        foreach (TargetImageSet targetImageSet in targetImageSets)
        {
            if (targetImageSet == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(targetImageSet.targetImageName))
            {
                fallbackSet = targetImageSet;
                continue;
            }

            if (targetImageSet.targetImageName == scannedImageName)
            {
                return targetImageSet;
            }
        }

        return fallbackSet;
    }

    private float GetTrackedImageScaleMultiplier(ARTrackedImage image)
    {
        if (image == null)
        {
            return 1f;
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            return 1f;
        }

        float cameraDistance = Vector3.Distance(arCamera.transform.position, image.transform.position);
        float scaleMultiplier = cameraDistance / BaseCameraDistanceMeters;
        return Mathf.Clamp(scaleMultiplier, MinAutoScaleMultiplier, MaxAutoScaleMultiplier);
    }

    private GameObject GetOrCreateRoot(ARTrackedImage image, TargetImageSet targetImageSet)
    {
        if (spawnedRoots.TryGetValue(image.trackableId, out GameObject root) && root != null)
        {
            return root;
        }

        root = new GameObject("FloatingItems_" + image.referenceImage.name);
        root.transform.SetPositionAndRotation(image.transform.position, image.transform.rotation);
        spawnedRoots[image.trackableId] = root;

        CreateFloatingItems(root.transform, targetImageSet);
        return root;
    }

    private void CreateFloatingItems(Transform root, TargetImageSet targetImageSet)
    {
        if (targetImageSet == null || targetImageSet.floatingItems == null)
        {
            return;
        }

        foreach (FloatingItem floatingItem in targetImageSet.floatingItems)
        {
            Sprite displaySprite = GetDisplaySprite(floatingItem);
            if (floatingItem == null || displaySprite == null)
            {
                continue;
            }

            GameObject itemObject = new GameObject(string.IsNullOrWhiteSpace(floatingItem.itemName)
                ? "FloatingItem"
                : floatingItem.itemName);
            itemObject.transform.SetParent(root, false);

            Vector3 startLocalPosition = new Vector3(
                0f,
                floatingItem.imageSurfaceOffsetMeters,
                0f);
            Vector3 targetImageLocalPosition = new Vector3(
                floatingItem.imageLocalPositionMeters.x,
                floatingItem.imageSurfaceOffsetMeters,
                floatingItem.imageLocalPositionMeters.y);
            Vector3 restLocalPosition = targetImageLocalPosition + new Vector3(0f, floatingItem.floatForwardMeters, 0f);

            itemObject.transform.localPosition = startLocalPosition;

            SpriteRenderer spriteRenderer = itemObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = displaySprite;
            spriteRenderer.sortingOrder = floatingItem.sortingOrder;

            float objectScale = FitSpriteWidth(itemObject.transform, displaySprite, floatingItem.widthMeters);

            BoxCollider collider = itemObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                displaySprite.bounds.size.x,
                displaySprite.bounds.size.y,
                0.02f / Mathf.Max(0.0001f, objectScale));

            TextMeshPro label = CreateDescriptionLabel(itemObject.transform, floatingItem, objectScale);
            AudioSource audioSource = itemObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.clip = floatingItem.audioClip;
            audioSource.volume = floatingItem.audioVolume;

            FloatingItemInteractable interactable = itemObject.AddComponent<FloatingItemInteractable>();
            interactable.Initialize(
                spriteRenderer,
                label,
                audioSource,
                GetAnimationSprites(floatingItem),
                floatingItem.animationFps,
                floatingItem.minScaleMultiplier,
                floatingItem.maxScaleMultiplier,
                startLocalPosition,
                restLocalPosition,
                floatInSeconds);
        }
    }

    private static TextMeshPro CreateDescriptionLabel(Transform parent, FloatingItem floatingItem, float parentScale)
    {
        GameObject labelObject = new GameObject("Description");
        labelObject.transform.SetParent(parent, false);
        float safeScale = Mathf.Max(0.0001f, parentScale);
        labelObject.transform.localPosition = floatingItem.textLocalOffsetMeters / safeScale;

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = floatingItem.description;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = ConvertTextFontSize(floatingItem.textFontSize) / safeScale;
        label.color = Color.white;
        label.enableWordWrapping = true;
        label.lineSpacing = floatingItem.textLineSpacing;
        label.rectTransform.sizeDelta = new Vector2(
            floatingItem.textBoxWidthMeters / safeScale,
            floatingItem.textBoxHeightMeters / safeScale);
        label.gameObject.SetActive(false);
        return label;
    }

    private static Sprite GetDisplaySprite(FloatingItem floatingItem)
    {
        if (floatingItem == null)
        {
            return null;
        }

        List<Sprite> animationSprites = GetAnimationSprites(floatingItem);
        if (animationSprites.Count > 0)
        {
            return animationSprites[0];
        }

        return floatingItem.transparentPngSprite;
    }

    private static List<Sprite> GetAnimationSprites(FloatingItem floatingItem)
    {
        if (floatingItem == null || floatingItem.animatedSprites == null)
        {
            return new List<Sprite>();
        }

        List<Sprite> validSprites = new List<Sprite>();
        foreach (Sprite sprite in floatingItem.animatedSprites)
        {
            if (sprite != null)
            {
                validSprites.Add(sprite);
            }
        }

        return validSprites;
    }

    private static float ConvertTextFontSize(float textFontSize)
    {
        if (textFontSize < 1f)
        {
            return textFontSize;
        }

        return textFontSize * 0.01f;
    }

    private static float FitSpriteWidth(Transform transformToScale, Sprite sprite, float widthMeters)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f)
        {
            return 1f;
        }

        float scale = widthMeters / sprite.bounds.size.x;
        transformToScale.localScale = Vector3.one * scale;
        return scale;
    }

    private static float GetSpriteHeightMeters(Sprite sprite, float widthMeters)
    {
        if (sprite == null || sprite.bounds.size.x <= 0f)
        {
            return widthMeters;
        }

        return widthMeters * sprite.bounds.size.y / sprite.bounds.size.x;
    }

    private void RemoveFloatingItems(TrackableId trackableId)
    {
        trackingImages.Remove(trackableId);

        if (!spawnedRoots.TryGetValue(trackableId, out GameObject root))
        {
            return;
        }

        if (root != null)
        {
            Destroy(root);
        }

        spawnedRoots.Remove(trackableId);
    }
}
