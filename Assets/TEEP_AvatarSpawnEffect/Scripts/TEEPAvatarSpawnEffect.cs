using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TEEPAvatarSpawnEffect : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0.05f)] private float effectDuration = 1.2f;
    [SerializeField] private bool hideBeforeEffect = true;
    [SerializeField, Min(0)] private int visualWarmupFrames = 2;

    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private string shaderName = "TEEP/Avatar Spawn Scan";
    [SerializeField] private string resourcesShaderName = "TEEP_AvatarSpawnScan";

    [Header("Scan Line Look")]
    [SerializeField] private Color lowerEdgeColor = new Color(0.15f, 1.1f, 1.4f, 1f);
    [SerializeField] private Color upperEdgeColor = new Color(0.2f, 0.55f, 2.4f, 1f);
    [SerializeField, Range(0.005f, 0.5f)] private float lowerEdgeSize = 0.06f;
    [SerializeField, Range(0.005f, 0.5f)] private float upperEdgeSize = 0.1f;
    [SerializeField, Range(0f, 0.4f)] private float noiseStrength = 0.04f;
    [SerializeField, Range(0.1f, 16f)] private float noiseScale = 7f;

    [Header("Scan Sound")]
    [SerializeField] private AudioSource scanAudioSource;
    [SerializeField] private AudioClip scanSound;
    [SerializeField] private bool matchSoundToEffectDuration = true;
    [SerializeField, Range(0f, 1f)] private float scanSoundVolume = 1f;
    [SerializeField, Min(0f)] private float scanSoundDelay = 0f;
    [SerializeField, Min(0f)] private float scanSoundTargetDuration = 0f;

    [Header("External Particle Effect")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Vector3 particleLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 particleLocalRotation = Vector3.zero;
    [SerializeField, Min(0.01f)] private float particleScale = 1f;
    [SerializeField] private bool parentParticleToAvatar = true;
    [SerializeField] private bool matchParticleToEffectDuration = true;
    [SerializeField, Min(0f)] private float particleStartDelay = 0f;

    [Header("Intro After Spawn")]
    [SerializeField, Min(0f)] private float introDelayAfterEffect = 2f;
    [SerializeField] private AudioSource introAudioSource;
    [SerializeField] private AudioClip introVoiceClip;
    [SerializeField] private Animator avatarAnimator;
    [SerializeField] private string talkBoolName = "Talk";
    [SerializeField] private GameObject canvasToShowAfterIntro;
    [SerializeField] private bool hideCanvasOnPlay = true;

    private readonly List<RendererState> rendererStates = new List<RendererState>();
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private MaterialPropertyBlock propertyBlock;
    private GameObject activeParticleObject;
    private ParticleSystem activeParticleRoot;
    private Coroutine runningEffect;
    private float originalAudioPitch = 1f;

    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int BoundsMinYId = Shader.PropertyToID("_BoundsMinY");
    private static readonly int BoundsMaxYId = Shader.PropertyToID("_BoundsMaxY");
    private static readonly int EdgeColor1Id = Shader.PropertyToID("_EdgeColor1");
    private static readonly int EdgeColor2Id = Shader.PropertyToID("_EdgeColor2");
    private static readonly int EdgeSizeBotId = Shader.PropertyToID("_EdgeSizeBot");
    private static readonly int EdgeSizeTopId = Shader.PropertyToID("_EdgeSizeTop");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        CacheAudioSourceIfNeeded();
        CacheIntroReferencesIfNeeded();
        CacheRenderersIfNeeded();
        CaptureOriginalMaterials();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (runningEffect != null)
        {
            StopCoroutine(runningEffect);
            runningEffect = null;
        }

        RestoreOriginalMaterials();
        StopScanSound();
        StopParticleEffect();
        StopIntroVoice();
        SetTalkAnimation(false);
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        CacheRenderersIfNeeded();

        if (runningEffect != null)
        {
            StopCoroutine(runningEffect);
            runningEffect = null;
            RestoreOriginalMaterials();
            StopScanSound();
            StopParticleEffect();
            StopIntroVoice();
            SetTalkAnimation(false);
        }

        if (hideCanvasOnPlay && canvasToShowAfterIntro != null)
        {
            canvasToShowAfterIntro.SetActive(false);
        }

        CaptureOriginalMaterials();
        runningEffect = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Shader spawnShader = Resources.Load<Shader>(resourcesShaderName);
        if (spawnShader == null)
        {
            spawnShader = Shader.Find(shaderName);
        }

        if (spawnShader == null)
        {
            Debug.LogWarning($"Spawn effect shader '{shaderName}' was not found. The avatar will appear without the scan effect.", this);
            SetRenderersEnabled(true);
            yield break;
        }

        ApplySpawnMaterials(spawnShader);
        SetRenderersEnabled(!hideBeforeEffect);
        SetCutoff(0.01f);

        yield return null;

        SetRenderersEnabled(true);
        for (int i = 0; i < visualWarmupFrames; i++)
        {
            yield return null;
        }

        float soundDelayTimer = scanSoundDelay;
        bool soundStarted = false;
        float particleDelayTimer = particleStartDelay;
        bool particleStarted = false;

        float elapsed = 0f;
        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;

            if (!soundStarted)
            {
                soundDelayTimer -= Time.deltaTime;
                if (soundDelayTimer <= 0f)
                {
                    PlayScanSound();
                    soundStarted = true;
                }
            }

            if (!particleStarted)
            {
                particleDelayTimer -= Time.deltaTime;
                if (particleDelayTimer <= 0f)
                {
                    PlayParticleEffect();
                    particleStarted = true;
                }
            }

            float t = Mathf.Clamp01(elapsed / effectDuration);
            float cutoff = Mathf.SmoothStep(0.01f, 1f, t);
            SetCutoff(cutoff);
            yield return null;
        }

        if (!soundStarted)
        {
            PlayScanSound();
        }

        if (!particleStarted)
        {
            PlayParticleEffect();
        }

        SetCutoff(1f);
        RestoreOriginalMaterials();
        StopScanSound();
        StopParticleEffect();

        yield return PlayIntroAfterSpawnRoutine();

        runningEffect = null;
    }

    private void CacheAudioSourceIfNeeded()
    {
        if (scanAudioSource == null)
        {
            scanAudioSource = GetComponent<AudioSource>();
        }
    }

    private void CacheIntroReferencesIfNeeded()
    {
        if (introAudioSource == null)
        {
            introAudioSource = GetComponent<AudioSource>();
        }

        if (avatarAnimator == null)
        {
            avatarAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void CacheRenderersIfNeeded()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CaptureOriginalMaterials()
    {
        rendererStates.Clear();

        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            rendererStates.Add(new RendererState(targetRenderer, targetRenderer.sharedMaterials, targetRenderer.enabled));
        }
    }

    private void ApplySpawnMaterials(Shader spawnShader)
    {
        CleanupRuntimeMaterials();
        Bounds bounds = CalculateBounds();

        foreach (RendererState state in rendererStates)
        {
            Material[] scanMaterials = new Material[state.OriginalMaterials.Length];

            for (int i = 0; i < scanMaterials.Length; i++)
            {
                Material original = state.OriginalMaterials[i];
                Material scanMaterial = new Material(spawnShader)
                {
                    name = original != null ? original.name + "_SpawnScan_Runtime" : "SpawnScan_Runtime"
                };

                CopyTexture(original, scanMaterial, "_MainTex", "_BaseMap");
                CopyTexture(original, scanMaterial, "_BumpMap", "_NormalMap");
                CopyTexture(original, scanMaterial, "_MetallicGlossMap", "_MetallicGlossMap");
                CopyColor(original, scanMaterial, "_Color", "_BaseColor");
                CopyFloat(original, scanMaterial, "_Metallic", "_Metallic");
                CopyFloat(original, scanMaterial, "_Glossiness", "_Smoothness");

                scanMaterials[i] = scanMaterial;
                runtimeMaterials.Add(scanMaterial);
            }

            state.Renderer.sharedMaterials = scanMaterials;
            ApplyPropertyBlock(state.Renderer, bounds, 0.01f);
        }
    }

    private void ApplyPropertyBlock(Renderer targetRenderer, Bounds bounds, float cutoff)
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(BoundsMinYId, bounds.min.y);
        propertyBlock.SetFloat(BoundsMaxYId, bounds.max.y);
        propertyBlock.SetFloat(CutoffId, cutoff);
        propertyBlock.SetColor(EdgeColor1Id, lowerEdgeColor);
        propertyBlock.SetColor(EdgeColor2Id, upperEdgeColor);
        propertyBlock.SetFloat(EdgeSizeBotId, lowerEdgeSize);
        propertyBlock.SetFloat(EdgeSizeTopId, upperEdgeSize);
        propertyBlock.SetFloat(NoiseStrengthId, noiseStrength);
        propertyBlock.SetFloat(NoiseScaleId, noiseScale);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetCutoff(float cutoff)
    {
        Bounds bounds = CalculateBounds();

        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer != null)
            {
                ApplyPropertyBlock(state.Renderer, bounds, cutoff);
            }
        }
    }

    private Bounds CalculateBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = state.Renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(state.Renderer.bounds);
            }
        }

        return bounds;
    }

    private void RestoreOriginalMaterials()
    {
        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer == null)
            {
                continue;
            }

            state.Renderer.sharedMaterials = state.OriginalMaterials;
            state.Renderer.enabled = state.WasEnabled;
            state.Renderer.SetPropertyBlock(null);
        }

        CleanupRuntimeMaterials();
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer != null)
            {
                state.Renderer.enabled = enabled && state.WasEnabled;
            }
        }
    }

    private void CleanupRuntimeMaterials()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        runtimeMaterials.Clear();
    }

    private void PlayScanSound()
    {
        CacheAudioSourceIfNeeded();

        if (scanAudioSource == null || scanSound == null)
        {
            return;
        }

        originalAudioPitch = scanAudioSource.pitch;
        scanAudioSource.Stop();
        scanAudioSource.clip = scanSound;
        scanAudioSource.loop = false;
        scanAudioSource.volume = scanSoundVolume;

        float targetDuration = scanSoundTargetDuration > 0f ? scanSoundTargetDuration : effectDuration;
        if (matchSoundToEffectDuration && targetDuration > 0f && scanSound.length > 0f)
        {
            scanAudioSource.pitch = Mathf.Clamp(scanSound.length / targetDuration, 0.25f, 3f);
        }

        scanAudioSource.Play();
    }

    private void StopScanSound()
    {
        if (scanAudioSource == null)
        {
            return;
        }

        scanAudioSource.Stop();
        scanAudioSource.pitch = originalAudioPitch;
    }

    private void PlayParticleEffect()
    {
        StopParticleEffect();

        if (particlePrefab == null)
        {
            return;
        }

        Transform parent = parentParticleToAvatar ? transform : null;
        GameObject particleObject = Instantiate(particlePrefab, parent);

        if (parentParticleToAvatar)
        {
            particleObject.transform.localPosition = particleLocalPosition;
            particleObject.transform.localRotation = Quaternion.Euler(particleLocalRotation);
        }
        else
        {
            particleObject.transform.position = transform.TransformPoint(particleLocalPosition);
            particleObject.transform.rotation = transform.rotation * Quaternion.Euler(particleLocalRotation);
        }

        particleObject.transform.localScale = Vector3.one * particleScale;
        activeParticleObject = particleObject;

        ParticleSystem[] particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>(true);
        if (particleSystems.Length == 0)
        {
            Debug.LogWarning("Particle Prefab does not contain any ParticleSystem components.", particleObject);
            Destroy(particleObject);
            return;
        }

        activeParticleRoot = particleSystems[0];

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;

            float particlePlayDuration = Mathf.Max(0.05f, effectDuration - particleStartDelay);
            if (matchParticleToEffectDuration && particlePlayDuration > 0f && main.duration > 0f)
            {
                main.simulationSpeed = Mathf.Clamp(main.duration / particlePlayDuration, 0.05f, 10f);
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void StopParticleEffect()
    {
        if (activeParticleObject == null)
        {
            return;
        }

        Destroy(activeParticleObject);
        activeParticleObject = null;
        activeParticleRoot = null;
    }

    private IEnumerator PlayIntroAfterSpawnRoutine()
    {
        if (introDelayAfterEffect > 0f)
        {
            yield return new WaitForSeconds(introDelayAfterEffect);
        }

        CacheIntroReferencesIfNeeded();

        if (introVoiceClip != null && introAudioSource != null)
        {
            introAudioSource.Stop();
            introAudioSource.clip = introVoiceClip;
            introAudioSource.loop = false;

            SetTalkAnimation(true);
            introAudioSource.Play();

            while (introAudioSource != null && introAudioSource.isPlaying)
            {
                yield return null;
            }
        }

        SetTalkAnimation(false);

        if (canvasToShowAfterIntro != null)
        {
            canvasToShowAfterIntro.SetActive(true);
        }
    }

    private void StopIntroVoice()
    {
        if (introAudioSource != null && introAudioSource.isPlaying)
        {
            introAudioSource.Stop();
        }
    }

    private void SetTalkAnimation(bool isTalking)
    {
        if (avatarAnimator == null || string.IsNullOrWhiteSpace(talkBoolName))
        {
            return;
        }

        avatarAnimator.SetBool(talkBoolName, isTalking);
    }

    private static void CopyTexture(Material source, Material target, string targetProperty, string fallbackSourceProperty)
    {
        if (source == null || target == null || !target.HasProperty(targetProperty))
        {
            return;
        }

        if (source.HasProperty(targetProperty) && source.GetTexture(targetProperty) != null)
        {
            target.SetTexture(targetProperty, source.GetTexture(targetProperty));
            return;
        }

        if (source.HasProperty(fallbackSourceProperty) && source.GetTexture(fallbackSourceProperty) != null)
        {
            target.SetTexture(targetProperty, source.GetTexture(fallbackSourceProperty));
        }
    }

    private static void CopyColor(Material source, Material target, string targetProperty, string fallbackSourceProperty)
    {
        if (source == null || target == null || !target.HasProperty(targetProperty))
        {
            return;
        }

        if (source.HasProperty(targetProperty))
        {
            target.SetColor(targetProperty, source.GetColor(targetProperty));
            return;
        }

        if (source.HasProperty(fallbackSourceProperty))
        {
            target.SetColor(targetProperty, source.GetColor(fallbackSourceProperty));
        }
    }

    private static void CopyFloat(Material source, Material target, string targetProperty, string fallbackSourceProperty)
    {
        if (source == null || target == null || !target.HasProperty(targetProperty))
        {
            return;
        }

        if (source.HasProperty(targetProperty))
        {
            target.SetFloat(targetProperty, source.GetFloat(targetProperty));
            return;
        }

        if (source.HasProperty(fallbackSourceProperty))
        {
            target.SetFloat(targetProperty, source.GetFloat(fallbackSourceProperty));
        }
    }

    private readonly struct RendererState
    {
        public RendererState(Renderer renderer, Material[] originalMaterials, bool wasEnabled)
        {
            Renderer = renderer;
            OriginalMaterials = originalMaterials;
            WasEnabled = wasEnabled;
        }

        public readonly Renderer Renderer;
        public readonly Material[] OriginalMaterials;
        public readonly bool WasEnabled;
    }
}
