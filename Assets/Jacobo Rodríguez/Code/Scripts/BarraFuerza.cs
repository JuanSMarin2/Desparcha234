using UnityEngine;
using UnityEngine.UI; // Para ocultar/mostrar imágenes de UI
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BarraFuerza : MonoBehaviour
{
    // Inicial ahora en true: bloquea cualquier lanzamiento hasta la confirmación del botón listo.
    public static bool GlobalShakeBlocked = true; // bloque global de shakes/entrada previo a lanzamiento

    [Header("Referencias UI")]
    [SerializeField] private RectTransform marcador; // Objeto que se mueve
    [SerializeField] private RectTransform barra;    // Barra con gradiente
    [SerializeField] private GameObject advertenciaLanzar;

    [Header("Visual de Barra")]
    // Ocultamos/mostramos los componentes de UI (MaskableGraphic: Image, Text, etc.) de este objeto y sus hijos
    private MaskableGraphic[] _uiGraphics;
    private bool _barraOculta = false;

    [Header("Configuración")]
    [SerializeField] private float velocidad = 200f;   // Velocidad base (px/seg)
    [SerializeField] private float fuerzaMaxima = 100f; // Fuerza máxima posible
    [Tooltip("Fracción mínima de la velocidad (0-1) para que nunca se detenga del todo")] 
    [SerializeField] private float factorMinVelocidad = 0.2f; 
    [Tooltip("Mayor que 1 para que suba lento y luego rápido; y baje rápido y luego lento")]
    [SerializeField] private float exponenteAceleracion = 2f;
    // Nuevo: elegir fuente de fuerza del lanzamiento
    [SerializeField] private bool lanzarConbarra = true;

    [Header("Objetivo")]
    [SerializeField] private Bolita bolita; // Referencia a la bolita con Rigidbody2D

    [Header("Shake (Móvil)")]
    [SerializeField] private bool usarShakeParaLanzar = true;
    [SerializeField] private float shakeThreshold = 2.2f;      // Intensidad aproximada (g) para disparar
    [SerializeField] private float shakeCooldown = 0.8f;       // Segundos entre lanzamientos por sacudida
    [SerializeField] private float lowPassFactor = 0.15f;      // 0-1 (menor = más suavizado)
    private Vector3 _lowPassAccel;
    private float _ultimoShakeTime;

    [Header("Shake direccional")]
    [SerializeField] private float upShakeThreshold = 1.6f;    // componente vertical mínima para lanzar
    [SerializeField] private float sideShakeThreshold = 1.0f;  // componente lateral mínima para "tocar"
    [SerializeField] private float lateralShakeDelay = 0.15f;  // pequeña espera tras lanzar
    private bool _lateralTapArmed = false;     // habilita tap lateral tras lanzar por shake
    private bool _lastLaunchWasByShake = false;
    private float _launchByShakeTime = -999f;

    [Header("Shake guard")]
    [Tooltip("Segundos tras reanudar el marcador en los que se bloquea el lanzamiento por shake")]
    [SerializeField] private float postResetShakeDelay = 1f;
    private float _shakeBlockedUntil = 0f;

    [Header("Debug shakes")]
    [SerializeField] private bool debugShakes = true;
    [Tooltip("Magnitud mínima del delta de aceleración para loggear (g aprox.)")]
    [SerializeField] private float debugShakeLogThreshold = 0.8f;
    [SerializeField] private float debugLogMinInterval = 0.1f;
    private float _ultimoShakeLogTime;

    private bool subiendo = true;   // Dirección del marcador
    private bool detenido = false;  // Estado de movimiento
    private float fuerzaActual;     // Fuerza calculada en el momento del click

    private void Awake()
    {
        if (bolita == null)
            bolita = FindAnyObjectByType<Bolita>();

        // Cachear todos los gráficos de UI propios e hijos para ocultar/mostrar sin desactivar scripts
        _uiGraphics = GetComponentsInChildren<MaskableGraphic>(true);

        // Inicial del filtro de acelerómetro usando el nuevo Input System si está disponible
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Accelerometer.current != null)
        {
            if (!UnityEngine.InputSystem.Accelerometer.current.enabled)
                InputSystem.EnableDevice(UnityEngine.InputSystem.Accelerometer.current);
            _lowPassAccel = UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue();
        }
        else
        {
            _lowPassAccel = Vector3.zero;
        }
#else
        _lowPassAccel = Input.acceleration; // fallback al Input antiguo
#endif
    }

    void Start()
    {
        if (!lanzarConbarra)
        {
            // Si no se usa la barra para lanzar, ocultar toda su UI desde el inicio
            SetBarraVisible(false);
            Debug.Log("LanzarConBarra desactivado: ocultando UI de la barra");
        }
    }
    private void Update()
    {
        if (GlobalShakeBlocked)
        {
            if (advertenciaLanzar != null) advertenciaLanzar.SetActive(false);
            return; // Pausa lógica: no mover marcador ni leer inputs
        }

        // Antes: if (detenido) return; -> Esto impedía detectar shakes en el aire
        if (marcador == null || barra == null) return;

        // Solo detener el movimiento de la barra si está detenido, pero seguir leyendo shakes
        if (!detenido)
        {
            MoverMarcador();
            if (advertenciaLanzar != null) advertenciaLanzar.SetActive(!_barraOculta);
        }
        else
        {
            if (advertenciaLanzar != null) advertenciaLanzar.SetActive(false);
        }

        DetectarShakeYLanzar(); // siempre leer shakes

        // Lanzar con teclado (ESPACIO o W) usando el nuevo Input System
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
        {
            Debug.Log("Lanzamiento manual con teclado (Input System)");
            if (bolita != null && bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar)
            {
                // Teclado: no arma el tap lateral
                _lastLaunchWasByShake = false;
                _lateralTapArmed = false;

                detenido = true;
                CalcularFuerza();
                // Ocultar barra visual al lanzar
                SetBarraVisible(false);
                bolita.DarVelocidadHaciaArriba(fuerzaActual);
            }
        }
#else
        // Fallback si no está el nuevo sistema compilado
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Lanzamiento manual con teclado (Legacy Input)");
            if (bolita != null && bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar)
            {
                // Teclado: no arma el tap lateral
                _lastLaunchWasByShake = false;
                _lateralTapArmed = false;

                detenido = true;
                CalcularFuerza();
                // Ocultar barra visual al lanzar
                SetBarraVisible(false);
                bolita.DarVelocidadHaciaArriba(fuerzaActual);
            }
        }
