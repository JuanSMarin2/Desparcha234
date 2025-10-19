using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class TransitionPanel : MonoBehaviour
{
    public static TransitionPanel Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform panel;     // El rect del panel (Image/UI)
    [SerializeField] private Canvas canvas;           // Canvas del panel (Screen Space Overlay recomendado)
    [SerializeField] private int sortingOrderOnTop = 10000;

    [Header("Tiempos / Easing")]
    [SerializeField] private float inDuration = 0.25f;      // cubrir (sube desde abajo hasta centro)
    [SerializeField] private float outDuration = 0.35f;      // salir (baja desde centro hasta fuera)
    [SerializeField] private Ease inEase = Ease.OutBack;   // entrada (cubrir)
    [SerializeField] private Ease outEase = Ease.InBack;    // salida (descubrir)

    // posiciones calculadas
    private Vector2 centerPos;      // cubriendo
    private Vector2 offBottomPos;   // fuera por ABAJO

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!panel) panel = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        // Asegurar pantalla completa
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        // Mantener siempre por encima
        if (canvas) canvas.sortingOrder = sortingOrderOnTop;

        RecomputePositions();

        // Al crearse, consideramos que está cubriendo (en el centro)
        panel.anchoredPosition = centerPos;

        // Nos suscribimos a cada carga de escena para hacer la salida automáticamente
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Si cambia la resolución/ratio, recalculamos
    void OnRectTransformDimensionsChange()
    {
        RecomputePositions();
    }

    private void RecomputePositions()
    {
        float h = GetCanvasHeight();
        centerPos = Vector2.zero;
        offBottomPos = new Vector2(0f, -h);
    }

    private float GetCanvasHeight()
    {
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return Screen.height * (canvas ? canvas.scaleFactor : 1f);

        return panel.rect.height > 0 ? panel.rect.height : Screen.height;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al entrar a cualquier escena nueva: el panel está persistente, así que lo colocamos “cubriendo”
        // y disparamos la salida hacia ABAJO.
        panel.DOKill();
        panel.anchoredPosition = centerPos;
        PlayOutDown();
    }

    /// <summary>
    /// Cubre: sube desde abajo hasta centro; cuando termina, ejecuta onCovered (ahí cargas la escena).
    /// </summary>
    public void CoverThen(System.Action onCovered)
    {
        panel.DOKill();
        // empezar fuera por ABAJO
        panel.anchoredPosition = offBottomPos;

        panel.DOAnchorPos(centerPos, inDuration)
             .SetEase(inEase)
             .OnComplete(() => onCovered?.Invoke());
    }

    /// <summary>
    /// Sale: baja desde el centro hasta fuera por ABAJO.
    /// </summary>
    public void PlayOutDown()
    {
        panel.DOKill();
        // aseguramos que parte desde centro
        if ((panel.anchoredPosition - centerPos).sqrMagnitude > 1f)
            panel.anchoredPosition = centerPos;

        panel.DOAnchorPos(offBottomPos, outDuration)
             .SetEase(outEase);
    }
}
