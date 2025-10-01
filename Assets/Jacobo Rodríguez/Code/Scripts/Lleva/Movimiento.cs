using UnityEngine;

public class Movimiento : MonoBehaviour
{
    [Header("Jugador")] [Range(1,4)] [SerializeField] private int playerIndex = 1; // 1..4
    [Header("Movimiento")] [SerializeField] private float moveDistance = 3f; // distancia por avance
    [SerializeField] private float moveSpeed = 5f;           // velocidad de desplazamiento
    [Header("Rotación Idle")] [SerializeField] private float rotationSpeed = 120f; // grados/seg mientras está quieto

    private bool _isMoving = false;
    private Vector3 _targetPos;
    private Coroutine _moveRoutine;

    public bool IsMoving => _isMoving;

    // Registro estático (1..4)
    private static Movimiento[] _jugadores = new Movimiento[4];

    public System.Action OnMoveStarted; // opcional
    public System.Action OnMoveFinished; // opcional

    private void Awake()
    {
        int idx = Mathf.Clamp(playerIndex,1,4) - 1;
        _jugadores[idx] = this;
    }

    private void OnDestroy()
    {
        int idx = Mathf.Clamp(playerIndex,1,4) - 1;
        if (_jugadores[idx] == this) _jugadores[idx] = null;
    }

    void Update()
    {
        if (!_isMoving)
        {
            // Rotar continuamente sobre Z (top‑down 2D)
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    // Llamar desde el botón del jugador correspondiente
    public void TriggerMove()
    {
        if (_isMoving) return;
        Vector3 dir = transform.up; // frente en top‑down
        _targetPos = transform.position + dir * moveDistance;
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveRoutine());
    }

    // Opción global: disparar por índice 1..4
    public static void TriggerMoveForPlayer(int playerIdx1Based)
    {
        int idx = playerIdx1Based - 1; if (idx < 0 || idx >= _jugadores.Length) return;
        var m = _jugadores[idx]; if (m != null) m.TriggerMove();
    }

    private System.Collections.IEnumerator MoveRoutine()
    {
        _isMoving = true; OnMoveStarted?.Invoke();
        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
            if ((transform.position - _targetPos).sqrMagnitude < 0.0001f) break;
            yield return null;
        }
        transform.position = _targetPos;
        _isMoving = false; OnMoveFinished?.Invoke();
    }
}