#endif
    }

    /// Mueve el marcador arriba y abajo dentro de los límites de la barra.
    private void MoverMarcador()
    {
        // Verificar límites superior e inferior
        float limiteSuperior = barra.rect.height / 2f;
        float limiteInferior = -barra.rect.height / 2f;

        Vector2 nuevaPos = marcador.anchoredPosition;

        // Normaliza la altura actual a [0,1] (0 = abajo, 1 = arriba)
        float yNorm = Mathf.InverseLerp(limiteInferior, limiteSuperior, nuevaPos.y);

        // Calcula un factor de velocidad no lineal para pasar más tiempo abajo SOLO al subir.
        // Al bajar, mantenemos velocidad constante para que no desacelere.
        float baseFactor = Mathf.Clamp01(factorMinVelocidad);
        float accel = Mathf.Max(1f, exponenteAceleracion);
        float curvaSubida = Mathf.Pow(yNorm, accel);
        float factor = subiendo ? Mathf.Lerp(baseFactor, 1f, curvaSubida) : 1f;

        float paso = velocidad * factor * Time.deltaTime;
        nuevaPos.y += subiendo ? paso : -paso;

        if (nuevaPos.y >= limiteSuperior)
        {
            nuevaPos.y = limiteSuperior;
            subiendo = false;
        }
        else if (nuevaPos.y <= limiteInferior)
        {
            nuevaPos.y = limiteInferior;
            subiendo = true;
        }

        marcador.anchoredPosition = nuevaPos;
    }

    /// Calcula la fuerza en base a la altura del marcador (0 a fuerzaMaxima).
    private void CalcularFuerza()
    {
        float alturaNormalizada = (marcador.anchoredPosition.y + barra.rect.height / 2f) / barra.rect.height;
        fuerzaActual = alturaNormalizada * fuerzaMaxima;
    }

    private void DetectarShakeYLanzar()
    {
        if (GlobalShakeBlocked) return; // no detectar shakes mientras está pausado esperando botón

        if (!usarShakeParaLanzar) return;
        if (bolita == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Accelerometer.current == null) return; // no hay acelerómetro
        // Filtro pasa-bajos para separar aceleración lenta de cambios bruscos usando nuevo Input System
        Vector3 accelNow = UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue();
        _lowPassAccel = Vector3.Lerp(_lowPassAccel, accelNow, lowPassFactor);
        Vector3 delta = accelNow - _lowPassAccel;
#else
        // Fallback al sistema antiguo
        _lowPassAccel = Vector3.Lerp(_lowPassAccel, Input.acceleration, lowPassFactor);
        Vector3 delta = Input.acceleration - _lowPassAccel;
#endif
        // Dirección "arriba" relativa al teléfono (opuesta a la gravedad estimada)
        Vector3 upDir = _lowPassAccel.sqrMagnitude > 1e-4f ? (-_lowPassAccel.normalized) : Vector3.up;
        float upComponent = Vector3.Dot(delta, upDir); // + arriba, - abajo
        Vector3 lateralVec = delta - upComponent * upDir;
        float lateralMag = lateralVec.magnitude;
        float totalMag = delta.magnitude;

        // Estado y flags
        bool isPending = bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar;
        bool isAirTapWindow = bolita.Estado == Bolita.EstadoLanzamiento.EnElAire && _lastLaunchWasByShake;
        bool cooldownReady = (Time.time - _ultimoShakeTime) >= shakeCooldown;
        bool delayReady = (Time.time - _launchByShakeTime) >= lateralShakeDelay;
        bool upOK = Mathf.Abs(upComponent) >= upShakeThreshold;
        bool sideOK = lateralMag >= sideShakeThreshold;
        bool upDominates = Mathf.Abs(upComponent) >= lateralMag;
        bool resetGuardReady = Time.time >= _shakeBlockedUntil; // nuevo guardado post-reset

        // Opcional: evita consumir un tap si hay un dedo en pantalla (reduce falsos positivos)
        bool touchActive = false;
#if ENABLE_INPUT_SYSTEM
        touchActive = Touchscreen.current?.primaryTouch.isInProgress ?? false;
#else
        touchActive = Input.touchCount > 0;
#endif

        // DEBUG: Log cada agite significativo con contexto del flujo
        if (debugShakes && totalMag >= debugShakeLogThreshold && Time.time - _ultimoShakeLogTime >= debugLogMinInterval)
        {
            _ultimoShakeLogTime = Time.time;
            Debug.Log($"[ShakeDBG][RAW] total={totalMag:F2} up={upComponent:F2} side={lateralMag:F2} state={bolita.Estado} detenido={detenido}");
            Debug.Log($"[ShakeDBG][EVAL] isPending={isPending} isAirTapWindow={isAirTapWindow} cooldownReady={cooldownReady} delayReady={delayReady} upOK={upOK} upDominates={upDominates} sideOK={sideOK} armed={_lateralTapArmed} porTocarSuelo={bolita.PorTocarSuelo} resetGuardReady={resetGuardReady}");
        }

        // 1) Evaluación de lanzamiento (pendiente)
        if (isPending)
        {
            bool shouldLaunch = upOK && upDominates && cooldownReady && resetGuardReady;
            if (debugShakes && totalMag >= debugShakeLogThreshold)
            {
                Debug.Log($"[ShakeDBG][PENDING] shouldLaunch={shouldLaunch} (upOK&&upDominates&&cooldownReady&&resetGuardReady)");
            }
            if (shouldLaunch)
            {
                _ultimoShakeTime = Time.time;
                detenido = true; // Detiene la barra, pero Update seguirá leyendo shakes

                // Elegir fuente de fuerza: barra vs magnitud de shake
                float fuerzaLanzamiento;
                if (lanzarConbarra)
                {
                    CalcularFuerza();
                    fuerzaLanzamiento = fuerzaActual;
                }
                else
                {
                    // Mapear intensidad vertical |upComponent| a [~25%..100%] de fuerzaMaxima
                    float norm = Mathf.InverseLerp(upShakeThreshold, upShakeThreshold + 2.5f, Mathf.Abs(upComponent));
                    fuerzaLanzamiento = Mathf.Lerp(fuerzaMaxima * 0.25f, fuerzaMaxima, norm);
                    fuerzaLanzamiento = Mathf.Clamp(fuerzaLanzamiento, 0f, fuerzaMaxima);
                    fuerzaActual = fuerzaLanzamiento; // para UI/consulta
                }

                Debug.Log($"[ShakeDBG][ACT-LAUNCH] up={upComponent:F2} side={lateralMag:F2} fuerza={fuerzaActual:F1}");
                // Ocultar barra visual al lanzar
                SetBarraVisible(false);
                bolita.DarVelocidadHaciaArriba(fuerzaLanzamiento);

                // Armar tap lateral solo si el lanzamiento fue por shake
                _lastLaunchWasByShake = true;
                _lateralTapArmed = true;
                _launchByShakeTime = Time.time;
            }
        }

        // 2) Evaluación de tap lateral en el aire (solo si está cerca del suelo)
        if (isAirTapWindow && _lateralTapArmed)
        {
            bool shouldTap = !touchActive && delayReady && sideOK && cooldownReady && bolita.PorTocarSuelo;
            if (debugShakes && totalMag >= debugShakeLogThreshold)
            {
                Debug.Log($"[ShakeDBG][AIR] shouldTap={shouldTap} (delayReady&&sideOK&&cooldownReady&&porTocarSuelo)");
            }
            if (shouldTap)
            {
                _ultimoShakeTime = Time.time;
                _lateralTapArmed = false; // consumir

                var progression = FindAnyObjectByType<Progression>();
                progression?.NotificarBolitaTocada();

                Debug.Log($"[ShakeDBG][ACT-TAP] up={upComponent:F2} side={lateralMag:F2}");
            }
        }
    }

    /// Devuelve la última fuerza calculada.
    public float ObtenerFuerza()
    {
        return fuerzaActual;
    }

    /// Reinicia el marcador para un nuevo intento.
    public void Reiniciar()
    {
        detenido = false;
        if (marcador != null && barra != null)
            marcador.anchoredPosition = new Vector2(marcador.anchoredPosition.x, -barra.rect.height / 2f);
        subiendo = true;

        // Bloqueo temporal de lanzamiento por shake tras reanudar
        _shakeBlockedUntil = Time.time + postResetShakeDelay;

        // Reset de flags de shake/tap
        _lateralTapArmed = false;
        _lastLaunchWasByShake = false;

        if (bolita == null) bolita = FindAnyObjectByType<Bolita>();
        if (bolita != null && bolita.Estado != Bolita.EstadoLanzamiento.EnElAire)
        {
            bolita.ReiniciarBola();
        }
        fuerzaActual = 0f; // Reiniciar fuerza
        
        // Mostrar barra visual nuevamente si corresponde
        if (lanzarConbarra)
            SetBarraVisible(true);

        Debug.Log("Barra de fuerza reiniciada.");
    }

    /// <summary>
    /// Llamar justo antes de permitir nuevamente los shakes (al cerrar la pantalla de listo).
    /// Limpia el estado del filtro y coloca un pequeño cooldown para que no se use un delta residual.
    /// </summary>
    public void PostResumeReset(float resumeIgnoreDuration = 0.3f)
    {
#if ENABLE_INPUT_SYSTEM
        if (Accelerometer.current != null)
        {
            // Releer aceleración actual y alinear el low-pass para que delta ~ 0 al primer frame
            _lowPassAccel = Accelerometer.current.acceleration.ReadValue();
        }
        else
        {
            _lowPassAccel = Vector3.zero;
        }
#else
        _lowPassAccel = Input.acceleration; // alinear al valor actual
#endif
        // Forzar cooldown de lanzamiento por shake
        _shakeBlockedUntil = Time.time + resumeIgnoreDuration; // reutiliza la misma compuerta resetGuardReady
        _ultimoShakeTime = Time.time; // asegura que cooldownReady también sea false unos ms
        _lateralTapArmed = false;
        _lastLaunchWasByShake = false;
        // Evitar que se lance inmediatamente por teclado: detenido permanece false (barra sigue moviéndose) hasta que el jugador decida.
        Debug.Log($"[ShakeDBG] PostResumeReset aplicado. Ignorando shakes por {resumeIgnoreDuration:F2}s");
    }

    public static void SetGlobalShakeBlocked(bool v) { GlobalShakeBlocked = v; }

    // Control centralizado de visual de barra
    public void SetBarraVisible(bool visible)
    {
        _barraOculta = !visible;

        // Mostrar/ocultar todos los gráficos de UI de este objeto y sus hijos
        if (_uiGraphics == null || _uiGraphics.Length == 0)
        {
            _uiGraphics = GetComponentsInChildren<MaskableGraphic>(true);
        }
        foreach (var g in _uiGraphics)
        {
            if (g == null) continue;
            g.enabled = visible;
        }

        // La advertencia sigue la visibilidad de la barra y el estado
        if (advertenciaLanzar != null)
        {
            advertenciaLanzar.SetActive(visible && !detenido && !GlobalShakeBlocked);
        }
    }

    // Llamar para ocultar toda la UI de la barra al finalizar el juego
    public void OcultarUIBarra()
    {
        var graphics = GetComponentsInChildren<MaskableGraphic>(true);
        foreach (var g in graphics) g.enabled = false;
        var go = gameObject;
        if (go) go.SetActive(false); // opcional: desactivar el GameObject entero
    }
}
