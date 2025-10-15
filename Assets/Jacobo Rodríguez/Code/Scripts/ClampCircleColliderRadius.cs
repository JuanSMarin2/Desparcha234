  using UnityEngine;

// Mantiene el radio "en mundo" del CircleCollider2D por debajo de un máximo serializado,
// aunque el Transform escale. Devuelve al radio base cuando la escala baja.
[ExecuteAlways]
[RequireComponent(typeof(CircleCollider2D))]
public class ClampCircleColliderRadius : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private CircleCollider2D circle; // si se deja vacío se toma el del mismo objeto

    [Header("Límites (mundo)")]
    [Tooltip("Radio máximo permitido en unidades de mundo (tras aplicar escala del Transform)")]
    [Min(0f)] [SerializeField] private float maxWorldRadius = 0.5f;
    [Tooltip("Opcional: radio mínimo en mundo para evitar colisionador demasiado pequeño (0 = ignorar)")]
    [Min(0f)] [SerializeField] private float minWorldRadius = 0f;

    [Header("Ajustes")]
    [Tooltip("Captura automáticamente el radio actual como base al iniciar")]
    [SerializeField] private bool captureBaseOnAwake = true;
    [Tooltip("Usar el mayor factor entre X e Y para calcular el radio en mundo")]
    [SerializeField] private bool useMaxXYScale = true;

    [Tooltip("Mostrar logs en modo Play para depuración")]
    [SerializeField] private bool debugLogs = false;

    // Radio local con el que queremos operar cuando no hay clamp activo.
    [SerializeField, HideInInspector] private float baseLocalRadius = -1f;

    private void Reset()
    {
        circle = GetComponent<CircleCollider2D>();
        if (circle != null) baseLocalRadius = circle.radius;
    }

    private void Awake()
    {
        if (circle == null) circle = GetComponent<CircleCollider2D>();
        if (circle == null) return;
        if (captureBaseOnAwake || baseLocalRadius <= 0f)
        {
            baseLocalRadius = circle.radius;
        }
        ApplyClamp();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (circle == null) circle = GetComponent<CircleCollider2D>();
        // Evitar valores inválidos
        if (maxWorldRadius < 0f) maxWorldRadius = 0f;
        if (minWorldRadius < 0f) minWorldRadius = 0f;
        // No forzar baseLocalRadius aquí para no sobreescribir el del usuario
        ApplyClamp();
    }
#endif

    private void Update()
    {
        // En Play y en Editor (ExecuteAlways) se mantiene el clamp activo
        ApplyClamp();
    }

    private void ApplyClamp()
    {
        if (circle == null) return;
        if (maxWorldRadius <= 0f)
        {
            // Si el máximo es 0, deshabilitar collider efectivo
            circle.radius = 0f;
            return;
        }

        // Factor de escala efectivo (2D): usa el mayor entre X e Y para aproximar el radio en mundo
        Vector3 ls = circle.transform.lossyScale;
        float scaleFactor = useMaxXYScale ? Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y)) : Mathf.Abs(ls.x);
        if (scaleFactor <= 1e-6f) scaleFactor = 1e-6f; // evitar división por cero

        // Radio deseado en mundo si usamos el radio base
        float desiredWorld = baseLocalRadius * scaleFactor;

        float targetLocal = baseLocalRadius;
        if (desiredWorld > maxWorldRadius)
        {
            targetLocal = maxWorldRadius / scaleFactor;
        }
        else if (minWorldRadius > 0f && desiredWorld < minWorldRadius)
        {
            targetLocal = minWorldRadius / scaleFactor;
        }

        // Aplicar sólo si cambia para evitar settear cada frame
        if (!Mathf.Approximately(circle.radius, targetLocal))
        {
            if (debugLogs && Application.isPlaying)
            {
                Debug.Log($"[ClampCircleColliderRadius] Ajuste radius local {circle.radius:F3} -> {targetLocal:F3} (worldMax={maxWorldRadius:F3}, scale={scaleFactor:F3})", this);
            }
            circle.radius = targetLocal;
        }
    }

    [ContextMenu("Capture current as base radius")]
    public void CaptureCurrentAsBase()
    {
        if (circle == null) circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            baseLocalRadius = circle.radius;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    // Permite cambiar el máximo en runtime y aplicar inmediatamente
    public void SetMaxWorldRadius(float maxR)
    {
        maxWorldRadius = Mathf.Max(0f, maxR);
        ApplyClamp();
    }
}
