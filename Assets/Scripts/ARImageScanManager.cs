using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageScanManager : MonoBehaviour
{
    [Header("AR Image Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private string targetImageName;

    [Header("Avatar")]
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private Vector3 avatarLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 avatarRotationOffset = new Vector3(90f, 0f, 0f);
    [SerializeField] private float avatarWorldYawOffset;
    [SerializeField] private Vector3 avatarScale = Vector3.one;
    [SerializeField] private bool hideWhenTrackingLost = true;

    private readonly Dictionary<TrackableId, GameObject> spawnedAvatars = new Dictionary<TrackableId, GameObject>();

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
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateAvatarForImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateAvatarForImage(trackedImage);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            RemoveAvatar(removedImage.Key);
        }
    }

    private void UpdateAvatarForImage(ARTrackedImage trackedImage)
    {
        if (avatarPrefab == null || !IsTargetImage(trackedImage))
        {
            return;
        }

        GameObject avatar = GetOrCreateAvatar(trackedImage.trackableId);
        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;

        if (hideWhenTrackingLost)
        {
            avatar.SetActive(isTracking);
        }

        if (!isTracking)
        {
            return;
        }

        Transform imageTransform = trackedImage.transform;
        avatar.transform.position = imageTransform.TransformPoint(avatarLocalOffset);
        avatar.transform.rotation = Quaternion.AngleAxis(avatarWorldYawOffset, Vector3.up)
            * imageTransform.rotation
            * Quaternion.Euler(avatarRotationOffset);
        avatar.transform.localScale = avatarScale;
    }

    private bool IsTargetImage(ARTrackedImage trackedImage)
    {
        if (string.IsNullOrWhiteSpace(targetImageName))
        {
            return true;
        }

        return trackedImage.referenceImage.name == targetImageName;
    }

    private GameObject GetOrCreateAvatar(TrackableId trackableId)
    {
        if (spawnedAvatars.TryGetValue(trackableId, out GameObject avatar) && avatar != null)
        {
            return avatar;
        }

        avatar = Instantiate(avatarPrefab);
        avatar.name = avatarPrefab.name + "_AR";
        spawnedAvatars[trackableId] = avatar;
        return avatar;
    }

    private void RemoveAvatar(TrackableId trackableId)
    {
        if (!spawnedAvatars.TryGetValue(trackableId, out GameObject avatar))
        {
            return;
        }

        if (avatar != null)
        {
            Destroy(avatar);
        }

        spawnedAvatars.Remove(trackableId);
    }
}
