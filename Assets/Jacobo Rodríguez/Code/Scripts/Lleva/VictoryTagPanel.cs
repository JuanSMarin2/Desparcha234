using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VictoryTagPanel : MonoBehaviour
{
    [Header("Referencias")] [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject[] victoryObjects = new GameObject[4]; // 1..4
    [SerializeField] private TMP_Text textoGanador;
    [SerializeField] private string formatoGanador = "Jugador {0} gana";

    [Header("Animación")] 
    [Tooltip("Animator que reproduce la animación de victoria. Se fuerza a UpdateMode = UnscaledTime si Time.timeScale = 0")] 
    [SerializeField] private Animator victoryAnimator;
    [Tooltip("Nombre del estado (o trigger) a reproducir al mostrar el panel")] 
    [SerializeField] private string victoryStateName = "Victory";
    [Tooltip("Usar SetTrigger en lugar de Play")] [SerializeField] private bool usarTrigger = false;
    [Tooltip("Si no llega Animation Event tras este tiempo, finalizar ronda igualmente")] [SerializeField] private bool fallbackSiNoEvent = true;
    [SerializeField] private float fallbackDelay = 4f;

    [Header("Modo Congelados")] [SerializeField] private bool esCongelados = false;
    [SerializeField] private Image iconoGanador; // opcional para mostrar sprite del ganador modo congelados
    [SerializeField] private Image[] iconosMultiples; // nuevos slots para múltiples ganadores (empate)

    private int _currentWinner = -1;
    private bool _animationStarted = false;
    private bool _finishedSignaled = false;
    private System.Collections.IEnumerator _fallbackCo;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        HideAllVictoryObjects();
        gameObject.SetActive(false);
    }

    private void HideAllVictoryObjects()
    {
        if (victoryObjects == null) return;
        foreach (var go in victoryObjects) if (go) go.SetActive(false);
    }

    public void ShowWinner(int playerIndex1Based)
    {
        _currentWinner = playerIndex1Based;
        gameObject.SetActive(true);
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        HideAllVictoryObjects();
        int idx = playerIndex1Based - 1;
        if (idx >= 0 && idx < victoryObjects.Length && victoryObjects[idx]) victoryObjects[idx].SetActive(true);
        if (textoGanador) textoGanador.text = string.Format(formatoGanador, playerIndex1Based);

        IniciarAnimacionVictoria();
    }

    private void IniciarAnimacionVictoria()
    {
        if (_animationStarted) return;
        _animationStarted = true;
        if (victoryAnimator == null)
        {
            victoryAnimator = GetComponentInChildren<Animator>(true);
            if (victoryAnimator == null)
            {
                Debug.LogWarning("[VictoryTagPanel] No se encontró Animator. Se finalizará inmediatamente.");
                FinalizarSiNoSeñalado();
                return;
            }
        }
        // Asegurar modo de actualización para reproducirse aunque timeScale=0
        victoryAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Adjuntar (o verificar) proxy de animation event
        var proxy = victoryAnimator.GetComponent<VictoryTagAnimationEventProxy>();
        if (proxy == null)
        {
            proxy = victoryAnimator.gameObject.AddComponent<VictoryTagAnimationEventProxy>();
            proxy.tagManager = TagManager.Instance;
            Debug.Log("[VictoryTagPanel] Proxy de AnimationEvent agregado dinámicamente.");
        }

        if (usarTrigger)
        {
            victoryAnimator.ResetTrigger(victoryStateName);
            victoryAnimator.SetTrigger(victoryStateName);
            Debug.Log($"[VictoryTagPanel] SetTrigger '{victoryStateName}'");
        }
        else
        {
            victoryAnimator.Play(victoryStateName, 0, 0f);
            Debug.Log($"[VictoryTagPanel] Play estado '{victoryStateName}'");
        }

        if (fallbackSiNoEvent)
        {
            if (_fallbackCo != null) StopCoroutine(_fallbackCo);
            _fallbackCo = FallbackCoroutine();
            StartCoroutine(_fallbackCo);
        }
    }

    private System.Collections.IEnumerator FallbackCoroutine()
    {
        float t = 0f;
        while (t < fallbackDelay)
        {
            t += Time.unscaledDeltaTime; // unscaled para funcionar con timeScale=0
            yield return null;
            if (_finishedSignaled) yield break; // ya terminó por event
        }
        Debug.LogWarning("[VictoryTagPanel] Fallback: no llegó Animation Event, finalizando ronda.");
        FinalizarSiNoSeñalado();
    }

    private void FinalizarSiNoSeñalado()
    {
        if (_finishedSignaled) return;
        _finishedSignaled = true;
        TagManager.Instance?.OnVictoryAnimationFinished();
    }

    // Método público para anim event final (si se usa este panel en lugar de proxy separado)
    public void OnVictoryAnimationFinished()
    {
        Debug.Log("[VictoryTagPanel] Animation Event recibido (panel)");
        FinalizarSiNoSeñalado();
    }

    public void EnableCongeladosModeAndShow(int playerIndex1Based)
    {
        esCongelados = true;
        ShowWinner(playerIndex1Based);
        // Reemplazar texto y objetos por un solo icono feliz del ganador
        if (iconoGanador)
        {
            var gm = IconManagerGeneral.Instance;
            if (gm != null)
            {
                var sprite = gm.GetHappy(Mathf.Clamp(playerIndex1Based - 1, 0, 3));
                iconoGanador.sprite = sprite;
                iconoGanador.enabled = sprite != null;
            }
        }
        // Ocultar objetos de tag clásico
        HideAllVictoryObjects();
        if (textoGanador) textoGanador.text = $"Jugador {playerIndex1Based} gana"; // podría personalizarse
        LimpiarIconosMultiples();
    }

    public void EnableCongeladosModeAndShowMultiple(System.Collections.Generic.List<int> winners1Based)
    {
        esCongelados = true;
        gameObject.SetActive(true);
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        HideAllVictoryObjects();
        LimpiarIconosMultiples();
        if (textoGanador)
        {
            if (winners1Based != null && winners1Based.Count > 1)
                textoGanador.text = "Empate";
            else if (winners1Based != null && winners1Based.Count == 1)
                textoGanador.text = string.Format(formatoGanador, winners1Based[0]);
            else
                textoGanador.text = "";
        }
        var gm = IconManagerGeneral.Instance;
        if (gm != null && winners1Based != null)
        {
            int slot = 0;
            foreach (var w in winners1Based)
            {
                if (iconosMultiples != null && slot < iconosMultiples.Length)
                {
                    var img = iconosMultiples[slot];
                    if (img)
                    {
                        var sprite = gm.GetHappy(Mathf.Clamp(w - 1, 0, 3));
                        img.sprite = sprite;
                        img.enabled = sprite != null;
                    }
                }
                slot++;
            }
        }
        if (iconoGanador) iconoGanador.enabled = false; // ocultar el single si usamos múltiples
        IniciarAnimacionVictoria();
    }

    private void LimpiarIconosMultiples()
    {
        if (iconosMultiples == null) return;
        foreach (var img in iconosMultiples)
        {
            if (img)
            {
                img.enabled = false;
                img.sprite = null;
            }
        }
    }
}
