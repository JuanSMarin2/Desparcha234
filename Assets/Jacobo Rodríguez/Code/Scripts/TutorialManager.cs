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

    [Header("Modo Animación (en vez de video)")]
    [SerializeField] private bool modoAnimacion = false;
    [Tooltip("GameObject que contiene la Image/Animator para el modo animación")]
    [SerializeField] private GameObject animacionImageObject;
    [Tooltip("Animator que tiene los estados de animación (Anim1/Anim2/Anim3 por defecto)")]
    [SerializeField] private Animator animacionAnimator;
    [Tooltip("Nombres de los estados de animator que mapean a los textos 0..2")]
    [SerializeField] private string animStateName1 = "Anim1";
    [SerializeField] private string animStateName2 = "Anim2";
    [SerializeField] private string animStateName3 = "Anim3";

    [Header("Textos (3)")]
    [SerializeField] private TMP_Text[] textos = new TMP_Text[3];
    [SerializeField][Range(0f, 1f)] private float inactiveAlpha = 0.4f;
    [SerializeField][Range(0f, 1f)] private float activeAlpha = 1f;
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
        if (!modoAnimacion)
        {
            if (videoPlayer && !_eventsHooked)
            {
                videoPlayer.loopPointReached += OnClipFinished;
                videoPlayer.prepareCompleted += OnPrepared;
                _eventsHooked = true;
            }
        }
    }

    void OnDisable()
    {
        if (!modoAnimacion)
        {
            if (videoPlayer && _eventsHooked)
            {
                videoPlayer.loopPointReached -= OnClipFinished;
                videoPlayer.prepareCompleted -= OnPrepared;
                _eventsHooked = false;
            }
        }
    }

    void Start()
    {
        // modoAnimacion: usar Image+Animator en lugar de VideoPlayer
        if (modoAnimacion)
        {
            if (videoPlayer && videoPlayer.gameObject.activeSelf)
                videoPlayer.gameObject.SetActive(false);
            if (animacionImageObject) animacionImageObject.SetActive(true);
            // actualizar textos inmediatamente según estado actual
            UpdateAnimacionText();
            return;
        }

        if (animacionImageObject) animacionImageObject.SetActive(false);

        if (clips == null || clips.Length == 0 || !videoPlayer) return;
        if (autoPlayOnStart) PlayIndex(0); else UpdatePresentationForIndex(0);
    }

    void Update()
    {
        if (modoAnimacion)
        {
            UpdateAnimacionText();
        }
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

    private void UpdateAnimacionText()
    {
        if (animacionAnimator == null || textos == null || textos.Length == 0) return;
        var info = animacionAnimator.GetCurrentAnimatorStateInfo(0);
        int activeIdx = -1;
        if (!string.IsNullOrEmpty(animStateName1) && info.IsName(animStateName1)) activeIdx = 0;
        else if (!string.IsNullOrEmpty(animStateName2) && info.IsName(animStateName2)) activeIdx = 1;
        else if (!string.IsNullOrEmpty(animStateName3) && info.IsName(animStateName3)) activeIdx = 2;
        SetActiveText(activeIdx);
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