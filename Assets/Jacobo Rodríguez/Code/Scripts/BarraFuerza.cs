using UnityEngine;
using UnityEngine.UI; // Para ocultar/mostrar imágenes de UI
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BarraFuerza : MonoBehaviour
{
    // Inicial ahora en true: bloquea cualquier lanzamiento hasta la confirmación del botón listo.
    public static bool GlobalShakeBlocked = true; 

    [Header("Referencias UI")]
    [SerializeField] private RectTransform marcador;
    [SerializeField] private RectTransform barra;  
    [SerializeField] private GameObject advertenciaLanzar;

    [Header("Visual de Barra")]
    private MaskableGraphic[] _uiGraphics;
    private bool _barraOculta = false;

    [Header("Configuración")]
    [SerializeField] private float velocidad = 200f;   // Velocidad base (px/seg)
    [SerializeField] private float fuerzaMaxima = 100f; // Fuerza máxima posible
    [Tooltip("Fuerza mínima garantizada al lanzar, incluso con el marcador abajo")] 
    [SerializeField] private float fuerzaMinima = 25f;
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

    [Header("Gesto único (arriba)")]
    [SerializeField] private float upShakeThreshold = 1.6f;    // umbral para lanzar
    [SerializeField] private float pickupShakeThreshold = 1.2f; // umbral para atrapar (más permisivo)
    [SerializeField] private float lateralShakeDelay = 0.15f;  // pequeña espera tras lanzar
    private bool _lateralTapArmed = false;     // habilita pickup tras lanzar por shake
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

    [Header("Interacción (UI)")]
    [Tooltip("Imagen de UI que captura taps para lanzar o atrapar")]
    [SerializeField] private Image inputImage;
    private Button _inputButton;

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

        // Preparar overlay de interacción si existe
        SetupInputImage();
    }

    private void OnEnable()
    {
        // Reasignar por si fue desactivada/activada
        SetupInputImage();
    }

    private void SetupInputImage()
    {
        if (inputImage == null) return;
        // Asegurar que pueda recibir raycasts
        inputImage.raycastTarget = true;
        var btn = inputImage.GetComponent<Button>();
        if (btn == null)
        {
            btn = inputImage.gameObject.AddComponent<Button>();
        }
        if (_inputButton != btn)
        {
            if (_inputButton != null) _inputButton.onClick.RemoveAllListeners();
            _inputButton = btn;
            _inputButton.onClick.AddListener(OnInputImageClicked);
        }
    }

    private void OnInputImageClicked()
    {
        if (bolita == null) bolita = FindAnyObjectByType<Bolita>();
        if (bolita == null) return;

        if (bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar)
        {
            LanzarPorInteraccion();
        }
        else if (bolita.Estado == Bolita.EstadoLanzamiento.EnElAire && bolita.PorTocarSuelo)
        {
            var progression = FindAnyObjectByType<Progression>();
            progression?.NotificarBolitaTocada();
        }
    }

    // Permite lanzar usando el overlay o teclado (sin depender del shake)
    public void LanzarPorInteraccion()
    {
        if (bolita == null) bolita = FindAnyObjectByType<Bolita>();
        if (bolita == null) return;
        if (bolita.Estado != Bolita.EstadoLanzamiento.PendienteDeLanzar) return;

        // Lanzamiento como el de teclado
        _lastLaunchWasByShake = false;
        _lateralTapArmed = false;

        detenido = true;
        CalcularFuerza();
        SetBarraVisible(false);
        bolita.DarVelocidadHaciaArriba(fuerzaActual);
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

    /// Calcula la fuerza en base a la altura del marcador (0 a fuerzaMaxima), respetando un mínimo.
    private void CalcularFuerza()
    {
        float alturaNormalizada = (marcador.anchoredPosition.y + barra.rect.height / 2f) / barra.rect.height;
        float minF = Mathf.Clamp(fuerzaMinima, 0f, fuerzaMaxima);
        float rango = Mathf.Max(0f, fuerzaMaxima - minF);
        fuerzaActual = minF + alturaNormalizada * rango;
    }

    private void DetectarShakeYLanzar()
    {
        if (GlobalShakeBlocked) return; // no detectar shakes mientras está pausado esperando botón
        if (!usarShakeParaLanzar) return;
        if (bolita == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Accelerometer.current == null) return; // no hay acelerómetro
        Vector3 accelNow = UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue();
        _lowPassAccel = Vector3.Lerp(_lowPassAccel, accelNow, lowPassFactor);
        Vector3 delta = accelNow - _lowPassAccel;
#else
        _lowPassAccel = Vector3.Lerp(_lowPassAccel, Input.acceleration, lowPassFactor);
        Vector3 delta = Input.acceleration - _lowPassAccel;
#endif
        // Dirección "arriba" relativa al teléfono (opuesta a la gravedad estimada)
        Vector3 upDir = _lowPassAccel.sqrMagnitude > 1e-4f ? (-_lowPassAccel.normalized) : Vector3.up;
        float upComponent = Vector3.Dot(delta, upDir); // + arriba, - abajo
        float totalMag = delta.magnitude;

        // Estado y flags
        bool isPending = bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar;
        // Permitir atrapar por shake aunque el lanzamiento no haya sido por shake
        bool isAirTapWindow = bolita.Estado == Bolita.EstadoLanzamiento.EnElAire;
        bool cooldownReady = (Time.time - _ultimoShakeTime) >= shakeCooldown;
        bool delayReady = (Time.time - _launchByShakeTime) >= lateralShakeDelay;
        bool upOK = Mathf.Abs(upComponent) >= upShakeThreshold;           // lanzar
        bool upPickupOK = Mathf.Abs(upComponent) >= pickupShakeThreshold; // atrapar
        bool resetGuardReady = Time.time >= _shakeBlockedUntil; // guardado post-reset

        bool touchActive = false;
#if ENABLE_INPUT_SYSTEM
        touchActive = Touchscreen.current?.primaryTouch.isInProgress ?? false;
#else
        touchActive = Input.touchCount > 0;
#endif

        if (debugShakes && totalMag >= debugShakeLogThreshold && Time.time - _ultimoShakeLogTime >= debugLogMinInterval)
        {
            _ultimoShakeLogTime = Time.time;
            Debug.Log($"[ShakeDBG][RAW] total={totalMag:F2} up={upComponent:F2} state={bolita.Estado} detenido={detenido}");
            Debug.Log($"[ShakeDBG][EVAL] isPending={isPending} isAirTapWindow={isAirTapWindow} cooldownReady={cooldownReady} delayReady={delayReady} upOK={upOK} upPickupOK={upPickupOK} armed={_lateralTapArmed} porTocarSuelo={bolita.PorTocarSuelo} resetGuardReady={resetGuardReady}");
        }

        // 1) Lanzar cuando está pendiente: mismo gesto (arriba)
        if (isPending)
        {
            bool shouldLaunch = upOK && cooldownReady && resetGuardReady;
            if (debugShakes && totalMag >= debugShakeLogThreshold)
            {
                Debug.Log($"[ShakeDBG][PENDING] shouldLaunch={shouldLaunch} (upOK && cooldownReady && resetGuardReady)");
            }
            if (shouldLaunch)
            {
                _ultimoShakeTime = Time.time;
                detenido = true; // detiene la barra, pero se siguen leyendo shakes

                // Elegir fuente de fuerza: barra vs magnitud de shake
                float fuerzaLanzamiento;
                if (lanzarConbarra)
                {
                    CalcularFuerza();
                    fuerzaLanzamiento = fuerzaActual;
                }
                else
                {
                    // Mapear intensidad vertical a [fuerzaMinima..fuerzaMaxima]
                    float minF = Mathf.Clamp(fuerzaMinima, 0f, fuerzaMaxima);
                    float norm = Mathf.InverseLerp(upShakeThreshold, upShakeThreshold + 2.5f, Mathf.Abs(upComponent));
                    fuerzaLanzamiento = Mathf.Lerp(minF, fuerzaMaxima, norm);
                    fuerzaLanzamiento = Mathf.Clamp(fuerzaLanzamiento, minF, fuerzaMaxima);
                    fuerzaActual = fuerzaLanzamiento;
                }

                Debug.Log($"[ShakeDBG][ACT-LAUNCH] up={upComponent:F2} fuerza={fuerzaActual:F1}");
                SetBarraVisible(false);
                bolita.DarVelocidadHaciaArriba(fuerzaLanzamiento);

                // Armar pickup tras lanzamiento con shake
                _lastLaunchWasByShake = true;
                _lateralTapArmed = true;
                _launchByShakeTime = Time.time;
            }
        }

        // 2) Atrapar en el aire: mismo gesto (arriba)
        if (isAirTapWindow && _lateralTapArmed)
        {
            bool shouldTap = !touchActive && delayReady && upPickupOK && cooldownReady && bolita.PorTocarSuelo;
            if (debugShakes && totalMag >= debugShakeLogThreshold)
            {
                Debug.Log($"[ShakeDBG][AIR] shouldTap={shouldTap} (upPickupOK && delayReady && cooldown && porTocarSuelo) up={upComponent:F2}");
            }
            if (shouldTap)
            {
                _ultimoShakeTime = Time.time;
                _lateralTapArmed = false; // consumir

                var progression = FindAnyObjectByType<Progression>();
                progression?.NotificarBolitaTocada();

                Debug.Log($"[ShakeDBG][ACT-TAP] up={upComponent:F2}");
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
