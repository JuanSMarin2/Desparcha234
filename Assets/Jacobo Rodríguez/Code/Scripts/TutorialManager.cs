using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] clips;
    [Tooltip("Reproducir automáticamente el primer clip en Start")] 
    [SerializeField] private bool autoPlayOnStart = true;
    [Tooltip("Si es true, al terminar el último clip vuelve al primero")] 
    [SerializeField] private bool loopAtEnd = false;

    [Header("Textos (3)")]
    [SerializeField] private TMP_Text[] textos = new TMP_Text[3];
    [SerializeField] [Range(0f, 1f)] private float inactiveAlpha = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float activeAlpha = 1f;
    [Tooltip("Override por clip del índice de texto activo (>=0). Valores negativos se ignoran y se usa el índice del clip.")]
    [SerializeField] private int[] activeTextIndexPerClip;

    private int _clipIndex = -1;
    private bool _eventsHooked = false;

    void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
    }

    void OnEnable()
    {
        if (videoPlayer && !_eventsHooked)
        {
            videoPlayer.loopPointReached += OnClipFinished;
            videoPlayer.prepareCompleted += OnPrepared;
            _eventsHooked = true;
        }
    }

    void OnDisable()
    {
        if (videoPlayer && _eventsHooked)
        {
            videoPlayer.loopPointReached -= OnClipFinished;
            videoPlayer.prepareCompleted -= OnPrepared;
            _eventsHooked = false;
        }
    }

    void Start()
    {
        if (clips == null || clips.Length == 0 || !videoPlayer) return;
        if (autoPlayOnStart) PlayIndex(0); else UpdatePresentationForIndex(0);
    }

    private void PlayIndex(int idx)
    {
        if (!videoPlayer || clips == null || clips.Length == 0) return;
        idx = Mathf.Clamp(idx, 0, clips.Length - 1);
        _clipIndex = idx;
        UpdatePresentationForIndex(idx);
        videoPlayer.Stop();
        videoPlayer.clip = clips[idx];
        videoPlayer.Prepare(); // OnPrepared -> Play
    }

    private void OnPrepared(VideoPlayer vp)
    {
        if (vp != videoPlayer) return;
        videoPlayer.Play();
    }

    private void OnClipFinished(VideoPlayer vp)
    {
        if (vp != videoPlayer) return;
        int next = _clipIndex + 1;
        if (next >= (clips?.Length ?? 0))
        {
            if (!loopAtEnd) return;
            next = 0;
        }
        PlayIndex(next);
    }

    private void UpdatePresentationForIndex(int idx)
    {
        // Corregido: usar override solo si es >=0; si no, usar el índice del clip
        int maxTexts = (textos != null) ? textos.Length : 0;
        if (maxTexts > 0)
        {
            int overrideIdx = (activeTextIndexPerClip != null && idx < activeTextIndexPerClip.Length)
                ? activeTextIndexPerClip[idx]
                : int.MinValue;
            int activeIdx = (overrideIdx >= 0)
                ? Mathf.Clamp(overrideIdx, 0, maxTexts - 1)
                : Mathf.Clamp(idx, 0, maxTexts - 1);
            SetActiveText(activeIdx);
        }
    }

    private void SetActiveText(int activeIdx)
    {
        if (textos == null) return;
        for (int i = 0; i < textos.Length; i++)
        {
            var t = textos[i];
            if (!t) continue;
            var c = t.color;
            c.a = (i == activeIdx) ? activeAlpha : inactiveAlpha;
            t.color = c;
        }
    }

    // API pública opcional
    public void PlayNext()
    {
        if (clips == null || clips.Length == 0) return;
        int next = _clipIndex + 1;
        if (next >= clips.Length) next = loopAtEnd ? 0 : _clipIndex;
        if (next != _clipIndex) PlayIndex(next);
    }

    public void PlayAt(int index) => PlayIndex(index);

    void OnValidate()
    {
        int len = clips != null ? clips.Length : 0;
        ResizeArray(ref activeTextIndexPerClip, len, -1);
        if (textos != null && textos.Length != 3)
        {
            System.Array.Resize(ref textos, 3);
        }
    }

    private static void ResizeArray(ref int[] arr, int len, int def)
    {
        if (len < 0) len = 0;
        if (arr == null) { arr = new int[len]; for (int i = 0; i < len; i++) arr[i] = def; return; }
        if (arr.Length == len) return;
        var old = arr; arr = new int[len];
        int copy = Mathf.Min(old.Length, len);
        for (int i = 0; i < copy; i++) arr[i] = old[i];
        for (int i = copy; i < len; i++) arr[i] = def;
    }
    private static void ResizeArray(ref bool[] arr, int len, bool def)
    {
        if (len < 0) len = 0;
        if (arr == null) { arr = new bool[len]; for (int i = 0; i < len; i++) arr[i] = def; return; }
        if (arr.Length == len) return;
        var old = arr; arr = new bool[len];
        int copy = Mathf.Min(old.Length, len);
        for (int i = 0; i < copy; i++) arr[i] = old[i];
        for (int i = copy; i < len; i++) arr[i] = def;
    }
}
