using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class Bolita : MonoBehaviour
{
    public enum EstadoLanzamiento
    {
        PendienteDeLanzar, // "pendiente de lanzar"
        EnElAire,          // "en el aire"
        TocadaPorJugador,  // "tocada por el jugador"
        Fallado             // "fallado"
    }

    [Header("Física")]
    [SerializeField] private string tagSuelo = "Suelo"; // Asegúrate de poner este tag al objeto de suelo
    [SerializeField] private Transform puntoReinicio;    // Opcional: posición para reiniciar
    [SerializeField] private float gravedadAlLanzar = 0.65f; // Gravedad al lanzar
    private Rigidbody2D _rb;
    private EstadoLanzamiento _estado = EstadoLanzamiento.PendienteDeLanzar;

    // Notifica cuando cambia el estado (útil para UI Manager)
    public event Action<EstadoLanzamiento> OnEstadoCambio;

    
    public EstadoLanzamiento Estado => _estado;
    [SerializeField] private SpriteRenderer _sprite;

    // --- Top‑Down Visual (fake 3D) ---
    [Header("Top‑Down Visual (fake 3D)")]
    [Tooltip("Nodo hijo que contiene el SpriteRenderer principal a escalar visualmente")] 
    [SerializeField] private Transform spriteRoot;   // ideal: un hijo con el SpriteRenderer
    [Tooltip("Sombra (sprite circular) en el piso para vender profundidad")] 
    [SerializeField] private Transform shadow;       // hijo con SpriteRenderer (círculo)

    [Tooltip("Factor base de escala por unidad de altura (interpolado por fuerza)")] 
    [SerializeField] private float scalePerUnitMin = 0.12f;
    [SerializeField] private float scalePerUnitMax = 0.30f;
    [SerializeField] private float maxVisualScale = 2.0f;  // escala visual máxima

    [Header("Desaparición por altura")]
    [Tooltip("Si la escala visual supera este valor, ocultar el sprite (solo queda la sombra)")]
    [SerializeField] private float disappearScaleThreshold = 1.65f;

    [Header("Fuerza (para visual)")]
    [Tooltip("Fuerza máxima esperada para normalizar el efecto visual")] 
    [SerializeField] private float maxExpectedForce = 100f;

    [Header("Sombra (seguimiento piso)")]
    [SerializeField] private bool keepSpriteAtGround = true; // anclar visual al piso (no sube en pantalla)
    [SerializeField] private float shadowShrinkPerUnit = 0.25f; // cuanto encoge la sombra por unidad de altura
    [SerializeField] private float shadowMinScale = 0.55f;  // escala mínima de la sombra
    [SerializeField, Range(0f,1f)] private float shadowMinAlpha = 0.25f; // alpha mínima de la sombra

    [Header("Virtual Z (simulación)")]
    [Tooltip("Si está activo, NO mover en Y. Se simula altura en un eje Z virtual con gravedad, y el contacto con suelo ocurre cuando z<=0.")]
    [SerializeField] private bool useVirtualZ = true;
    [Tooltip("Gravedad aplicada al eje Z virtual (negativa hacia abajo)")]
    [SerializeField] private float gravedadVirtualZ = -9.81f;
    [Tooltip("Arrastre lineal en Z (0 = sin arrastre). Valores 0.2–1 dan caída más suave")]
    [SerializeField, Range(0f, 3f)] private float dragZ = 0.6f;

    [Header("Advertencia de recoger bola")]
    [Tooltip("Si la altura virtual Z está por debajo de este valor (pero > 0), se mostrará la advertencia para recoger la bola.")]
    [SerializeField] private float distanciaAdvertenciaZ = 1.0f;
    [SerializeField] private bool usarBolaEnPantalla = true;
    private float _launchY;
    private float _launchForce;
    private float _launchForceNorm; // fuerza normalizada [0,1]
    private Vector3 _spriteBaseScale = Vector3.one;
    private Vector3 _shadowBaseScale = Vector3.one;
    private Vector3 _spriteBaseLocalPos;
    private Vector3 _shadowBaseLocalPos;
    private SpriteRenderer _shadowSr;

    // Estado del eje Z virtual
    private float _z = 0f;   // altura virtual sobre el suelo
    private float _vz = 0f;  // velocidad vertical virtual

    // Flag público para otros sistemas (p.ej. BarraFuerza)
    public bool PorTocarSuelo { get; private set; }

    private UiManager _ui;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();

        // Inicialización visual
        if (spriteRoot == null)
        {
            if (_sprite != null) spriteRoot = _sprite.transform;
            else spriteRoot = transform;
        }
        _spriteBaseScale = spriteRoot.localScale;
        _spriteBaseLocalPos = spriteRoot.localPosition;

        if (shadow != null)
        {
            _shadowSr = shadow.GetComponent<SpriteRenderer>();
            _shadowBaseScale = shadow.localScale;
            _shadowBaseLocalPos = shadow.localPosition;
        }

        _ui = FindAnyObjectByType<UiManager>();
    }

    void Start()
    {
        _rb.gravityScale = 0f; // Desactivar gravedad al inicio
        _rb.linearVelocity = Vector2.zero;
        NotificarEstado(_estado);
        // Asegurar que la advertencia inicie oculta
        ComunicarPorTocarSuelo(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Simulación Z virtual: integrar solo cuando está en el aire
        if (useVirtualZ && _estado == EstadoLanzamiento.EnElAire)
        {
            _vz += gravedadVirtualZ * Time.deltaTime;
            // Aplicar arrastre lineal en Z para suavizar la subida/bajada
            if (dragZ > 0f)
            {
                _vz -= dragZ * _vz * Time.deltaTime;
            }
            _z += _vz * Time.deltaTime;

            // Asegurar que no se mueva físicamente en Y
            if (_rb != null)
            {
                var vel = _rb.linearVelocity;
                if (Mathf.Abs(vel.y) > 0.0001f)
                {
                    vel.y = 0f;
                    _rb.linearVelocity = vel;
                }
                if (_rb.gravityScale != 0f)
                {
                    _rb.gravityScale = 0f; // sin gravedad física en modo Z virtual
                }
            }

            // Advertencia: cercano al suelo (pero aún en el aire) SOLO cuando va bajando
            bool descending = _vz <= 0f;

            // Calcular visibilidad del sprite en base a la escala visual s y el umbral de desaparición
            float alturaForVis = useVirtualZ ? Mathf.Max(0f, _z) : Mathf.Max(0f, transform.position.y - _launchY);
            float scalePerUnitVis = Mathf.Lerp(scalePerUnitMin, scalePerUnitMax, Mathf.Clamp01(_launchForceNorm));
            float sVis = Mathf.Clamp(1f + alturaForVis * scalePerUnitVis, 1f, maxVisualScale);
            bool bolaVisible = sVis < disappearScaleThreshold; // true si no está desaparecida

            // Dos criterios posibles para mostrar advertencia
            bool criterioNearGround = _z > 0f && _z <= distanciaAdvertenciaZ;
            bool criterioBolaEnPantalla = descending && bolaVisible; // solicitado: descendiendo y visible

            bool mostrarAdvertencia = descending && (usarBolaEnPantalla ? criterioBolaEnPantalla : criterioNearGround);
            if (mostrarAdvertencia != PorTocarSuelo)
            {
                ComunicarPorTocarSuelo(mostrarAdvertencia);
            }

            // Suelo virtual
            if (_z <= 0f)
            {
                _z = 0f;
                if (PorTocarSuelo) ComunicarPorTocarSuelo(false);
                CambiarEstado(EstadoLanzamiento.Fallado);

                // SFX: error/caída
                var sm = GameObject.Find("SoundManager");
                if (sm != null)
                {
                    sm.SendMessage("SonidoError", SendMessageOptions.DontRequireReceiver);
                }

                var progressionZ = FindAnyObjectByType<Progression>();
                progressionZ?.PerderPorTocarSuelo();
            }
        }

        // Visual fake 3D: escalar sprite y ajustar sombra mientras esté en el aire
        if (_estado == EstadoLanzamiento.EnElAire && spriteRoot != null)
        {
            float altura = useVirtualZ
                ? Mathf.Max(0f, _z)
                : Mathf.Max(0f, transform.position.y - _launchY);

            // Efecto depende de fuerza: interpola scalePerUnit por fuerza normalizada
            float scalePerUnit = Mathf.Lerp(scalePerUnitMin, scalePerUnitMax, Mathf.Clamp01(_launchForceNorm));
            float s = Mathf.Clamp(1f + altura * scalePerUnit, 1f, maxVisualScale);

            // Ocultar sprite si supera threshold (queda solo sombra)
            if (_sprite != null)
                _sprite.enabled = s < disappearScaleThreshold;

            // Escala visual del sprite (aunque esté oculto, mantenemos para cuando vuelva)
            spriteRoot.localScale = _spriteBaseScale * s;

            // Mantener sprite "pegado" al piso: en Z virtual no hay subida en Y física, así que no compensamos
            if (keepSpriteAtGround && spriteRoot != transform)
            {
                var lp = spriteRoot.localPosition;
                lp.y = useVirtualZ ? _spriteBaseLocalPos.y : _spriteBaseLocalPos.y - altura; // cancelar solo si sube físicamente
                spriteRoot.localPosition = lp;
            }

            // Sombra en el piso: seguir piso y ajustar escala/alpha
            if (shadow != null)
            {
                // mantener en piso (si es hijo lo dejamos en su base)
                if (shadow != transform)
                {
                    var slp = shadow.localPosition;
                    slp.y = _shadowBaseLocalPos.y; // fijo en piso
                    shadow.localPosition = slp;
                }

                float sh = Mathf.Clamp(1f - altura * shadowShrinkPerUnit, shadowMinScale, 1f);
                shadow.localScale = _shadowBaseScale * sh;

                if (_shadowSr != null)
                {
                    var c = _shadowSr.color;
                    // A menor sombra (más "alta" la bola), menor alpha
                    float t = Mathf.InverseLerp(1f, shadowMinScale, sh); // 0 cuando sh=1, 1 cuando sh=shadowMinScale
                    float a = Mathf.Lerp(1f, shadowMinAlpha, t);
                    _shadowSr.color = new Color(c.r, c.g, c.b, a);
                }
            }
        }
    }

    // Nuevo: lanzar hacia ARRIBA.
    public void DarVelocidadHaciaArriba(float fuerza)
    {
        if (_rb == null) return;

        CambiarEstado(EstadoLanzamiento.EnElAire);
        ComunicarPorTocarSuelo(false); // al iniciar el vuelo, aún lejos del suelo

        if (useVirtualZ)
        {
            // En modo Z virtual: no usar Y física ni gravedad 2D
            _rb.gravityScale = 0f;
            Vector2 v2 = _rb.linearVelocity;
            v2.y = 0f; // no subir en Y
            _rb.linearVelocity = v2;

            _z = 0f;                       // partimos del suelo virtual
            _vz = Mathf.Abs(fuerza);       // velocidad inicial en Z virtual
        }
        else
        {
            _rb.gravityScale = gravedadAlLanzar; // Activar gravedad al lanzar (física 2D)
            // Interpretamos "fuerza" como velocidad objetivo en m/s hacia arriba
            Vector2 v = _rb.linearVelocity;
            v.y = Mathf.Abs(fuerza);
            _rb.linearVelocity = v;
        }

        // Base para "altura visual" y fuerza
        _launchY = transform.position.y;
        _launchForce = Mathf.Abs(fuerza);
        _launchForceNorm = maxExpectedForce > 0f ? Mathf.Clamp01(_launchForce / maxExpectedForce) : 1f;

        // Nuevo: notificar a Progression que la bola fue lanzada para que spawnee Jacks
        var progression = FindAnyObjectByType<Progression>();
        progression?.OnBallLaunched();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // En modo Z virtual, ignorar colisiones físicas con el suelo; el contacto se maneja por z<=0
        if (useVirtualZ) return;
        if (_estado != EstadoLanzamiento.EnElAire) return;
        if (!collision.collider.CompareTag(tagSuelo)) return;

        // Toca suelo -> pierde turno
        CambiarEstado(EstadoLanzamiento.Fallado);
        ComunicarPorTocarSuelo(false);

        // SFX: error/caída
        var sm = GameObject.Find("SoundManager");
        if (sm != null)
        {
            sm.SendMessage("SonidoError", SendMessageOptions.DontRequireReceiver);
        }

        Progression progression = FindAnyObjectByType<Progression>();
        if (progression != null)
        {
            progression.PerderPorTocarSuelo();
        }
    }

    // public void OnMouseDown() { ... }

    public void ReiniciarBola()
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) return;
        }
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.gravityScale = 0f; // asegurar desactivación
        _z = 0f; _vz = 0f;      // reset estado virtual
        ComunicarPorTocarSuelo(false);
        Debug.Log($"Bolita reiniciada. gravityScale={_rb.gravityScale}");
        if (puntoReinicio != null)
        {
            transform.position = puntoReinicio.position;
            transform.rotation = puntoReinicio.rotation;
        }
        if (_sprite != null)
        {
            _sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b, 1f);
            Debug.Log("Sprite restaurado a alpha 1" + _sprite.color.r + _sprite.color.g + _sprite.color.b + _sprite.color.a);
        }
        // Reset visual (fake 3D)
        if (spriteRoot != null)
        {
            spriteRoot.localScale = _spriteBaseScale;
            spriteRoot.localPosition = _spriteBaseLocalPos;
        }
        if (_sprite != null) _sprite.enabled = true;
        if (shadow != null)
        {
            shadow.localScale = _shadowBaseScale;
            shadow.localPosition = _shadowBaseLocalPos;
            if (_shadowSr != null)
            {
                var c = _shadowSr.color;
                _shadowSr.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
        CambiarEstado(EstadoLanzamiento.PendienteDeLanzar);
        var progression = FindAnyObjectByType<Progression>();
        progression?.OnballePendingThrow();

    }

    private void CambiarEstado(EstadoLanzamiento nuevo)
    {
        if (_estado == nuevo) return;
        _estado = nuevo;
        NotificarEstado(_estado);
    }

    private void NotificarEstado(EstadoLanzamiento e)
    {
        OnEstadoCambio?.Invoke(e);
    }

    // Método público para comunicar el estado PorTocarSuelo al UI Manager
    public void ComunicarPorTocarSuelo(bool valor)
    {
        PorTocarSuelo = valor;
        if (_ui == null) _ui = FindAnyObjectByType<UiManager>();
        if (_ui != null)
        {
            _ui.SendMessage("MostrarAdvertenciaRecoger", valor, SendMessageOptions.DontRequireReceiver);
        }
    }
}
