using UnityEngine;

[DisallowMultipleComponent]
public class Obstaculo : MonoBehaviour
{
    [System.Flags]
    public enum Tipo
    {
        Fijo = 0,
        Empujable = 1 << 0,
        SeMueve = 1 << 1,
        Intermitente = 1 << 2,
        ApareceConElTiempo = 1 << 3,
    }

    [Header("Tipo de obstáculo")]
    [SerializeField] private Tipo tipo = Tipo.Fijo;

    // ---------------- Empujable ----------------
    [Header("Empujable (física)")]
    [Tooltip("Masa del obstáculo cuando es Empujable")] [SerializeField] private float masa = 2f;
    [Tooltip("Rozamiento lineal para frenar el deslizamiento")] [SerializeField] private float linearDamping = 3f;
    [Tooltip("Rozamiento angular para frenar giros")] [SerializeField] private float angularDamping = 0.5f;
    [Tooltip("Si está activo, convertirá todos los Collider2D hijos a no-Trigger")] [SerializeField] private bool forceNonTrigger = true;

    // ---------------- Movimiento A <-> B ----------------
    [Header("Movimiento entre puntos")]
    [Tooltip("Si se asignan, se usan estos Transforms para A y B. Si no, se usan puntos locales.")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;
    [Tooltip("Punto A relativo a la posición inicial si no se usan Transforms.")] [SerializeField] private Vector2 puntoALocal = Vector2.zero;
    [Tooltip("Punto B relativo a la posición inicial si no se usan Transforms.")] [SerializeField] private Vector2 puntoBLocal = Vector2.right * 3f;
    [SerializeField] private float velocidad = 2f;
    [Tooltip("Hace ping-pong entre A y B.")] [SerializeField] private bool pingPong = true;
    [Tooltip("Pausa en los extremos (segundos)")] [SerializeField] private float esperaEnExtremos = 0f;

    // ---------------- Intermitente ----------------
    [Header("Intermitente (aparece/desaparece)")]
    [Tooltip("Si inicia visible/activo (colliders + render)")] [SerializeField] private bool iniciarVisible = true;
    [Tooltip("Usar intervalos aleatorios entre encendido y apagado")]
    [SerializeField] private bool usarIntervalosAleatorios = true;
    [Tooltip("Rango de tiempo visible (min, max) si aleatorio")] [SerializeField] private Vector2 intervaloOn = new Vector2(2f, 3.5f);
    [Tooltip("Rango de tiempo oculto (min, max) si aleatorio")] [SerializeField] private Vector2 intervaloOff = new Vector2(1f, 2.5f);
    [Tooltip("Tiempo visible si NO es aleatorio")] [SerializeField] private float duracionOn = 3f;
    [Tooltip("Tiempo oculto si NO es aleatorio")] [SerializeField] private float duracionOff = 1.5f;

    // ---------------- ApareceConElTiempo ----------------
    [Header("Aparece con el tiempo (desde inicio de ronda)")]
    [Tooltip("Segundos que tarda en aparecer desde que la ronda empieza")] [SerializeField] private float delayAparicion = 2f;
    [Tooltip("Estado visible inicial al cargar escena (antes de la primera ronda)")] [SerializeField] private bool visibleEnEscenaAntesDeRonda = false;

    // Cache componentes
    private Rigidbody2D _rb;
    private Collider2D[] _colliders;
    private SpriteRenderer[] _sprites;

    // Estado movimiento
    private Vector2 _movA;
    private Vector2 _movB;
    private bool _moverHaciaB = true;
    private float _waitTimer = 0f;

    // Coroutines
    private System.Collections.IEnumerator _coIntermitente;
    private System.Collections.IEnumerator _coAparecer;

    private bool _aparecido = true; // gating de comportamientos dependiente del nuevo tipo

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        _sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        // Preparar puntos A/B
        Vector2 basePos = transform.position;
        _movA = puntoA ? (Vector2)puntoA.position : basePos + puntoALocal;
        _movB = puntoB ? (Vector2)puntoB.position : basePos + puntoBLocal;

        // Configurar según tipo
        if (Has(Tipo.Empujable))
        {
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = false; // puede rotar si choca
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.mass = Mathf.Max(0.01f, masa);
            _rb.linearDamping = Mathf.Max(0f, linearDamping);
            _rb.angularDamping = Mathf.Max(0f, angularDamping);
            if (forceNonTrigger && _colliders != null)
            {
                foreach (var c in _colliders) if (c != null && c.isTrigger) c.isTrigger = false;
            }
        }
        else if (Has(Tipo.SeMueve))
        {
            // Si solo se mueve, usar Kinematic para mover por script pero colisionar
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Intermitente: estado inicial
        if (Has(Tipo.Intermitente))
        {
            SetVisibleState(iniciarVisible);
        }

        // Aparece con el tiempo: estado inicial (antes de la ronda)
        if (Has(Tipo.ApareceConElTiempo))
        {
            _aparecido = visibleEnEscenaAntesDeRonda;
            ApplyAparecidoState(_aparecido);
        }
    }

    private void OnEnable()
    {
        if (Has(Tipo.Intermitente))
        {
            if (_coIntermitente != null) StopCoroutine(_coIntermitente);
            _coIntermitente = CoIntermitente();
            StartCoroutine(_coIntermitente);
        }
        // Suscribirse solo al inicio de ronda (la lógica de etapas se movió a AparecerEnStage)
        TagManager.OnRoundStarted += OnRoundStarted;
    }

    private void OnDisable()
    {
        if (_coIntermitente != null) { StopCoroutine(_coIntermitente); _coIntermitente = null; }
        if (_coAparecer != null) { StopCoroutine(_coAparecer); _coAparecer = null; }
        TagManager.OnRoundStarted -= OnRoundStarted;
        if (Has(Tipo.Empujable) && _rb != null)
        {
            // No forzar velocidades a cero; dejar a la física resolver
        }
    }

    private void FixedUpdate()
    {
        if (!Has(Tipo.SeMueve)) return;
        if (Has(Tipo.ApareceConElTiempo) && !_aparecido) return; // gatea movimiento hasta aparecer

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.fixedDeltaTime;
            if (_waitTimer > 0f) return;
        }

        Vector2 pos = _rb ? _rb.position : (Vector2)transform.position;
        Vector2 target = _moverHaciaB ? _movB : _movA;
        Vector2 to = target - pos;
        float dist = to.magnitude;
        float step = velocidad * Time.fixedDeltaTime;

        if (dist <= 0.001f)
        {
            // Llegó: decidir siguiente
            if (pingPong)
            {
                _moverHaciaB = !_moverHaciaB;
                if (esperaEnExtremos > 0f) _waitTimer = esperaEnExtremos;
            }
            else
            {
                // Teletransportar de B->A o detener
                _moverHaciaB = true;
                if (_rb) _rb.MovePosition(_movA); else transform.position = _movA;
            }
            // Frenar si es dinámico
            if (_rb && _rb.bodyType == RigidbodyType2D.Dynamic) _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = to / Mathf.Max(dist, 0.0001f);

        if (_rb)
        {
            if (_rb.bodyType == RigidbodyType2D.Dynamic && Has(Tipo.Empujable))
            {
                // Empujable + se mueve: usar velocidad deseada (interactúa con física)
                _rb.linearVelocity = dir * velocidad;
            }
            else
            {
                // Kinematic o sin empujable: MovePosition
                Vector2 newPos = pos + dir * Mathf.Min(step, dist);
                _rb.MovePosition(newPos);
            }
        }
        else
        {
            // Sin rigidbody (no recomendado): mover Transform
            transform.position = (Vector2)transform.position + dir * Mathf.Min(step, dist);
        }
    }

    private System.Collections.IEnumerator CoIntermitente()
    {
        // Alterna visible/oculto para siempre
        bool visible = iniciarVisible;
        while (true)
        {
            if (Has(Tipo.ApareceConElTiempo) && !_aparecido)
            {
                // Esperar a que aparezca para empezar/continuar el ciclo
                yield return null;
                continue;
            }

            float dur = usarIntervalosAleatorios
                ? (visible ? Random.Range(intervaloOn.x, intervaloOn.y) : Random.Range(intervaloOff.x, intervaloOff.y))
                : (visible ? Mathf.Max(0f, duracionOn) : Mathf.Max(0f, duracionOff));
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; // visible aunque haya pausas
                yield return null;
            }
            visible = !visible;
            SetVisibleState(visible);
        }
    }

