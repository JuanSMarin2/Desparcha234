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
        victoryAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (esCongelados)
        {
            var proxyCong = victoryAnimator.GetComponent<CongeladosVictoryAnimationEventProxy>();
            if (proxyCong == null)
            {
                proxyCong = victoryAnimator.gameObject.AddComponent<CongeladosVictoryAnimationEventProxy>();
            }
        }
        else
        {
            var proxy = victoryAnimator.GetComponent<VictoryTagAnimationEventProxy>();
            if (proxy == null)
            {
                proxy = victoryAnimator.gameObject.AddComponent<VictoryTagAnimationEventProxy>();
                proxy.tagManager = TagManager.Instance;
            }
        }

        if (usarTrigger)
        {
            victoryAnimator.ResetTrigger(victoryStateName);
            victoryAnimator.SetTrigger(victoryStateName);
        }
        else
        {
            victoryAnimator.Play(victoryStateName, 0, 0f);
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
            t += Time.unscaledDeltaTime;
            yield return null;
            if (_finishedSignaled) yield break;
        }
        FinalizarSiNoSeñalado();
    }

    private void FinalizarSiNoSeñalado()
    {
        if (_finishedSignaled) return;
        _finishedSignaled = true;
        if (esCongelados)
        {
            TagCongelados.Instance?.FinalizeFromAnimationEvent();
        }
        else
        {
            TagManager.Instance?.OnVictoryAnimationFinished();
        }
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
        // Mostrar el panel sin activar objetos del modo clásico
        gameObject.SetActive(true);
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        HideAllVictoryObjects();
        LimpiarIconosMultiples();

        // Texto opcional
        if (textoGanador) textoGanador.text = string.Format(formatoGanador, playerIndex1Based);

        // Usar el icono múltiple fijo por jugador (índice = jugador-1)
        var gm = IconManagerGeneral.Instance;
        int idx0 = Mathf.Clamp(playerIndex1Based - 1, 0, 3);
        if (iconosMultiples != null && idx0 < iconosMultiples.Length)
        {
            var img = iconosMultiples[idx0];
            if (img && gm != null)
            {
                var sprite = gm.GetHappy(idx0);
                img.sprite = sprite;
                img.enabled = sprite != null;
                // Asegurar activación del GameObject externo
                if (sprite != null && !img.gameObject.activeSelf) img.gameObject.SetActive(true);
            }
        }
        // Asegurar que el icono "single" no se use en Congelados
        if (iconoGanador) iconoGanador.enabled = false;

        IniciarAnimacionVictoria();
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

        // Encender los iconos fijos de cada jugador ganador
        var gm = IconManagerGeneral.Instance;
        if (gm != null && winners1Based != null && iconosMultiples != null)
        {
            foreach (var w in winners1Based)
            {
                int idx0 = Mathf.Clamp(w - 1, 0, 3);
                if (idx0 < iconosMultiples.Length)
                {
                    var img = iconosMultiples[idx0];
                    if (img)
                    {
                        var sprite = gm.GetHappy(idx0);
                        img.sprite = sprite;
                        img.enabled = sprite != null;
                        // Asegurar activación del GameObject externo
                        if (sprite != null && !img.gameObject.activeSelf) img.gameObject.SetActive(true);
                    }
                }
            }
        }
        if (iconoGanador) iconoGanador.enabled = false; // ocultar el single
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
                // Nota: no desactivamos el GameObject aquí para no interferir con otros usos
            }
        }
    }
}

public class CongeladosVictoryAnimationEventProxy : MonoBehaviour
{
    public void OnVictoryAnimationFinished()
    {
        TagCongelados.Instance?.FinalizeFromAnimationEvent();
    }
    public void OnVictoryAnimationMidPoint() { }
}
