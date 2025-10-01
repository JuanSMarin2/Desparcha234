using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public enum PlayerSlot { Player1 = 1, Player2 = 2, Player3 = 3, Player4 = 4 }
    [Header("Jugador")] [SerializeField] private PlayerSlot playerSlot = PlayerSlot.Player1; // Dropdown en Inspector
    [Header("Movimiento Continuo")] [SerializeField] private float moveSpeed = 1.2f; // unidades/seg
    [Header("Rotación Idle")] [SerializeField] private float rotationSpeed = 120f; // grados/seg mientras está quieto

    [Header("Deslizamiento Física")] 
    [SerializeField] private bool usePhysicsSlide = true;
    [SerializeField] private LayerMask collisionMask = ~0; // todas por defecto
    [SerializeField, Tooltip("Pequeño padding para separarse de la pared y evitar vibración")] private float wallPadding = 0.01f;
    [SerializeField, Tooltip("Iteraciones máximas de resolución (por frame) para deslizar")] private int slideIterations = 2;

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

    private void Awake()
    {
        int idx = Mathf.Clamp(PlayerIndex1Based,1,4) - 1;
        _jugadores[idx] = this;
        _rb = GetComponent<Rigidbody2D>();
        if (!_rb)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic; // movimiento manual
        }
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(collisionMask);
        _contactFilter.useLayerMask = true;
        _contactFilter.useTriggers = false;
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
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }

        // Soporte alternativo: barra espaciadora controla al jugador 1 (hold)
        if (PlayerIndex1Based == 1)
        {
            if (Input.GetKeyDown(KeyCode.Space)) { StartHoldMove(); Debug.Log("[Movimiento] Space DOWN -> start hold Player1"); }
            if (Input.GetKeyUp(KeyCode.Space)) { StopHoldMove(); Debug.Log("[Movimiento] Space UP -> stop hold Player1"); }
        }
    }

    private void FixedUpdate()
    {
        if (!_holdRequested)
        {
            if (_isMoving) { _isMoving = false; OnMoveFinished?.Invoke(); }
            return;
        }

        Vector2 startPos = _rb.position;
        Vector2 desired = (Vector2)transform.up * (moveSpeed * Time.fixedDeltaTime);

        if (usePhysicsSlide && desired.sqrMagnitude > 0f)
        {
            Vector2 remaining = desired;
            for (int iter = 0; iter < slideIterations && remaining.sqrMagnitude > 0.0000001f; iter++)
            {
                int hitCount = _rb.Cast(remaining.normalized, _contactFilter, _hits, remaining.magnitude + wallPadding);
                if (hitCount == 0)
                {
                    // No colisión, mover todo lo restante
                    _rb.MovePosition(_rb.position + remaining);
                    remaining = Vector2.zero;
                    break;
                }
                // Seleccionar el hit más cercano válido
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
                // Mover hasta justo antes de la pared
                float travel = Mathf.Max(0f, minDist - wallPadding);
                Vector2 advance = remaining.normalized * travel;
                _rb.MovePosition(_rb.position + advance);
                remaining -= advance;
                // Proyectar el resto sobre el plano tangente para deslizar
                Vector2 n = closest.normal; // normal externa de la pared
                // Eliminar componente contra la normal
                float into = Vector2.Dot(remaining, n);
                if (into < 0f)
                {
                    remaining -= n * into; // quita componente perpendicular
                }
                else
                {
                    // Si ya no empuja hacia la pared, podemos aplicar directamente en siguiente iter.
                }
                // Seguridad para evitar bucle: si el ajuste es muy pequeño, terminar
                if (remaining.magnitude < 0.0001f) { remaining = Vector2.zero; break; }
            }
        }
        else
        {
            _rb.MovePosition(startPos + desired);
        }

        if (!_isMoving) { _isMoving = true; OnMoveStarted?.Invoke(); }
    }

    // Iniciar movimiento (pointer down / button down)
    public void StartHoldMove() { _holdRequested = true; }
    // Detener movimiento (pointer up / button up)
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

    public static void TriggerMoveForPlayer(int playerIdx1Based)
    {
        int idx = playerIdx1Based - 1; if (idx < 0 || idx >= _jugadores.Length) return;
        var m = _jugadores[idx]; if (m != null) m.TriggerMove();
    }
}
