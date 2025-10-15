using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelFinalTurno : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup canvasGroup; // para fades
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text textoResultado;
    [SerializeField] private TMP_Text textoPuntos;

    
   [Header("Textos")]
   [SerializeField] private string textoGanar;
   [SerializeField] private string textoPerder;

    [Header("Sprites Jugadores (feliz)")]
    [Tooltip("Orden: Jugador 1..4")] public Sprite[] iconosFelices = new Sprite[4];

    [Header("Sprites Jugadores (triste) - opcional")] 
    [Tooltip("Orden: Jugador 1..4")] public Sprite[] iconosTristes = new Sprite[4];

    [Header("Tiempos")] 
    [SerializeField] private float fadeInDuration = 0.2f; 
    [SerializeField] private float conteoDuration = 0.8f; 
    [SerializeField] private float holdAfterConteo = 2f; // configurable por diseñador
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Escala / efecto icono (opcional)")]
    [SerializeField] private bool pulseIcon = true;
    [SerializeField] private float pulseAmplitude = 0.08f;
    [SerializeField] private float pulseSpeed = 5f;

    [Header("SFX Conteo")]
    [SerializeField] private string sfxTickKey = "catapis:tick"; // clave en SceneAudioLibrary
    [SerializeField] private float minTickInterval = 0.03f;       // evita saturación
    private int _ultimoValorTick = -1;
    private float _lastTickTime = -999f;

    private Action _onFinished;
    private bool _running;
    private bool _success;
    private int _puntos;
    private int _player1Based;
    private float _baseIconScale = 1f;

    public static event Action<int, bool> OnPanelShown;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (iconImage != null) _baseIconScale = iconImage.rectTransform.localScale.x;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_running || !pulseIcon || iconImage == null) return;
        float s = _baseIconScale + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
        iconImage.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    public void Show(int playerIndex1Based, bool success, int puntos, Action onFinished)
    {
        if (_running) return; // evitar solapado
        _running = true;
        _onFinished = onFinished;
        _success = success;
        _puntos = puntos; // permitir negativos
        _player1Based = Mathf.Clamp(playerIndex1Based, 1, 4);
        _ultimoValorTick = -1; // reset para conteo
        _lastTickTime = Time.unscaledTime;

        // Seleccionar sprite
        Sprite spriteElegido = null;
        int idx = _player1Based - 1;
        if (success && idx < iconosFelices.Length) spriteElegido = iconosFelices[idx];
        if (!success && idx < iconosTristes.Length && iconosTristes[idx] != null) spriteElegido = iconosTristes[idx];
        if (!success && spriteElegido == null && idx < iconosFelices.Length) spriteElegido = iconosFelices[idx]; // fallback feliz si falta triste
        if (iconImage != null) iconImage.sprite = spriteElegido;

        // Texto resultado
        if (textoResultado != null)
        {
            if (_success)
                textoResultado.text = (_puntos < 0) ? textoPerder : textoGanar;
            else
                textoResultado.text = textoPerder;
        }

        // Texto puntos inicial
        if (textoPuntos != null)
        {
            if (_success)
            {
                if (_puntos > 0)
                {
                    // caso normal: ganancia positiva, iniciamos conteo desde 0
                    textoPuntos.text = $"Ganas: 0 puntos";
                }
                else if (_puntos < 0)
                {
                    // intento exitoso pero puntaje negativo (bombas): mostrar pérdidas
                    textoPuntos.text = $"Pierdes: {Mathf.Abs(_puntos)} puntos";
                }
                else
                {
                    // cero exacto
                    textoPuntos.text = $"Ganas: 0 puntos";
                }
            }
            else
            {
                // intento fallado siempre muestra "Ganas: 0 puntos"
                textoPuntos.text = $"Ganas: 0 puntos";
            }
        }

        // Preparar canvas
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Notificar a escuchas (p.ej. IconManagerCatapis) del resultado de este turno
        OnPanelShown?.Invoke(_player1Based, _success);

        // Deshabilitar todos los jacks existentes al mostrar el panel
        var spawners = UnityEngine.Object.FindObjectsByType<JackSpawner>(FindObjectsSortMode.None);
        foreach (var sp in spawners)
        {
            if (sp != null) sp.DisableAll();
        }

        // Usar tiempo no escalado para funcionar en pausa
        StartCoroutine(RutinaPanel());
    }

    private IEnumerator RutinaPanel()
    {
        // Fade in
        if (canvasGroup != null && fadeInDuration > 0f)
        {
            float t = 0f; while (t < fadeInDuration) { t += Time.unscaledDeltaTime; canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration); yield return null; }
            canvasGroup.alpha = 1f;
        }
        else if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Conteo: positivo (gana) o negativo (pierde)
        if (conteoDuration > 0f && textoPuntos != null && _success)
        {
            if (_puntos > 0)
            {
                float t = 0f;
                while (t < conteoDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / conteoDuration);
                    int valor = Mathf.RoundToInt(Mathf.Lerp(0, _puntos, k));
                    if (valor != _ultimoValorTick && (Time.unscaledTime - _lastTickTime) >= minTickInterval)
                    {
                        _ultimoValorTick = valor;
                        _lastTickTime = Time.unscaledTime;
                        var sm = SoundManager.instance; if (sm) sm.PlaySfx(sfxTickKey, 0.75f);
                    }
                    textoPuntos.text = $"Ganas: {valor} puntos";
                    yield return null;
                }
            }
            else if (_puntos < 0)
            {
                int maxLoss = Mathf.Abs(_puntos);
                float t = 0f;
                while (t < conteoDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / conteoDuration);
                    int valor = Mathf.RoundToInt(Mathf.Lerp(maxLoss, 0, k));
                    if (valor != _ultimoValorTick && (Time.unscaledTime - _lastTickTime) >= minTickInterval)
                    {
                        _ultimoValorTick = valor;
                        _lastTickTime = Time.unscaledTime;
                        var sm = SoundManager.instance; if (sm) sm.PlaySfx(sfxTickKey, 0.75f);
                    }
                    textoPuntos.text = $"Pierdes: {valor} puntos";
                    yield return null;
                }
            }
        }
        
        // Texto final según caso
        if (textoPuntos != null)
        {
            if (_success)
            {
                if (_puntos > 0) textoPuntos.text = $"Ganas: {_puntos} puntos";
                else if (_puntos < 0) textoPuntos.text = $"Pierdes: {Mathf.Abs(_puntos)} puntos";
                else textoPuntos.text = $"Ganas: 0 puntos";
            }
            else
            {
                textoPuntos.text = $"Ganas: 0 puntos";
            }
        }

        // Espera
        if (holdAfterConteo > 0f) yield return new WaitForSecondsRealtime(holdAfterConteo);

        // Fade out
        if (canvasGroup != null && fadeOutDuration > 0f)
        {
            float t = 0f; while (t < fadeOutDuration) { t += Time.unscaledDeltaTime; canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration); yield return null; }
            canvasGroup.alpha = 0f;
        }
        else if (canvasGroup != null) canvasGroup.alpha = 0f;

        // Terminar
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        _running = false;
        var cb = _onFinished; _onFinished = null; cb?.Invoke();
    }
}
