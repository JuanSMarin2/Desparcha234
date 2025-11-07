using UnityEngine;

[DisallowMultipleComponent]
public class FootstepSound : MonoBehaviour
{
    [Header("Footstep SFX")]
    [Tooltip("Clave del SFX en el SoundManager")] [SerializeField] private string sfxKey = "footstep";
    [Tooltip("Intervalo entre pasos (segundos)")] [SerializeField] private float interval = 0.35f;
    [Tooltip("Volumen del paso (0..1)")] [SerializeField] private float volume = 1f;

    [Header("Opcional")]
    [Tooltip("Si está activo, solo suena cuando el jugador se está moviendo (requiere 'Movimiento' en el mismo objeto)")]
    [SerializeField] private bool playOnlyWhenMoving = true;

    private float _timer;
    private Movimiento _mov;

    private void Awake()
    {
        _mov = GetComponent<Movimiento>();
        interval = Mathf.Max(0.01f, interval);
    }

    private void OnEnable()
    {
        _timer = 0f; // empieza contando desde cero
        if (_mov != null)
        {
            _mov.OnMoveStarted += HandleMoveStarted;
            _mov.OnMoveFinished += HandleMoveFinished;
        }
    }

    private void OnDisable()
    {
        if (_mov != null)
        {
            _mov.OnMoveStarted -= HandleMoveStarted;
            _mov.OnMoveFinished -= HandleMoveFinished;
        }
    }

    private void HandleMoveStarted()
    {
        // Reproducir inmediatamente al presionar
        PlayStep();
        // Reiniciar contador para que el próximo ocurra tras 'interval' mientras se mantiene
        _timer = 0f;
    }

    private void HandleMoveFinished()
    {
        // Opcional: resetear para que al próximo press vuelva a sonar inmediato (lo maneja el evento igualmente)
        _timer = 0f;
    }

    private void Update()
    {
        // Si solo debe sonar al moverse y no hay movimiento, no avanzar el timer
        if (playOnlyWhenMoving && _mov != null && !_mov.IsMoving)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= interval)
        {
            _timer -= interval;
            PlayStep();
        }
    }

    private void PlayStep()
    {
        var sm = SoundManager.instance;
        if (sm != null && !string.IsNullOrEmpty(sfxKey))
        {
            sm.PlaySfx(sfxKey, volume);
        }
    }
}
