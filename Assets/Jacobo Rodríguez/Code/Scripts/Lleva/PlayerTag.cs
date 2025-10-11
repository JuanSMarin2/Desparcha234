using UnityEngine;

[RequireComponent(typeof(Movimiento))]
public class PlayerTag : MonoBehaviour
{
    [Header("Identidad")] [SerializeField] private int playerIndex1Based = 1; // 1..4

    [Header("Velocidades")] 
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float taggedSpeed = 5f;

    [Header("Feedback Visual")]
    [Tooltip("Objeto de outline / resalte que se activa cuando este jugador está taggeado")] 
    [SerializeField] private GameObject outlineObject;

    [Header("Transferencia Tag")] 
    [SerializeField] private float transferCooldown = 0.3f;
    [Tooltip("Usar colisiones físicas (OnCollisionEnter2D) en lugar de triggers para transferir")] [SerializeField] private bool usarColisionFisica = false;
    [Tooltip("Si true mantiene también el trigger para otras detecciones (dual)")] [SerializeField] private bool mantenerTriggerParaDeteccion = false;

    private float _lastTransferTime = -999f;
    private int _lastTransferFrame = -999;

    private bool _isTagged;
    private Movimiento _mov;

    public bool IsTagged => _isTagged;
    public int PlayerIndex => playerIndex1Based;

    private void Awake()
    {
        _mov = GetComponent<Movimiento>();
        ApplySpeed();
        UpdateVisuals();
    }

    public void SetTagged(bool value, bool playFeedback = true)
    {
        if (_isTagged == value) return;
        _isTagged = value;
        ApplySpeed();
        UpdateVisuals();
        if (playFeedback)
        {
            Debug.Log($"[PlayerTag] Player {playerIndex1Based} tagged={(value?"YES":"NO")}");
        }
    }

    private void ApplySpeed()
    {
        if (_mov != null)
        {
            _mov.SetMoveSpeed(_isTagged ? taggedSpeed : normalSpeed);
        }
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
        if (Time.time - _lastTransferTime < transferCooldown) return;
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
        var otherTag = other.GetComponentInParent<PlayerTag>();
        HandleTagTransfer(otherTag);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!usarColisionFisica) return;
        var otherTag = collision.collider.GetComponentInParent<PlayerTag>();
        HandleTagTransfer(otherTag);
    }

    public void EliminarJugador()
    {
        Debug.Log($"[PlayerTag] Eliminando player {playerIndex1Based}");
        Destroy(gameObject);
    }
}
