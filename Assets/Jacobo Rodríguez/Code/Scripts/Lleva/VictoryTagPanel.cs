using UnityEngine;
using TMPro;

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
}
