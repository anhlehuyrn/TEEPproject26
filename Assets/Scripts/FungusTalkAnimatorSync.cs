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

        EnsureAnimator();
        CacheWriter();
        SetTalk(false);
    }

    private void EnsureAnimator()
    {
        if (avatarAnimator == null)
        {
            avatarAnimator = GetComponent<Animator>();
            if (avatarAnimator == null) avatarAnimator = GetComponentInChildren<Animator>(true);
            if (avatarAnimator == null) avatarAnimator = GetComponentInParent<Animator>();
        }
    }

    private void OnEnable()
    {
        EnsureAnimator();
        WriterSignals.OnWriterState += OnWriterState;
    }

    private void OnDisable()
    {
        WriterSignals.OnWriterState -= OnWriterState;
        SetTalk(false);
    }

    private void Update()
    {
        EnsureAnimator();
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

        if (targetSayDialog == null || !targetSayDialog.gameObject.activeInHierarchy)
        {
            cachedWriter = null;
            cachedWriterAudio = null;
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
        SayDialog currentDialog = sayDialog != null ? sayDialog : SayDialog.ActiveSayDialog;
        if (currentDialog == null || !currentDialog.gameObject.activeInHierarchy)
        {
            return false;
        }

        // 1. KIỂM TRA TRỰC TIẾP: Loa trên SayDialog đang phát âm thanh (Voice-over clip)
        AudioSource[] dialogAudios = currentDialog.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audio in dialogAudios)
        {
            if (audio != null && audio.isPlaying && audio.clip != null)
            {
                // Nếu âm thanh đang phát và chưa chạm đến điểm kết thúc
                if (audio.time < audio.clip.length - 0.05f)
                {
                    return true;
                }
            }
        }

        // 2. Kiểm tra WriterAudio (Voice-over còn thời lượng)
        if (talkWhileVoiceOverIsPlaying && cachedWriterAudio != null)
        {
            if (cachedWriterAudio.IsPlayingVoiceOver && cachedWriterAudio.GetSecondsRemaining() > 0.05f)
            {
                return true;
            }
        }

        // 3. Kiểm tra nếu chữ đang gõ ra màn hình (Typewriter)
        if (cachedWriter != null && cachedWriter.IsWriting)
        {
            return true;
        }

        return false;
    }

    private void SetTalk(bool isTalking)
    {
        lastTalkingState = isTalking;

        if (avatarAnimator == null)
        {
            EnsureAnimator();
        }

        if (avatarAnimator == null || string.IsNullOrWhiteSpace(talkBoolName))
        {
            return;
        }

        avatarAnimator.SetBool(talkBoolName, isTalking);

        if (logDebugMessages)
        {
            Debug.Log($"Animator bool '{talkBoolName}' = {isTalking}", this);
        }
    }
}
