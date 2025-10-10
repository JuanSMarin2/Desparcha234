using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class PanelEliminacionTag : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconEliminado;
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private string formatoMensaje = "Jugador {0} eliminado";
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float fadeOut = 0.15f;

    [Header("Opcional: Colores por jugador")]
    [SerializeField] private bool recolorPorJugador;
    [SerializeField] private Image fondoRecolorDestino; // si no se asigna, usa el propio CanvasGroup/este objeto buscando un Image
    [SerializeField] private Color colorJugador1 = new Color(0.85f,0.25f,0.25f);
    [SerializeField] private Color colorJugador2 = new Color(0.25f,0.45f,0.9f);
    [SerializeField] private Color colorJugador3 = new Color(0.95f,0.85f,0.2f);
    [SerializeField] private Color colorJugador4 = new Color(0.3f,0.85f,0.4f);
    [Tooltip("Multiplicador opcional para atenuar color del fondo")] [SerializeField] private float fondoIntensity = 0.9f;

    [Header("Opcional: Reposicionamiento")]
    [Tooltip("Si está activo, intentará reposicionarse usando un ReposicionarPorTurno adjunto llamando ReposicionarPorEventoJugador")] [SerializeField] private bool autoReposicionar = true;

    private Action _onContinue;
    private bool _visible;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (botonSiguiente)
        {
            botonSiguiente.onClick.AddListener(OnClickSiguiente);
        }
        if (fondoRecolorDestino == null && recolorPorJugador)
        {
            // intentar encontrar un Image en este objeto para recolor
            fondoRecolorDestino = GetComponent<Image>();
        }
        gameObject.SetActive(false);
    }

    public void Show(int playerIndex1Based, Sprite icon, Action onContinue)
    {
        Debug.Log($"[PanelEliminacionTag] Show llamado jugador={playerIndex1Based} icon={(icon?icon.name:"null")} timeScale={Time.timeScale} objeto={gameObject.name}");
        _onContinue = onContinue;
        if (iconEliminado) iconEliminado.sprite = icon;
        if (textoMensaje) textoMensaje.text = string.Format(formatoMensaje, playerIndex1Based);
        gameObject.SetActive(true);
        _visible = true;

        // Reproducir SFX de eliminación al abrir el panel
        var sm = SoundManager.instance; if (sm != null) sm.PlaySfx("lleva:Eliminado");

        if (recolorPorJugador)
        {
            Color c = ColorForPlayer(playerIndex1Based - 1) * fondoIntensity;
            if (fondoRecolorDestino != null)
            {
                fondoRecolorDestino.color = c;
                Debug.Log($"[PanelEliminacionTag] Recolor aplicado fondo={fondoRecolorDestino.name} color={c}");
            }
            else
            {
                Debug.LogWarning("[PanelEliminacionTag] recolorPorJugador activo pero no hay fondoRecolorDestino");
            }
            ApplyColorToChildrenImages(c);
        }

        if (autoReposicionar)
        {
            var rep = GetComponent<ReposicionarPorTurno>();
            if (rep != null)
            {
                // mover=true, recolorear solo si el script de reposicionar lo maneja; instant true para evitar tween sobre panel
                rep.ReposicionarPorEventoJugador(playerIndex1Based - 1, mover: true, recolorear: false, instant: true);
                Debug.Log("[PanelEliminacionTag] Auto reposicionado usando ReposicionarPorTurno.");
            }
            else
            {
                Debug.LogWarning("[PanelEliminacionTag] autoReposicionar activo pero no hay ReposicionarPorTurno adjunto.");
            }
        }

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            StopAllCoroutines();
            StartCoroutine(FadeCanvas(0f, 1f, fadeIn));
        }
    }

    private void ApplyColorToChildrenImages(Color c)
    {
        var images = GetComponentsInChildren<Image>(includeInactive:true);
        foreach (var img in images)
        {
            if (img == iconEliminado) continue; // deja el ícono independiente si se quiere mantener su sprite original
            // Mantener alfa original del elemento si difiere (solo multiplicar RGB)
            float a = img.color.a;
            img.color = new Color(c.r, c.g, c.b, a);
        }
        Debug.Log($"[PanelEliminacionTag] Recolor aplicado a {images.Length} Image(s) hijas");
    }

    private Color ColorForPlayer(int idx)
    {
        switch (idx)
        {
            case 0: return colorJugador1; case 1: return colorJugador2; case 2: return colorJugador3; case 3: return colorJugador4; default: return Color.white;
        }
    }

    private System.Collections.IEnumerator FadeCanvas(float a, float b, float dur)
    {
        if (dur <= 0f)
        {
            canvasGroup.alpha = b; yield break;
        }
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(a, b, t / dur);
            yield return null;
        }
        canvasGroup.alpha = b;
    }

    public void OnClickSiguiente()
    {
        if (!_visible) return;
        Debug.Log("[PanelEliminacionTag] Botón siguiente presionado");
        _visible = false;
        if (canvasGroup)
        {
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }
        else
        {
            FinishAndCallback();
        }
    }

    private System.Collections.IEnumerator CloseRoutine()
    {
        yield return FadeCanvas(canvasGroup.alpha, 0f, fadeOut);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        FinishAndCallback();
    }

    private void FinishAndCallback()
    {
        Debug.Log("[PanelEliminacionTag] Callback continuar ejecutado");
        var cb = _onContinue; _onContinue = null;
        cb?.Invoke();
    }
}
