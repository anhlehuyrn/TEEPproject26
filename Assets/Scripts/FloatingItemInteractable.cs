using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingItemInteractable : MonoBehaviour
{
    private static AudioSource activeAudioSource;
    private static FloatingItemInteractable activeItem;

    [SerializeField] private float idleBobMeters = 0.01f;
    [SerializeField] private float idleBobSpeed = 1.8f;
    [SerializeField] private bool faceCamera = true;

    private SpriteRenderer spriteRenderer;
    private TextMeshPro descriptionLabel;
    private AudioSource audioSource;
    private List<Sprite> animationSprites = new List<Sprite>();
    private Camera arCamera;
    private Vector3 startLocalPosition;
    private Vector3 restLocalPosition;
    private Vector3 baseLocalPosition;
    private Vector3 defaultLocalScale;
    private Vector3 pinchStartScale;
    private float floatInSeconds = 0.7f;
    private float spriteAnimationFps = 8f;
    private float minScaleMultiplier = 0.5f;
    private float maxScaleMultiplier = 2.5f;
    private float pinchStartDistance;
    private float spriteFrameTimer;
    private Coroutine floatInCoroutine;
    private int spriteFrameIndex;
    private bool isDragging;
    private bool isPinching;
    private Plane dragPlane;
    private Vector2 pointerStartScreen;
    private float pointerStartTime;

    public void Initialize(
        SpriteRenderer renderer,
        TextMeshPro label,
        AudioSource itemAudioSource,
        List<Sprite> itemAnimationSprites,
        float itemAnimationFps,
        float itemMinScaleMultiplier,
        float itemMaxScaleMultiplier,
        Vector3 startPosition,
        Vector3 restPosition,
        float animationSeconds)
    {
        spriteRenderer = renderer;
        descriptionLabel = label;
        audioSource = itemAudioSource;
        animationSprites = itemAnimationSprites ?? new List<Sprite>();
        spriteAnimationFps = Mathf.Max(1f, itemAnimationFps);
        minScaleMultiplier = Mathf.Max(0.1f, itemMinScaleMultiplier);
        maxScaleMultiplier = Mathf.Max(minScaleMultiplier, itemMaxScaleMultiplier);
        startLocalPosition = startPosition;
        restLocalPosition = restPosition;
        baseLocalPosition = restPosition;
        defaultLocalScale = transform.localScale;
        floatInSeconds = Mathf.Max(0.01f, animationSeconds);
        arCamera = Camera.main;

        PlayFloatIn();
        PlaySpriteAnimationIfNeeded();
    }

    public void PlayFloatIn()
    {
        if (floatInCoroutine != null)
        {
            StopCoroutine(floatInCoroutine);
        }

        floatInCoroutine = StartCoroutine(FloatInRoutine());
    }

    public void ResetForTrackingFound()
    {
        ResetState(true);
    }

    public void ResetForExplorationHidden()
    {
        ResetState(false);
    }

    private void ResetState(bool playFloatIn)
    {
        isDragging = false;
        isPinching = false;

        StopAudio();
        HideDescription();
        ResetSpriteAnimationToFirstFrame();
        ClearActiveItemIfThis();

        transform.localScale = defaultLocalScale;
        transform.localPosition = startLocalPosition;
        baseLocalPosition = restLocalPosition;

        if (playFloatIn)
        {
            PlayFloatIn();
        }
    }

    private void Update()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        UpdateBillboard();
        UpdateSpriteAnimation();
        UpdateIdleFloat();
        UpdatePointerInput();
    }

    private IEnumerator FloatInRoutine()
    {
        transform.localPosition = startLocalPosition;
        baseLocalPosition = restLocalPosition;
        SetAlpha(0f);

        float timer = 0f;
        while (timer < floatInSeconds)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / floatInSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            transform.localPosition = Vector3.Lerp(startLocalPosition, restLocalPosition, eased);
            SetAlpha(eased);
            yield return null;
        }

        transform.localPosition = restLocalPosition;
        SetAlpha(1f);
        floatInCoroutine = null;
    }

    private void PlaySpriteAnimationIfNeeded()
    {
        if (spriteRenderer == null || animationSprites == null || animationSprites.Count < 2)
        {
            return;
        }

        spriteFrameIndex = 0;
        spriteFrameTimer = 0f;
        spriteRenderer.sprite = animationSprites[spriteFrameIndex];
    }

    private void ResetSpriteAnimationToFirstFrame()
    {
        spriteFrameIndex = 0;
        spriteFrameTimer = 0f;

        if (spriteRenderer != null && animationSprites != null && animationSprites.Count > 0)
        {
            spriteRenderer.sprite = animationSprites[0];
        }
    }

    private void UpdateSpriteAnimation()
    {
        if (spriteRenderer == null || animationSprites == null || animationSprites.Count < 2)
        {
            return;
        }

        spriteFrameTimer += Time.deltaTime;
        float frameSeconds = 1f / Mathf.Max(1f, spriteAnimationFps);
        if (spriteFrameTimer < frameSeconds)
        {
            return;
        }

        spriteFrameTimer -= frameSeconds;
        spriteFrameIndex = (spriteFrameIndex + 1) % animationSprites.Count;
        spriteRenderer.sprite = animationSprites[spriteFrameIndex];
    }

    private void UpdateBillboard()
    {
        if (!faceCamera || arCamera == null)
        {
            return;
        }

        Vector3 direction = transform.position - arCamera.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void UpdateIdleFloat()
    {
        if (isDragging || floatInCoroutine != null)
        {
            return;
        }

        float offset = Mathf.Sin(Time.time * idleBobSpeed) * idleBobMeters;
        transform.localPosition = baseLocalPosition + new Vector3(0f, offset, 0f);
    }

    private void UpdatePointerInput()
    {
        if (Input.touchCount >= 2)
        {
            HandlePinch(Input.GetTouch(0), Input.GetTouch(1));
            return;
        }

        if (isPinching)
        {
            isPinching = false;
            isDragging = false;
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            HandlePointer(touch.position, touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary, touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
            return;
        }

        HandlePointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Input.GetMouseButtonUp(0));
    }

    private void HandlePinch(Touch firstTouch, Touch secondTouch)
    {
        if (arCamera == null)
        {
            return;
        }

        Vector2 firstPosition = firstTouch.position;
        Vector2 secondPosition = secondTouch.position;
        float currentDistance = Vector2.Distance(firstPosition, secondPosition);

        if (!isPinching)
        {
            if (!IsPointerOverThis(firstPosition) && !IsPointerOverThis(secondPosition))
            {
                return;
            }

            isPinching = true;
            isDragging = false;
            pinchStartDistance = Mathf.Max(1f, currentDistance);
            pinchStartScale = transform.localScale;
            return;
        }

        float scaleRatio = currentDistance / Mathf.Max(1f, pinchStartDistance);
        Vector3 targetScale = pinchStartScale * scaleRatio;
        float minScale = defaultLocalScale.x * minScaleMultiplier;
        float maxScale = defaultLocalScale.x * maxScaleMultiplier;
        float clampedScale = Mathf.Clamp(targetScale.x, minScale, maxScale);
        transform.localScale = Vector3.one * clampedScale;
    }

    private void HandlePointer(Vector2 screenPosition, bool began, bool held, bool ended)
    {
        if (arCamera == null)
        {
            return;
        }

        if (began && IsPointerOverThis(screenPosition))
        {
            isDragging = true;
            pointerStartScreen = screenPosition;
            pointerStartTime = Time.time;
            dragPlane = new Plane(-arCamera.transform.forward, transform.position);
        }

        if (isDragging && held)
        {
            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                transform.position = worldPoint;

                if (transform.parent != null)
                {
                    baseLocalPosition = transform.parent.InverseTransformPoint(worldPoint);
                    transform.localPosition = baseLocalPosition;
                }
                else
                {
                    baseLocalPosition = transform.localPosition;
                }
            }
        }

        if (isDragging && ended)
        {
            bool isTap = Vector2.Distance(pointerStartScreen, screenPosition) < 18f && Time.time - pointerStartTime < 0.3f;
            isDragging = false;

            if (isTap)
            {
                ToggleDescription();
            }
        }
    }

    private bool IsPointerOverThis(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.transform == transform;
    }

    private void ToggleDescription()
    {
        if (descriptionLabel == null)
        {
            CloseActiveItemBeforeOpeningThis();
            activeItem = this;
            PlayAudioFromStart();
            return;
        }

        bool shouldShow = !descriptionLabel.gameObject.activeSelf;
        descriptionLabel.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            CloseActiveItemBeforeOpeningThis();
            activeItem = this;
            PlayAudioFromStart();
        }
        else
        {
            StopAudio();
            ClearActiveItemIfThis();
        }
    }

    private void CloseActiveItemBeforeOpeningThis()
    {
        if (activeItem != null && activeItem != this)
        {
            activeItem.CloseDescriptionAndAudio();
        }
    }

    private void CloseDescriptionAndAudio()
    {
        isDragging = false;
        isPinching = false;

        StopAudio();
        HideDescription();
        ClearActiveItemIfThis();
    }

    private void ClearActiveItemIfThis()
    {
        if (activeItem == this)
        {
            activeItem = null;
        }
    }

    private void HideDescription()
    {
        if (descriptionLabel != null)
        {
            descriptionLabel.gameObject.SetActive(false);
        }
    }

    private void PlayAudioFromStart()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }

        if (activeAudioSource != null && activeAudioSource != audioSource)
        {
            activeAudioSource.Stop();
        }

        activeAudioSource = audioSource;
        audioSource.Stop();
        audioSource.Play();
    }

    private void StopAudio()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();

        if (activeAudioSource == audioSource)
        {
            activeAudioSource = null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        if (descriptionLabel != null)
        {
            Color color = descriptionLabel.color;
            color.a = alpha;
            descriptionLabel.color = color;
        }
    }
}
