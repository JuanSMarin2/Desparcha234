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
    private float _lastTransferTime = -999f;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isTagged) return; // sólo el que tiene tag puede transferirlo
        if (Time.time - _lastTransferTime < transferCooldown) return;
        var otherTag = other.GetComponentInParent<PlayerTag>();
        if (otherTag == null || otherTag == this) return;
        if (otherTag._isTagged) return; // ya lo tiene
        // Transferir
        _lastTransferTime = Time.time;
        otherTag._lastTransferTime = Time.time; // protege del rebote inmediato
        SetTagged(false);
        otherTag.SetTagged(true);
        TagManager.Instance?.NotifyTagChanged(this, otherTag);
    }

    public void EliminarJugador()
    {
        Debug.Log($"[PlayerTag] Eliminando player {playerIndex1Based}");
        Destroy(gameObject);
    }
}
