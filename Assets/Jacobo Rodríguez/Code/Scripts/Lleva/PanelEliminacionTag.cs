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
        gameObject.SetActive(false);
    }

    public void Show(int playerIndex1Based, Sprite icon, Action onContinue)
    {
        _onContinue = onContinue;
        if (iconEliminado) iconEliminado.sprite = icon;
        if (textoMensaje) textoMensaje.text = string.Format(formatoMensaje, playerIndex1Based);
        gameObject.SetActive(true);
        _visible = true;
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            StopAllCoroutines();
            StartCoroutine(FadeCanvas(0f, 1f, fadeIn));
        }
        else
        {
            // sin canvasGroup, simplemente visible
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
        var cb = _onContinue; _onContinue = null;
        cb?.Invoke();
    }
}
