using Fungus;
using UnityEngine;

public class FungusTalkAnimatorSync : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator avatarAnimator;
    [SerializeField] private string talkBoolName = "Talk";

    [Header("Fungus")]
    [SerializeField] private SayDialog sayDialog;
    [SerializeField] private bool useActiveSayDialog = true;
    [SerializeField] private bool talkWhileWaitingForInput = false;
    [SerializeField] private bool talkWhileVoiceOverIsPlaying = true;
    [SerializeField] private bool logDebugMessages = false;

    private Writer cachedWriter;
    private WriterAudio cachedWriterAudio;
    private bool lastTalkingState;

    private void Awake()
    {
        if (avatarAnimator == null)
        {
            avatarAnimator = GetComponentInChildren<Animator>(true);
        }

        CacheWriter();
        SetTalk(false);
    }

    private void OnEnable()
    {
        WriterSignals.OnWriterState += OnWriterState;
    }

    private void OnDisable()
    {
        WriterSignals.OnWriterState -= OnWriterState;
        SetTalk(false);
    }

    private void Update()
    {
        CacheWriter();

        bool isTalking = IsFungusCurrentlySpeaking();
        if (isTalking != lastTalkingState)
        {
            SetTalk(isTalking);
        }
    }

    private void OnWriterState(Writer writer, WriterState writerState)
    {
        if (writer == null)
        {
            return;
        }

        if (!ShouldUseWriter(writer))
        {
            return;
        }

        cachedWriter = writer;

        if (logDebugMessages)
        {
            Debug.Log($"Fungus writer state: {writerState}", this);
        }
    }

    private void CacheWriter()
    {
        SayDialog targetSayDialog = sayDialog;

        if (useActiveSayDialog && SayDialog.ActiveSayDialog != null)
        {
            targetSayDialog = SayDialog.ActiveSayDialog;
        }

        if (targetSayDialog == null)
        {
            cachedWriter = null;
            return;
        }

        cachedWriter = targetSayDialog.GetComponent<Writer>();
        cachedWriterAudio = targetSayDialog.GetComponent<WriterAudio>();
    }

    private bool ShouldUseWriter(Writer writer)
    {
        if (sayDialog != null)
        {
            Writer assignedWriter = sayDialog.GetComponent<Writer>();
            return assignedWriter == writer;
        }

        if (!useActiveSayDialog || SayDialog.ActiveSayDialog == null)
        {
            return true;
        }

        Writer activeWriter = SayDialog.ActiveSayDialog.GetComponent<Writer>();
        return activeWriter == writer;
    }

    private bool IsFungusCurrentlySpeaking()
    {
        if (cachedWriter == null)
        {
            return false;
        }

        if (talkWhileVoiceOverIsPlaying
            && cachedWriterAudio != null
            && cachedWriterAudio.IsPlayingVoiceOver
            && cachedWriterAudio.GetSecondsRemaining() > 0f)
        {
            return true;
        }

        if (!cachedWriter.IsWriting)
        {
            return false;
        }

        if (!talkWhileWaitingForInput && cachedWriter.IsWaitingForInput)
        {
            return false;
        }

        return true;
    }

    private void SetTalk(bool isTalking)
    {
        lastTalkingState = isTalking;

        if (avatarAnimator == null || string.IsNullOrWhiteSpace(talkBoolName))
        {
            if (logDebugMessages && avatarAnimator == null)
            {
                Debug.LogWarning("Avatar Animator is not assigned, so Talk cannot be updated.", this);
            }

            return;
        }

        avatarAnimator.SetBool(talkBoolName, isTalking);

        if (logDebugMessages)
        {
            Debug.Log($"Animator bool '{talkBoolName}' = {isTalking}", this);
        }
    }
}
