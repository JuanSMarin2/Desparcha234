using System.Collections;
using UnityEngine;
using UnityEngine.UI; // agregado para soporte de Image UI

[DefaultExecutionOrder(200)]
public class ReposicionarPorTurno : MonoBehaviour
{
    [Header("Targets por jugador (0..3 => Jugador 1..4)")]
    [Tooltip("Índice 0 = Jugador 1, 1 = Jugador 2, etc.")]
    public Transform[] targets = new Transform[4];

    [Header("Opciones de movimiento")]
    [Tooltip("Si está activo, usará transform.localPosition en lugar de transform.position")] 
    public bool usarLocalPosition = false;

    [Tooltip("Si el objeto es UI, usa RectTransform y copia anchoredPosition de los targets (también RectTransform)")]
    public bool usarRectTransform = false;
    [Tooltip("Si está activo, tomará target.anchoredPosition en lugar de posición mundo/local.")]
    public bool usarAnchoredPosition = true;

    [Header("Rotación")]
    [Tooltip("Si está activo, también copiará la rotación del target")] 
    public bool aplicarRotacion = true;
    [Tooltip("Usar localRotation en vez de rotation al copiar la rotación")] 
    public bool usarLocalRotation = false;

    [Header("Anchors en Canvas (para objetos de mundo)")]
    [Tooltip("Actívalo si tus targets están en un Canvas (Screen Space) pero este objeto es de mundo")] 
    public bool targetsEnCanvas = false;
    [Tooltip("Canvas que contiene los targets UI (si es Screen Space). Si es Overlay, deja worldCamera vacío.")]
    public Canvas canvasUI;
    [Tooltip("Cámara del mundo donde existe este objeto (usualmente la principal)")]
    public Camera camMundo;
    [Tooltip("Usar el Z actual del objeto al proyectar desde pantalla a mundo")] 
    public bool usarZActualDelObjeto = true;
    [Tooltip("Z de mundo a usar si no se usa el Z actual del objeto (por ejemplo, el plano del suelo)")]
    public float zMundo = 0f;

    [Header("Tween opcional")]
    public bool suavizarMovimiento = false;
    [Min(0f)] public float duracion = 0.25f;
    public AnimationCurve curva = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Si se aplica al iniciar la escena, hacerlo animado (no instantáneo)")]
    public bool animarAlIniciar = false;

    [Header("Color por jugador (opcional)")]
    [Tooltip("Si está activo, al reposicionar se aplica un color distinto por jugador")] public bool cambiarColorPorJugador = false;
    public Color colorJugador1 = Color.red;
    public Color colorJugador2 = Color.blue;
    public Color colorJugador3 = Color.yellow;
    public Color colorJugador4 = Color.green;

    [Header("Recolocación por Evento (no turnos)")]
    [Tooltip("Targets alternativos cuando se reposiciona por evento (ej: jugador eliminado). Índice 0=J1")] public Transform[] eliminationTargets = new Transform[4];
    [Tooltip("Usar los eliminationTargets si están asignados; si no, usar 'targets'")] public bool usarEliminationTargets = true;
    [Tooltip("Aplicar color del jugador al reposicionar por evento")] public bool recolorAlEvento = true;
    [Tooltip("Desaturar ligeramente el color si es un jugador eliminado")] public bool desaturarEliminado = false;
    [Tooltip("Multiplicador de color para desaturar/eliminar (solo si desaturarEliminado=true)")] public Color desaturadoMultiplier = new Color(0.6f, 0.6f, 0.6f, 1f);

    private RectTransform _rt;
    private Coroutine _moveCo;
    private Coroutine _rotateCo;
    private bool _aplicadoInicio = false;
    private Coroutine _esperaInicioCo;

    // Caches para color
    private SpriteRenderer _spriteRenderer;
    private Image _image;

    void Awake()
    {
        _rt = usarRectTransform ? GetComponent<RectTransform>() : null;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _image = GetComponent<Image>();
    }