    private void OnRoundStarted()
    {
        if (!Has(Tipo.ApareceConElTiempo)) return;
        // Reiniciar estado a oculto y lanzar temporizador basado en tiempo
        _aparecido = false;
        ApplyAparecidoState(false);
        if (_coAparecer != null) { StopCoroutine(_coAparecer); _coAparecer = null; }
        _coAparecer = CoAparecerTrasDelay(delayAparicion);
        StartCoroutine(_coAparecer);
    }

    private System.Collections.IEnumerator CoAparecerTrasDelay(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime; // usa tiempo no escalado por si hay pausa de UI
            yield return null;
        }
        _aparecido = true;
        ApplyAparecidoState(true);
    }

    public void StartRound()
    {
        OnRoundStarted();
    }

    private bool Has(Tipo t) => (tipo & t) != 0;

    private void SetVisibleState(bool visible)
    {
        // Render
        if (_sprites != null)
        {
            foreach (var s in _sprites) if (s) s.enabled = visible;
        }
        // Colliders
        if (_colliders != null)
        {
            foreach (var c in _colliders) if (c) c.enabled = visible;
        }
    }

    private void ApplyAparecidoState(bool visible)
    {
        SetVisibleState(visible);
        // Opcional: al ocultar, si es dinámico, detener movimiento para evitar energías fantasma
        if (!visible && _rb && _rb.bodyType == RigidbodyType2D.Dynamic)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    // ---------------- API pública para modificar masa/drag en runtime ----------------
    public void SetMass(float m)
    {
        masa = Mathf.Max(0.01f, m);
        if (_rb) _rb.mass = masa;
    }
    public void SetLinearDamping(float d)
    {
        linearDamping = Mathf.Max(0f, d);
        if (_rb) _rb.linearDamping = linearDamping;
    }
    public void SetAngularDamping(float d)
    {
        angularDamping = Mathf.Max(0f, d);
        if (_rb) _rb.angularDamping = angularDamping;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Dibujar A/B
        Gizmos.color = Color.yellow;
        Vector2 basePos = Application.isPlaying ? (Vector2)transform.position : (Vector2)transform.position;
        Vector2 a = puntoA ? (Vector2)puntoA.position : basePos + puntoALocal;
        Vector2 b = puntoB ? (Vector2)puntoB.position : basePos + puntoBLocal;
        Gizmos.DrawSphere(a, 0.08f);
        Gizmos.DrawSphere(b, 0.08f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
