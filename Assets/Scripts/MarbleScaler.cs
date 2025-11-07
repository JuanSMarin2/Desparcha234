using UnityEngine;

[DisallowMultipleComponent]
public class MarbleScaler : MonoBehaviour
{
    [Header("Escalas")]
    [Tooltip("Factor de escala cuando se detecta TABLET. 0.5 por defecto.")]
    [SerializeField, Range(0.1f, 1f)] private float tabletScale = 0.5f;

    [Tooltip("Usar la escala inicial del objeto como 'normal'. Si se desactiva, se usa 'normalScaleOverride'.")]
    [SerializeField] private bool useInitialAsNormal = true;

    [Tooltip("Si 'useInitialAsNormal' está desactivado, se usará esta escala como normal (celular/desktop).")]
    [SerializeField] private Vector3 normalScaleOverride = Vector3.one;

    [Header("Detección (Auto)")]
    [Tooltip("Actualizar automáticamente si cambia la resolución/DPI.")]
    [SerializeField] private bool autoUpdateOnResolutionChange = true;

    [Header("Umbrales físicos (pulgadas)")]
    [Tooltip("Diagonal máxima considerada teléfono. Ej: 7.2")]
    [SerializeField] private float phoneDiagonalMaxInches = 7.2f;
    [Tooltip("Diagonal mínima considerada tablet. Ej: 7.5")]
    [SerializeField] private float tabletDiagonalMinInches = 7.5f;

    [Header("Validación de DPI")]
    [Tooltip("Mínimo DPI creíble para confiar en la medición (sube esto para iOS/Android).")]
    [SerializeField] private float minTrustedDpi = 180f;
    [Tooltip("Máximo DPI creíble (ruido por drivers).")]
    [SerializeField] private float maxTrustedDpi = 700f;

    [Header("Criterio adicional por aspecto (long/short)")]
    [Tooltip("Si el aspecto es >= a este valor, lo tratamos como 'teléfono' (alargado) incluso si el DPI dice otra cosa). 1.9–2.1 es típico.")]
    [SerializeField, Range(1.6f, 2.4f)] private float phoneAspectMin = 1.95f;

    [Header("Fallback por resolución (si DPI no fiable)")]
    [Tooltip("Máximo lado corto en píxeles para PHONE (<=)")]
    [SerializeField] private int fallbackPhoneShortSideMaxPx = 900;
    [Tooltip("Mínimo lado corto en píxeles para aplicar reducción (>=)")]
    [SerializeField] private int fallbackTabletShortSideMinPx = 1200;

    private Vector3 initialScale;
    private int lastW, lastH;
    private float lastDpi;

    void Awake()
    {
        initialScale = transform.localScale;
        lastW = Screen.width;
        lastH = Screen.height;
        lastDpi = Screen.dpi;
    }

    void Start()
    {
        ApplyScale();
    }

    void Update()
    {
        if (!autoUpdateOnResolutionChange) return;

        // Nota: hay plataformas donde el DPI se “mueve” o llega tarde.
        if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(Screen.dpi, lastDpi))
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastDpi = Screen.dpi;
            ApplyScale();
        }
    }

    public void ApplyScale()
    {
        bool large = IsLargeScreen();
        Vector3 baseNormal = useInitialAsNormal ? initialScale : normalScaleOverride;
        transform.localScale = large ? baseNormal * tabletScale : baseNormal;
    }

    private bool IsLargeScreen()
    {
        int w = Screen.width;
        int h = Screen.height;
        int shortSide = Mathf.Min(w, h);
        int longSide  = Mathf.Max(w, h);
        float aspect  = (float)longSide / Mathf.Max(1, shortSide);

        // 1) Intentar con DPI si es confiable y coherente
        float dpi = Screen.dpi;
        bool dpiTrusted = (dpi >= minTrustedDpi && dpi <= maxTrustedDpi);

        if (dpiTrusted)
        {
            float diagonalInches = Mathf.Sqrt(w * w + h * h) / dpi;

            // Si el aspecto es muy alargado, priorizamos tratarlo como teléfono
            if (aspect >= phoneAspectMin)
            {
                // Sólo considerar tablet si la diagonal es REALMENTE grande
                if (diagonalInches >= tabletDiagonalMinInches + 1.0f)
                    return true; // tablet "obvia"
                return false;    // teléfono por forma
            }

            // Rango sano por diagonal
            if (diagonalInches <= phoneDiagonalMaxInches) return false;   // phone
            if (diagonalInches >= tabletDiagonalMinInches) return true;   // tablet/grande

            // Zona gris: si el aspecto es bajo (no muy alto), seamos conservadores
            return false;
        }

        // 2) Fallback por resolución/forma: conservador para NO escalar celulares
        if (aspect >= phoneAspectMin)
            return false; // muy alargado => típico teléfono

        if (shortSide <= fallbackPhoneShortSideMaxPx)
            return false; // claro teléfono

        if (shortSide >= fallbackTabletShortSideMinPx)
            return true; // probable tablet

        return false; // por defecto, no tablet
    }
}
