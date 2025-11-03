using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TagRoundStartPopup : MonoBehaviour
{
    [Header("Componentes UI")] 
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text texto;

    [Header("Contenido")] 
    [SerializeField] private string formato = "La lleva el {0}"; // ej: "La lleva el verde"
    [SerializeField] private bool colorizeText = true;

    [Header("Timing")] 
    [SerializeField] private float mostrarSegundos = 3f;
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float fadeOut = 0.12f;

    private System.Collections.IEnumerator _coActual;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void Show(int playerIndex1Based, string colorName, Color color)
    {
        if (texto)
        {
            texto.text = string.Format(formato, colorName);
            if (colorizeText) texto.color = color;
        }
        gameObject.SetActive(true);
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        if (_coActual != null) StopCoroutine(_coActual);
        _coActual = CoShow();
        StartCoroutine(_coActual);
    }

    public void HideImmediate()
    {
        if (_coActual != null) { StopCoroutine(_coActual); _coActual = null; }
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator CoShow()
    {
        // Fade-in unscaled
        float t = 0f;
        if (fadeIn > 0f)
        {
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeIn);
                yield return null;
            }
        }
        if (canvasGroup) canvasGroup.alpha = 1f;

        // Mantener visible
        float wait = 0f;
        while (wait < mostrarSegundos)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade-out
        t = 0f;
        if (fadeOut > 0f)
        {
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOut);
                yield return null;
            }
        }
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        _coActual = null;
    }
}
