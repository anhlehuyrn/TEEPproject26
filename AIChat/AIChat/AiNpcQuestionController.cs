using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AiNpcQuestionController : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverBaseUrl = "http://127.0.0.1:8787";

    [Header("NPC Context")]
    [SerializeField] private string npcName = "Dragon Boat NPC";
    [SerializeField] private string targetName = "DragonBoat";
    [TextArea(2, 5)]
    [SerializeField] private string lessonContext = "You are explaining the Dragon Boat Festival to students.";

    [Header("Recording")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingSeconds = 8;

    [Header("UI")]
    [SerializeField] private Button askButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text answerText;

    [Header("Audio")]
    [SerializeField] private AudioSource answerAudioSource;

    private AudioClip recordingClip;
    private bool isRecording;
    private bool isBusy;

    private void Awake()
    {
        if (answerAudioSource == null)
        {
            answerAudioSource = GetComponent<AudioSource>();
        }
    }

    public void ToggleRecording()
    {
        if (isBusy)
        {
            return;
        }

        if (isRecording)
        {
            StopRecordingAndAsk();
        }
        else
        {
            StartRecording();
        }
    }

    public void StartRecording()
    {
        if (isBusy || isRecording)
        {
            return;
        }

#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            SetStatus("Please allow microphone permission, then tap Ask again.");
            return;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            SetStatus("No microphone found.");
            return;
        }

        recordingClip = Microphone.Start(null, false, maxRecordingSeconds, sampleRate);
        isRecording = true;
        SetStatus("Listening...");
    }

    public void StopRecordingAndAsk()
    {
        if (!isRecording || recordingClip == null)
        {
            return;
        }

        int samplePosition = Microphone.GetPosition(null);
        Microphone.End(null);
        isRecording = false;

        if (samplePosition <= 0)
        {
            SetStatus("No voice recorded. Try again.");
            return;
        }

        AudioClip trimmedClip = TrimClip(recordingClip, samplePosition);
        byte[] wavBytes = WavUtility.FromAudioClip(trimmedClip);

        StartCoroutine(SendQuestionRoutine(wavBytes));
    }

    private IEnumerator SendQuestionRoutine(byte[] wavBytes)
    {
        isBusy = true;
        SetButtonEnabled(false);
        SetStatus("Sending question...");

        AskRequest request = new AskRequest
        {
            audioBase64 = Convert.ToBase64String(wavBytes),
            mimeType = "audio/wav",
            npcName = npcName,
            targetName = targetName,
            context = lessonContext
        };

        string json = JsonUtility.ToJson(request);
        byte[] body = Encoding.UTF8.GetBytes(json);
        string askUrl = CombineUrl(serverBaseUrl, "/ask");

        using (UnityWebRequest webRequest = new UnityWebRequest(askUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus("AI request failed: " + webRequest.error);
                FinishBusyState();
                yield break;
            }

            AskResponse response = JsonUtility.FromJson<AskResponse>(webRequest.downloadHandler.text);

            if (!string.IsNullOrEmpty(response.error))
            {
                SetStatus("AI error: " + response.error);
                FinishBusyState();
                yield break;
            }

            if (answerText != null)
            {
                answerText.text = "Heard: " + response.transcript + "\n\nAI: " + response.reply;
            }

            SetStatus("Answer received.");

            if (!string.IsNullOrEmpty(response.audioUrl))
            {
                yield return DownloadAndPlayAudio(CombineUrl(serverBaseUrl, response.audioUrl));
            }
        }

        FinishBusyState();
    }

    private IEnumerator DownloadAndPlayAudio(string audioUrl)
    {
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                SetStatus("Audio download failed: " + audioRequest.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            if (answerAudioSource != null && clip != null)
            {
                answerAudioSource.clip = clip;
                answerAudioSource.Play();
            }
        }
    }

    private AudioClip TrimClip(AudioClip sourceClip, int samplePosition)
    {
        int channels = sourceClip.channels;
        float[] sourceData = new float[samplePosition * channels];
        sourceClip.GetData(sourceData, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "StudentQuestion",
            samplePosition,
            channels,
            sourceClip.frequency,
            false);

        trimmedClip.SetData(sourceData, 0);
        return trimmedClip;
    }

    private void FinishBusyState()
    {
        isBusy = false;
        SetButtonEnabled(true);
    }

    private void SetButtonEnabled(bool enabled)
    {
        if (askButton != null)
        {
            askButton.interactable = enabled;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            return path;
        }

        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    [Serializable]
    private class AskRequest
    {
        public string audioBase64;
        public string mimeType;
        public string npcName;
        public string targetName;
        public string context;
    }

    [Serializable]
    private class AskResponse
    {
        public string transcript;
        public string reply;
        public string audioUrl;
        public string error;
    }
}

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = ConvertFloatSamplesToPcm16(samples);
        byte[] wav = new byte[44 + pcmData.Length];

        WriteAscii(wav, 0, "RIFF");
        WriteInt(wav, 4, 36 + pcmData.Length);
        WriteAscii(wav, 8, "WAVE");
        WriteAscii(wav, 12, "fmt ");
        WriteInt(wav, 16, 16);
        WriteShort(wav, 20, 1);
        WriteShort(wav, 22, (short)clip.channels);
        WriteInt(wav, 24, clip.frequency);
        WriteInt(wav, 28, clip.frequency * clip.channels * 2);
        WriteShort(wav, 32, (short)(clip.channels * 2));
        WriteShort(wav, 34, 16);
        WriteAscii(wav, 36, "data");
        WriteInt(wav, 40, pcmData.Length);
        Buffer.BlockCopy(pcmData, 0, wav, 44, pcmData.Length);

        return wav;
    }

    private static byte[] ConvertFloatSamplesToPcm16(float[] samples)
    {
        byte[] pcmData = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            float sample = Mathf.Clamp(samples[i], -1f, 1f);
            short value = (short)(sample * short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(value);
            pcmData[i * 2] = bytes[0];
            pcmData[i * 2 + 1] = bytes[1];
        }

        return pcmData;
    }

    private static void WriteAscii(byte[] buffer, int offset, string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }

    private static void WriteShort(byte[] buffer, int offset, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
    }
}
