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

    [Header("Interactive Look-At")]
    [Tooltip("When enabled, the avatar will always smoothly turn and face the user/camera in AR.")]
    [SerializeField] private bool faceCamera = true;
    [Tooltip("Smoothly rotate towards the user instead of snapping instantly.")]
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float rotationSpeed = 8f;

    private const float BaseCameraDistanceMeters = 0.4f;
    private const float MinAutoScaleMultiplier = 0.5f;
    private const float MaxAutoScaleMultiplier = 8f;

    private readonly Dictionary<TrackableId, GameObject> spawnedAvatars = new Dictionary<TrackableId, GameObject>();
    private Camera arCamera;

    private void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        arCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!faceCamera)
        {
            return;
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            return;
        }

        foreach (KeyValuePair<TrackableId, GameObject> kvp in spawnedAvatars)
        {
            GameObject avatar = kvp.Value;
            if (avatar != null && avatar.activeInHierarchy)
            {
                Vector3 lookDir = arCamera.transform.position - avatar.transform.position;
                lookDir.y = 0;

                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                    if (smoothRotation && Application.isPlaying)
                    {
                        avatar.transform.rotation = Quaternion.Slerp(avatar.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                    }
                    else
                    {
                        avatar.transform.rotation = targetRotation;
                    }
                }
            }
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
        float scaleMultiplier = GetTrackedImageScaleMultiplier(trackedImage);
        avatar.transform.position = imageTransform.TransformPoint(avatarLocalOffset * scaleMultiplier);
        avatar.transform.localScale = avatarScale * scaleMultiplier;

        if (faceCamera)
        {
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (arCamera != null)
            {
                Vector3 lookDir = arCamera.transform.position - avatar.transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    avatar.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }
        }
        else
        {
            avatar.transform.rotation = Quaternion.AngleAxis(avatarWorldYawOffset, Vector3.up)
                * imageTransform.rotation
                * Quaternion.Euler(avatarRotationOffset);
        }
    }

    private float GetTrackedImageScaleMultiplier(ARTrackedImage trackedImage)
    {
        if (trackedImage == null)
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

        float cameraDistance = Vector3.Distance(arCamera.transform.position, trackedImage.transform.position);
        float scaleMultiplier = cameraDistance / BaseCameraDistanceMeters;
        return Mathf.Clamp(scaleMultiplier, MinAutoScaleMultiplier, MaxAutoScaleMultiplier);
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
