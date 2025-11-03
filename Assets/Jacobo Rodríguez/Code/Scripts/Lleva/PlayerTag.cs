using UnityEngine;

[RequireComponent(typeof(Movimiento))]
public class PlayerTag : MonoBehaviour
{
    [Header("Identidad")] [SerializeField] private int playerIndex1Based = 1; // 1..4

    // Velocidades centralizadas en TagManager

    [Header("Feedback Visual")]
    [Tooltip("Objeto de outline / resalte que se activa cuando este jugador está taggeado")] 
    [SerializeField] private GameObject outlineObject;

    [Header("Animator")] 
    [SerializeField] private Animator _anim; // Animator en hijo
    [SerializeField] private string hasTagParam = "HasTag";

    [Header("Transferencia Tag")] 
    [SerializeField] private float transferCooldown = 0f; // instantáneo por defecto
    [Tooltip("Usar colisiones físicas (OnCollisionEnter2D) en lugar de triggers para transferir")] [SerializeField] private bool usarColisionFisica = true;
    [Tooltip("Si true mantiene también el trigger para otras detecciones (dual)")] [SerializeField] private bool mantenerTriggerParaDeteccion = false;

    [Header("Efectos de Sonido")]
    [Tooltip("Sonido que reproduce al tocar objetos con tag 'planta'")]
    [SerializeField] private string plantaSfxKey = "planta";

    private float _lastTransferTime = -999f;
    private int _lastTransferFrame = -999;

    private bool _isTagged;
    private Movimiento _mov;

    // Fallbacks por si TagManager no existe (editor/test)
    private const float _movDefaultNormal = 3f;
    private const float _movDefaultTagged = 5f;

    public bool IsTagged => _isTagged;
    public int PlayerIndex => playerIndex1Based;

    private void Awake()
    {
        _mov = GetComponent<Movimiento>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
        // Si el movimiento tiene empuje activado (rigidbody dinámico), aseguramos colisión física para transferir
        if (_mov != null && _mov.IsPlayerPushEnabled) usarColisionFisica = true;
        ApplySpeed();
        UpdateVisuals();
        SetAnimHasTag(_isTagged);
    }

    public void SetTagged(bool value, bool playFeedback = true)
    {
        if (_isTagged == value) return;
        _isTagged = value;
        ApplySpeed();
        UpdateVisuals();
        SetAnimHasTag(_isTagged);
        if (playFeedback)
        {
            Debug.Log($"[PlayerTag] Player {playerIndex1Based} tagged={(value?"YES":"NO")}");
        }
    }

    private void SetAnimHasTag(bool value)
    {
        if (_anim == null) return;
        if (_anim.HasParameterOfType(hasTagParam, AnimatorControllerParameterType.Bool))
            _anim.SetBool(hasTagParam, value);
    }

    private void ApplySpeed()
    {
        if (_mov == null) return;
        var tm = TagManager.Instance;
        float speed = _isTagged ? (tm ? tm.MoveSpeedTagged : _movDefaultTagged) : (tm ? tm.MoveSpeedNormal : _movDefaultNormal);
        _mov.SetMoveSpeed(speed);
    }

    private void UpdateVisuals()
    {
        if (outlineObject) outlineObject.SetActive(_isTagged);
    }

    // Lógica central de transferencia (trigger o colisión)
    private void HandleTagTransfer(PlayerTag otherTag)
    {
        if (!_isTagged) return; // solo el actual taggeado transfiere
        if (otherTag == null || otherTag == this) return;
        if (otherTag._isTagged) return; // ya lo tiene
        if (transferCooldown > 0f && Time.time - _lastTransferTime < transferCooldown) return; // sin delay si cooldown=0
        if (Time.frameCount == _lastTransferFrame) return; // evitar doble en mismo frame

        _lastTransferTime = Time.time;
        otherTag._lastTransferTime = Time.time;
        _lastTransferFrame = Time.frameCount;
        otherTag._lastTransferFrame = Time.frameCount;

        // Transferir: este jugador deja de estar tag y el otro lo recibe
        SetTagged(false);
        var sm = SoundManager.instance;
        if (sm != null)
        {
            sm.PlaySfx("lleva:TagTransfer");
        }
        otherTag.SetTagged(true);
        // Rotar 180° al que entregó el tag (media vuelta)
        transform.Rotate(0f, 0f, 180f);
        TagManager.Instance?.NotifyTagChanged(this, otherTag);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (usarColisionFisica && !mantenerTriggerParaDeteccion) return; // ignorar triggers si se usa colisión física pura
        
        // Detectar colisión con plantas
        if (other.CompareTag("planta"))
        {
            PlayPlantaSound();
        }
        
        var otherTag = other.GetComponentInParent<PlayerTag>();
        HandleTagTransfer(otherTag);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!usarColisionFisica) return;
        
        // Detectar colisión con plantas también en modo físico
        if (collision.collider.CompareTag("planta"))
        {
            PlayPlantaSound();
        }
        
        var otherTag = collision.collider.GetComponentInParent<PlayerTag>();
        HandleTagTransfer(otherTag);
    }

    private void PlayPlantaSound()
    {
        var sm = SoundManager.instance;
        if (sm != null && !string.IsNullOrEmpty(plantaSfxKey))
        {
            sm.PlaySfx(plantaSfxKey);
        }
    }

    public void EliminarJugador()
    {
        Debug.Log($"[PlayerTag] Eliminando player {playerIndex1Based}");
        Destroy(gameObject);
    }
}