    void OnEnable()
    {
        Progression.OnTurnAdvanced += OnTurnAdvanced;
        if (!_aplicadoInicio)
        {
            if (_esperaInicioCo != null) StopCoroutine(_esperaInicioCo);
            _esperaInicioCo = StartCoroutine(CoAplicarAlInicio());
        }
    }

    void Start()
    {
        if (!_aplicadoInicio && TurnManager.instance != null)
        {
            int idx0 = TurnManager.instance.GetCurrentPlayerIndex();
            if (idx0 >= 0)
            {
                bool instant = !animarAlIniciar;
                ApplyPosition(idx0, instant);
                _aplicadoInicio = true;
                Debug.Log($"[ReposicionarPorTurno] Inicio aplicado (jugador {idx0 + 1}, instant={instant}), nombre el objeto reposicionado: {gameObject.name}");
            }
        }
    }

    void OnDisable()
    {
        Progression.OnTurnAdvanced -= OnTurnAdvanced;
        if (_esperaInicioCo != null) { StopCoroutine(_esperaInicioCo); _esperaInicioCo = null; }
    }

    private IEnumerator CoAplicarAlInicio()
    {
        float timeout = 1f; // espera hasta 1s por TurnManager
        while (timeout > 0f)
        {
            var tm = TurnManager.instance;
            int idx0 = (tm != null) ? tm.GetCurrentPlayerIndex() : -1;
            if (idx0 >= 0)
            {
                bool instant = !animarAlIniciar;
                ApplyPosition(idx0, instant);
                _aplicadoInicio = true;
                Debug.Log($"[ReposicionarPorTurno] Inicio aplicado (jugador {idx0 + 1}, instant={instant})");
                _esperaInicioCo = null;
                yield break;
            }
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.LogWarning("[ReposicionarPorTurno] Timeout esperando TurnManager al inicio. No se aplicó reposicionamiento inicial.");
        _esperaInicioCo = null;
    }

    private void OnTurnAdvanced(int playerIndex)
    {
        Debug.Log($"[ReposicionarPorTurno] Turno avanzado -> jugador {playerIndex + 1}, nombre el objeto reposicionado: {gameObject.name}");
        ApplyPosition(playerIndex, false);
    }

    // Nuevo: forzar reposicionamiento inmediato según el turno actual (útil si este objeto está inactivo)
    public void ApplyCurrentTurnPositionInstant()
    {
        int idx = (TurnManager.instance != null) ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
        if (idx >= 0) ApplyPosition(idx, true);
    }

    // Nuevo: forzar reposicionamiento inmediato para un índice específico de jugador (0-based)
    public void ApplyForPlayerIndexInstant(int playerIndexZeroBased)
    {
        ApplyPosition(playerIndexZeroBased, true);
    }

    /// <summary>
    /// Reposiciona (y opcionalmente recolorea) este objeto basado en un jugador eliminado u otro evento externo.
    /// No depende de turnos; útil en el modo Tag para ubicar paneles/indicadores.
    /// playerIndexZeroBased: 0..3 (Jugador 1..4)
    /// mover: si true mueve a la posición del target asignado.
    /// recolorear: si true aplica el color del jugador (con desaturación opcional).
    /// instant: si false y hay tween activado, usa animación.
    /// </summary>
    public void ReposicionarPorEventoJugador(int playerIndexZeroBased, bool mover = true, bool recolorear = true, bool instant = true)
    {
        Debug.Log($"[ReposicionarPorTurno] ReposicionarPorEventoJugador llamado -> jugador {(playerIndexZeroBased + 1)}, mover={mover}, (recolor desactivado en eventos), instant={instant}, usarEliminationTargets={usarEliminationTargets}, objeto={gameObject.name}");
        if (playerIndexZeroBased < 0 || playerIndexZeroBased >= targets.Length) return;
        Transform targetOverride = null;
        if (usarEliminationTargets && eliminationTargets != null && playerIndexZeroBased < eliminationTargets.Length)
        {
            targetOverride = eliminationTargets[playerIndexZeroBased];
            if (targetOverride != null)
                Debug.Log($"[ReposicionarPorTurno] Usando eliminationTarget para jugador {(playerIndexZeroBased + 1)}: {targetOverride.name}");
        }
        if (targetOverride == null)
        {
            targetOverride = targets != null && playerIndexZeroBased < targets.Length ? targets[playerIndexZeroBased] : null;
        }
        if (mover && targetOverride != null)
        {
            ApplyPositionCustom(playerIndexZeroBased, targetOverride, instant);
        }
        // Recolor por evento eliminado: desactivado (panel se encarga del color). Intento previo ignorado.
    }

    // Versión de ApplyPosition que acepta un target directo (sin usar el array principal)
    private void ApplyPositionCustom(int playerIndex, Transform customTarget, bool instant)
    {
        if (customTarget == null) return;

        // Reutilizar parte de la lógica de ApplyPosition pero con customTarget
        if (usarRectTransform && _rt != null)
        {
            var targetRt = customTarget as RectTransform;
            if (usarAnchoredPosition && targetRt != null)
            {
                Vector2 destino = targetRt.anchoredPosition;
                if (instant || !suavizarMovimiento || duracion <= 0f) _rt.anchoredPosition = destino; else StartMove(() => _rt.anchoredPosition, v => _rt.anchoredPosition = v, destino);
            }
            else
            {
                Vector3 destino = usarLocalPosition ? customTarget.localPosition : customTarget.position;
                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalPosition) _rt.localPosition = destino; else _rt.position = destino;
                }
                else
                {
                    if (usarLocalPosition) StartMove(() => _rt.localPosition, v => _rt.localPosition = v, destino); else StartMove(() => _rt.position, v => _rt.position = v, destino);
                }
            }
            if (aplicarRotacion)
            {
                Quaternion qDestino = usarLocalRotation ? customTarget.localRotation : customTarget.rotation;
                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalRotation) _rt.localRotation = qDestino; else _rt.rotation = qDestino;
                }
                else
                {
                    if (usarLocalRotation) StartRotate(() => _rt.localRotation, q => _rt.localRotation = q, qDestino); else StartRotate(() => _rt.rotation, q => _rt.rotation = q, qDestino);
                }
            }
        }
        else
        {
            Vector3 destino = usarLocalPosition ? customTarget.localPosition : customTarget.position;
            if (instant || !suavizarMovimiento || duracion <= 0f)
            {
                if (usarLocalPosition) transform.localPosition = destino; else transform.position = destino;
            }
            else
            {
                if (usarLocalPosition) StartMove(() => transform.localPosition, v => transform.localPosition = v, destino); else StartMove(() => transform.position, v => transform.position = v, destino);
            }
            if (aplicarRotacion)
            {
                Quaternion qDestino = usarLocalRotation ? customTarget.localRotation : customTarget.rotation;
                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalRotation) transform.localRotation = qDestino; else transform.rotation = qDestino;
                }
                else
                {
                    if (usarLocalRotation) StartRotate(() => transform.localRotation, q => transform.localRotation = q, qDestino); else StartRotate(() => transform.rotation, q => transform.rotation = q, qDestino);
                }
            }
        }

        // (Recolor por evento suprimido) Antes: if (cambiarColorPorJugador || recolorAlEvento) ApplyColorEvento(playerIndex);
    }

    private void ApplyColorEvento(int playerIndex)
    {
        Debug.Log($"[ReposicionarPorTurno] ApplyColorEvento inicio jugador {(playerIndex + 1)} (desaturarEliminado={desaturarEliminado}) objeto={gameObject.name}");
        ApplyColor(playerIndex);
        if (desaturarEliminado)
        {
            if (_spriteRenderer != null) _spriteRenderer.color = MultiplyColor(_spriteRenderer.color, desaturadoMultiplier);
            if (_image != null) _image.color = MultiplyColor(_image.color, desaturadoMultiplier);
            Debug.Log($"[ReposicionarPorTurno] Color desaturado aplicado jugador {(playerIndex + 1)} finalSprite={(_spriteRenderer!=null?_spriteRenderer.color.ToString():"none")} finalImage={(_image!=null?_image.color.ToString():"none")}");
        }
    }

    private Color MultiplyColor(Color baseColor, Color mult)
    {
        return new Color(baseColor.r * mult.r, baseColor.g * mult.g, baseColor.b * mult.b, baseColor.a * mult.a);
    }

    private void ApplyPosition(int playerIndex, bool instant)
    {
        if (playerIndex < 0 || playerIndex >= targets.Length) return;
        var target = targets[playerIndex];
        if (target == null) { Debug.LogWarning($"[ReposicionarPorTurno] Target nulo para jugador {playerIndex + 1}"); return; }

        if (targetsEnCanvas && (canvasUI == null || camMundo == null))
        {
            Debug.LogWarning("[ReposicionarPorTurno] targetsEnCanvas está activo, pero canvasUI o camMundo no están asignados.");
        }

        if (usarRectTransform && _rt != null)
        {
            var targetRt = target as RectTransform;
            // Posición
            if (usarAnchoredPosition && targetRt != null)
            {
                Vector2 destino = targetRt.anchoredPosition;
                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    _rt.anchoredPosition = destino;
                }
                else
                {
                    StartMove(() => _rt.anchoredPosition, v => _rt.anchoredPosition = v, destino);
                }
            }
            else
            {
                Vector3 destino = usarLocalPosition ? target.localPosition : target.position;
                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalPosition) _rt.localPosition = destino; else _rt.position = destino;
                }
                else
                {
                    if (usarLocalPosition)
                        StartMove(() => _rt.localPosition, v => _rt.localPosition = v, destino);
                    else
                        StartMove(() => _rt.position, v => _rt.position = v, destino);
                }
            }
            // Rotación
            if (aplicarRotacion)
            {
                Quaternion qDestino;
                if (targetRt != null)
                    qDestino = usarLocalRotation ? targetRt.localRotation : targetRt.rotation;
                else
                    qDestino = usarLocalRotation ? target.localRotation : target.rotation;

                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalRotation) _rt.localRotation = qDestino; else _rt.rotation = qDestino;
                }
                else
                {
                    if (usarLocalRotation)
                        StartRotate(() => _rt.localRotation, q => _rt.localRotation = q, qDestino);
                    else
                        StartRotate(() => _rt.rotation, q => _rt.rotation = q, qDestino);
                }
            }
            if (cambiarColorPorJugador) ApplyColor(playerIndex);
            return;
        }

        // Objeto no-UI
        {
            Vector3 destino;

            // Si los targets son UI en un Canvas de pantalla, convertir a mundo
            var targetRt = target as RectTransform;
            if (targetsEnCanvas && targetRt != null)
            {
                var uiCam = canvasUI != null ? canvasUI.worldCamera : null;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, targetRt.position);
                var cam = camMundo != null ? camMundo : Camera.main;

                float zDestino = usarZActualDelObjeto
                    ? (usarLocalPosition ? transform.localPosition.z : transform.position.z)
                    : zMundo;

                Vector3 sp3 = new Vector3(screen.x, screen.y, cam != null && !cam.orthographic
                    ? Mathf.Abs(zDestino - (cam != null ? cam.transform.position.z : 0f))
                    : zDestino);

                Vector3 world = cam != null ? cam.ScreenToWorldPoint(sp3) : (Vector3)screen;
                if (cam != null && cam.orthographic)
                {
                    world.z = zDestino; // asegurar el plano correcto en ortográfica
                }

                destino = usarLocalPosition && transform.parent != null
                    ? transform.parent.InverseTransformPoint(world)
                    : world;
            }
            else
            {
                destino = usarLocalPosition ? target.localPosition : target.position;
            }

            if (instant || !suavizarMovimiento || duracion <= 0f)
            {
                if (usarLocalPosition) transform.localPosition = destino; else transform.position = destino;
            }
            else
            {
                if (usarLocalPosition)
                    StartMove(() => transform.localPosition, v => transform.localPosition = v, destino);
                else
                    StartMove(() => transform.position, v => transform.position = v, destino);
            }

            // Rotación para objeto no-UI
            if (aplicarRotacion)
            {
                Quaternion qDestino;
                if (targetsEnCanvas && targetRt != null)
                {
                    // Para UI->mundo, copiar solo el ángulo Z del target UI
                    float angZ = usarLocalRotation ? targetRt.localEulerAngles.z : targetRt.eulerAngles.z;
                    qDestino = Quaternion.Euler(0f, 0f, angZ);
                }
                else
                {
                    qDestino = usarLocalRotation ? target.localRotation : target.rotation;
                }

                if (instant || !suavizarMovimiento || duracion <= 0f)
                {
                    if (usarLocalRotation) transform.localRotation = qDestino; else transform.rotation = qDestino;
                }
                else
                {
                    if (usarLocalRotation)
                        StartRotate(() => transform.localRotation, q => transform.localRotation = q, qDestino);
                    else
                        StartRotate(() => transform.rotation, q => transform.rotation = q, qDestino);
                }
            }
        }

        if (cambiarColorPorJugador) ApplyColor(playerIndex);
    }

    private void ApplyColor(int playerIndex)
    {
        Debug.Log("[ReposicionarPorTurno] Aplicando color para jugador " + (playerIndex + 1));
        Color c = colorJugador1;
        switch (playerIndex)
        {
            case 0: c = colorJugador1; break;
            case 1: c = colorJugador2; break;
            case 2: c = colorJugador3; break;
            case 3: c = colorJugador4; break;
        }
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = c;
            Debug.Log($"[ReposicionarPorTurno] Color aplicado a SpriteRenderer ({gameObject.name}) = {c}");
        }
        if (_image != null)
        {
            _image.color = c;
            Debug.Log($"[ReposicionarPorTurno] Color aplicado a Image ({gameObject.name}) = {c}");
        }
        if (_spriteRenderer == null && _image == null)
        {
            Debug.LogWarning("[ReposicionarPorTurno] No se encontró SpriteRenderer ni Image para aplicar color en objeto " + gameObject.name);
        }
    }

    private void StartMove(System.Func<Vector3> getter, System.Action<Vector3> setter, Vector3 destino)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(CoMove(getter, setter, destino));
    }

    private IEnumerator CoMove(System.Func<Vector3> getter, System.Action<Vector3> setter, Vector3 destino)
    {
        Vector3 origen = getter();
        float t = 0f;
        while (t < 1f)
        {
            t += (duracion > 0f ? Time.deltaTime / duracion : 1f);
            float e = curva != null ? curva.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
            setter(Vector3.LerpUnclamped(origen, destino, e));
            yield return null;
        }
        setter(destino);
        _moveCo = null;
    }

    private void StartRotate(System.Func<Quaternion> getter, System.Action<Quaternion> setter, Quaternion destino)
    {
        if (_rotateCo != null) StopCoroutine(_rotateCo);
        _rotateCo = StartCoroutine(CoRotate(getter, setter, destino));
    }

    private IEnumerator CoRotate(System.Func<Quaternion> getter, System.Action<Quaternion> setter, Quaternion destino)
    {
        Quaternion origen = getter();
        float t = 0f;
        while (t < 1f)
        {
            t += (duracion > 0f ? Time.deltaTime / duracion : 1f);
            float e = curva != null ? curva.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
            setter(Quaternion.SlerpUnclamped(origen, destino, e));
            yield return null;
        }
        setter(destino);
        _rotateCo = null;
    }
}
