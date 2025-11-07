using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public enum PlayerSlot { Player1 = 1, Player2 = 2, Player3 = 3, Player4 = 4 }
    public enum FacingAxis { Up, Right, Down, Left, Custom }
    [Header("Jugador")] [SerializeField] private PlayerSlot playerSlot = PlayerSlot.Player1; // Dropdown en Inspector
    [Header("Frente / Dirección Local")]
    [Tooltip("Eje local que se considera 'frente' para avanzar")] [SerializeField] private FacingAxis facingAxis = FacingAxis.Up;
    [Tooltip("Vector2 local usado si FacingAxis=Custom (ej: (-1,0) para izquierda). Se normaliza automáticamente")] [SerializeField] private Vector2 customLocalForward = Vector2.up;
    [Header("Movimiento Continuo")] [SerializeField] private float moveSpeed = 1.2f; // unidades/seg
    [Header("Rotación Idle")] [SerializeField] private float rotationSpeed = 130f; // grados/seg mientras está quieto
    [Header("Rotación cuando tiene Tag (opcional)")]
    [Tooltip("Si está activo, cuando el jugador tiene el Tag usará 'rotationSpeedTagged' en lugar de 'rotationSpeed'")]
    [SerializeField] private bool useTaggedRotationSpeed = false;
    [SerializeField] private float rotationSpeedTagged = 180f;

    [Header("Física Jugador vs Jugador")]
    [Tooltip("Si está activo, este jugador usa Rigidbody2D Dynamic y puede empujar/ser empujado por otros jugadores")]
    [SerializeField] private bool enablePlayerPush = true;
    [Tooltip("Si está activo y el empuje está habilitado, convierte todos los Collider2D de este objeto y sus hijos a no-Trigger para asegurar colisiones físicas")]
    [SerializeField] private bool forceAllCollidersNonTrigger = true;
    [Tooltip("Si está activo y hay empuje, el movimiento usa velocity en lugar de MovePosition (mejor transferencia de impulso)")]
    [SerializeField] private bool useVelocityWhenPushing = true;
    [Tooltip("Drag lineal para reducir deslizamiento cuando el rigidbody es Dynamic")] [SerializeField] private float dynamicLinearDrag = 4f;
    [Tooltip("Drag angular para reducir giros por impacto cuando el rigidbody es Dynamic")] [SerializeField] private float dynamicAngularDrag = 0.5f;
    [Tooltip("Masa del jugador cuando es Dynamic (afecta cuánto empuja y cuánto lo empujan)")] [SerializeField] private float dynamicMass = 2f;
    public bool IsPlayerPushEnabled => enablePlayerPush;

    [Header("Deslizamiento Física")] 
    [SerializeField] private bool usePhysicsSlide = true;
    [SerializeField] private LayerMask collisionMask = ~0; // todas por defecto
    [SerializeField, Tooltip("Pequeño padding para separarse de la pared y evitar vibración")] private float wallPadding = 0.01f;
    [SerializeField, Tooltip("Iteraciones máximas de resolución (por frame) para deslizar")] private int slideIterations = 2;

    [Header("Animator")]
    [SerializeField] private Animator _anim; // Animator en hijo
    [SerializeField] private string isMovingParam = "IsMoving";

    private bool _isMoving = false; // true mientras se mantiene presionado
    private bool _holdRequested = false; // estado de input presionado

    public bool IsMoving => _isMoving;

    private static Movimiento[] _jugadores = new Movimiento[4];

    public System.Action OnMoveStarted; // opcional
    public System.Action OnMoveFinished; // opcional

    private int PlayerIndex1Based => (int)playerSlot; // reemplaza antiguo playerIndex int

    private Rigidbody2D _rb;
    private ContactFilter2D _contactFilter;
    private RaycastHit2D[] _hits = new RaycastHit2D[8];

    private int _rotationDir = -1; // dirección actual (1 o -1)
    private PlayerTag _playerTag; // para saber si tiene Tag
    private Vector3 _initialPosition;

    private void Awake()
    {
        int idx = Mathf.Clamp(PlayerIndex1Based,1,4) - 1;
        _jugadores[idx] = this;
        _rb = GetComponent<Rigidbody2D>();
        if (!_rb)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = enablePlayerPush ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }
        else
        {
            // Si ya existía, ajustar tipo según opción
            _rb.bodyType = enablePlayerPush ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }

        // Guardar posición inicial al iniciar la escena
        _initialPosition = transform.position;

        // Config generales
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        if (enablePlayerPush)
        {
            // Mejor resolución de choques al moverse con MovePosition
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // Dejar que Box2D resuelva empujes entre jugadores -> no usar nuestro slide manual
            usePhysicsSlide = false;

            // Ajustes de física para controlar fricción/deslizamiento
            _rb.linearDamping = dynamicLinearDrag;
            _rb.angularDamping = dynamicAngularDrag;
            _rb.mass = dynamicMass;

            if (forceAllCollidersNonTrigger)
            {
                var colls = GetComponentsInChildren<Collider2D>(includeInactive: true);
                int changed = 0;
                foreach (var c in colls)
                {
                    if (c == null) continue;
                    if (c.isTrigger)
                    {
                        c.isTrigger = false;
                        changed++;
                    }
                }
                if (changed > 0)
                {
                    Debug.Log($"[Movimiento] Cambiados {changed} Collider2D a no-Trigger para empuje en {name}");
                }
            }
        }
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(collisionMask);
        _contactFilter.useLayerMask = true;
        _contactFilter.useTriggers = false;

        if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
        // cachear PlayerTag para consultar si está taggeado
        _playerTag = GetComponent<PlayerTag>();
    }

    private void OnEnable()
    {
        TagManager.OnRoundStarted += OnRoundStarted_ResetToInitial;
    }

    private void OnDisable()
    {
        TagManager.OnRoundStarted -= OnRoundStarted_ResetToInitial;
    }

    private void OnDestroy()
    {
        int idx = Mathf.Clamp(PlayerIndex1Based,1,4) - 1;
        if (_jugadores[idx] == this) _jugadores[idx] = null;
    }

    void Update()
    {
        // Rotación idle sólo visual aquí (física en FixedUpdate) cuando no se sostiene
        if (!_holdRequested)
        {
            float rot = rotationSpeed;
            if (useTaggedRotationSpeed && _playerTag != null && _playerTag.IsTagged)
            {
                rot = rotationSpeedTagged;
            }
            transform.Rotate(0f, 0f, _rotationDir * rot * Time.deltaTime);
        }

        // Soporte alternativo: barra espaciadora controla al jugador 1 (hold)
        if (PlayerIndex1Based == 1)
        {
            if (Input.GetKeyDown(KeyCode.Space)) { StartHoldMove(); Debug.Log("[Movimiento] Space DOWN -> start hold Player1"); }
            if (Input.GetKeyUp(KeyCode.Space)) { StopHoldMove(); Debug.Log("[Movimiento] Space UP -> stop hold Player1"); }
        }
    }

    private void SetAnimIsMoving(bool value)
    {
        if (_anim == null) return;
        if (_anim.HasParameterOfType(isMovingParam, AnimatorControllerParameterType.Bool))
            _anim.SetBool(isMovingParam, value);
    }

    private Vector2 GetForwardDir()
    {
        Vector2 localDir;
        switch (facingAxis)
        {
            case FacingAxis.Right: localDir = Vector2.right; break;
            case FacingAxis.Down: localDir = Vector2.down; break;
            case FacingAxis.Left: localDir = Vector2.left; break;
            case FacingAxis.Custom: localDir = customLocalForward; break;
            case FacingAxis.Up:
            default: localDir = Vector2.up; break;
        }
        if (localDir.sqrMagnitude < 0.0001f) localDir = Vector2.up;
        // Transformar dirección local a mundo considerando la rotación actual del objeto
        Vector3 world = transform.TransformDirection(new Vector3(localDir.x, localDir.y, 0f));
        return ((Vector2)world).normalized;
    }

    private void FixedUpdate()
    {
        if (!_holdRequested)
        {
            if (_isMoving) { _isMoving = false; OnMoveFinished?.Invoke(); SetAnimIsMoving(false); }
            return;
        }

        Vector2 startPos = _rb.position;
        Vector2 forward = GetForwardDir();
        Vector2 desired = forward * (moveSpeed * Time.fixedDeltaTime);

        if (enablePlayerPush && useVelocityWhenPushing)
        {
            // Movimiento por velocidad: mejor interacción con otros cuerpos dinámicos
            _rb.linearVelocity = forward * moveSpeed;
        }
        else if (usePhysicsSlide && desired.sqrMagnitude > 0f)
        {
            Vector2 remaining = desired;
            for (int iter = 0; iter < slideIterations && remaining.sqrMagnitude > 0.0000001f; iter++)
            {
                int hitCount = _rb.Cast(remaining.normalized, _contactFilter, _hits, remaining.magnitude + wallPadding);
                if (hitCount == 0)
                {
                    _rb.MovePosition(_rb.position + remaining);
                    remaining = Vector2.zero;
                    break;
                }
                RaycastHit2D closest = default;
                float minDist = float.MaxValue;
                bool found = false;
                for (int h = 0; h < hitCount; h++)
                {
                    var ht = _hits[h];
                    if (ht.collider == null) continue;
                    if (ht.distance < minDist)
                    {
                        minDist = ht.distance; closest = ht; found = true;
                    }
                }
                if (!found)
                {
                    _rb.MovePosition(_rb.position + remaining);
                    remaining = Vector2.zero; break;
                }
                float travel = Mathf.Max(0f, minDist - wallPadding);
                Vector2 advance = remaining.normalized * travel;
                _rb.MovePosition(_rb.position + advance);
                remaining -= advance;
                Vector2 n = closest.normal;
                float into = Vector2.Dot(remaining, n);
                if (into < 0f)
                {
                    remaining -= n * into;
                }
                if (remaining.magnitude < 0.0001f) { remaining = Vector2.zero; break; }
            }
        }
        else
        {
            _rb.MovePosition(startPos + desired);
        }

        if (!_isMoving) { _isMoving = true; OnMoveStarted?.Invoke(); SetAnimIsMoving(true); }
    }

    public void StartHoldMove() { if(!_holdRequested){ _rotationDir *= -1; } _holdRequested = true; }
    public void StopHoldMove() { _holdRequested = false; }

    public static void StartHoldForPlayer(int playerIdx1Based)
    {
        int idx = playerIdx1Based - 1; if (idx < 0 || idx >= _jugadores.Length) return;
        var m = _jugadores[idx]; if (m) m.StartHoldMove();
    }
    public static void StopHoldForPlayer(int playerIdx1Based)
    {
        int idx = playerIdx1Based - 1; if (idx < 0 || idx >= _jugadores.Length) return;
        var m = _jugadores[idx]; if (m) m.StopHoldMove();
    }

    public void TriggerMove()
    {
        StartHoldMove();
        Invoke(nameof(StopHoldMove), 0f);
        Debug.Log($"[Movimiento] TriggerMove jugador {PlayerIndex1Based}");
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        // Si estamos moviendo por velocidad, sincronizar inmediatamente
        if (enablePlayerPush && useVelocityWhenPushing && _holdRequested) { _rb.linearVelocity = GetForwardDir() * moveSpeed; }
    }

    public void ResetToInitialPosition()
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.position = _initialPosition;
        }
        else
        {
            transform.position = _initialPosition;
        }
    }

    private void OnRoundStarted_ResetToInitial()
    {
        ResetToInitialPosition();
    }
}
