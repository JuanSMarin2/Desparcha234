using UnityEngine;
using System;

[RequireComponent(typeof(Movimiento))]
[DisallowMultipleComponent]
public class PlayerCongelados : MonoBehaviour
{
    [Header("Identidad")] 
    [SerializeField] private int playerIndex1Based = 1; // 1..4

    [Header("Detección de contacto")]
    [Tooltip("Usar colisiones físicas (OnCollisionEnter2D) en lugar de triggers para interactuar")] 
    [SerializeField] private bool usarColisionFisica = true;
    [Tooltip("Si true mantiene también el trigger para otras detecciones (dual)")] 
    [SerializeField] private bool mantenerTriggerParaDeteccion = false;

    [Header("Sprites (congelado)")]
    [SerializeField] private SpriteRenderer spriteRenderer; // si no se asigna se busca en hijos
    [SerializeField] private Sprite spriteCongelado;
    [Tooltip("Sprite normal opcional; si está vacío se toma el actual en Awake")] 
    [SerializeField] private Sprite spriteNormal;

    [Header("Animator (opcional)")]
    [Tooltip("Animator que podría estar cambiando el sprite; se deshabilita al congelar para que no sobreescriba el sprite")] 
    [SerializeField] private Animator animatorOpcional;

    [Header("Efectos de Sonido")] 
    [Tooltip("Sonido que reproduce al tocar objetos con tag 'planta'")]
    [SerializeField] private string plantaSfxKey = "lleva:planta";
    [SerializeField] private string freezeSfxKey = "congelados:freeze";
    [SerializeField] private string unfreezeSfxKey = "congelados:unfreeze";

    [Header("Feedback Visual")]
    [Tooltip("Objeto de outline/resalte para indicar que este jugador es el Freezer actual")]
    [SerializeField] private GameObject outlineFreezerObject;

    [Header("Debug")] 
    [SerializeField] private bool debugLogs = false;

    private Movimiento _mov;
    private Rigidbody2D _rb;
    private TagCongelados _mgr;

    private bool _isFrozen;
    private bool _isFreezer; // el que congela (no cambia en la ronda)

    // throttling de múltiples contactos en un mismo frame
    private int _lastContactFrame = -999;
    private float _lastHandleTime = -999f;

    // Fallbacks por si TagManager no existe (editor/test)
    private const float _movDefaultNormal = 3f;
    private const float _movDefaultTagged = 5f;

    private RigidbodyType2D _originalBodyType = RigidbodyType2D.Dynamic;
    private bool _cachedOriginalBodyType = false;

    public bool IsFrozen => _isFrozen;
    public bool IsFreezer => _isFreezer;
    public int PlayerIndex => playerIndex1Based;

    public static event Action<PlayerCongelados> OnFrozenStateChanged; // notifica cambios al manager/UI

    private void Awake()
    {
        _mov = GetComponent<Movimiento>();
        _rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (!animatorOpcional) animatorOpcional = GetComponentInChildren<Animator>(true);
        if (spriteRenderer && spriteNormal == null) spriteNormal = spriteRenderer.sprite;
        _mgr = FindFirstObjectByType<TagCongelados>();
        // Si el movimiento tiene empuje activado, forzar colisión física
        if (_mov != null && _mov.IsPlayerPushEnabled) usarColisionFisica = true;
        if (_rb && !_cachedOriginalBodyType) { _originalBodyType = _rb.bodyType; _cachedOriginalBodyType = true; }
        if (debugLogs) Debug.Log($"[PlayerCongelados] Awake P{playerIndex1Based} usarColFisica={usarColisionFisica} empuje={_mov?.IsPlayerPushEnabled}");
        ApplyRoleAndStateVisuals();
    }

    private void OnEnable()
    {
        // El modo Congelados dispara TagManager.OnRoundStarted también
        TagManager.OnRoundStarted += OnRoundStarted_Reset;
        TagCongelados.OnRoundStarted += OnRoundStarted_Reset;
        if (_mgr == null) _mgr = FindFirstObjectByType<TagCongelados>();
        if (debugLogs) Debug.Log($"[PlayerCongelados] OnEnable P{playerIndex1Based} Freezer={_isFreezer} Frozen={_isFrozen}");
    }

    private void OnDisable()
    {
        TagManager.OnRoundStarted -= OnRoundStarted_Reset;
        TagCongelados.OnRoundStarted -= OnRoundStarted_Reset;
    }

    private void OnRoundStarted_Reset()
    {
        // Asegurar estado consistente al inicio de ronda
        if (_isFrozen) { _isFrozen = false; ApplyMovementParams(); ApplyRoleAndStateVisuals(); }
    }

    private void ResetPhysicsVelocities()
    {
        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    public void SetFreezer(bool value)
    {
        _isFreezer = value;
        if (_isFreezer)
        {
            // El que congela nunca está congelado
            if (_isFrozen)
            {
                _isFrozen = false;
            }
        }
        ApplyMovementParams();
        ApplyRoleAndStateVisuals();
    }

    public void ForceUnfreeze()
    {
        if (!_isFrozen) return;
        _isFrozen = false;
        RestorePushable();
        ApplyMovementParams();
        ApplyRoleAndStateVisuals();
        OnFrozenStateChanged?.Invoke(this);
        PlaySfx(unfreezeSfxKey);
    }

    public void Freeze()
    {
        if (_isFreezer || _isFrozen) return; // el freezer no se congela, ni doble freeze
        _isFrozen = true;
        MakeNonPushable();
        ApplyMovementParams();
        ApplyRoleAndStateVisuals();
        ResetPhysicsVelocities();
        OnFrozenStateChanged?.Invoke(this);
        PlaySfx(freezeSfxKey);
    }

    public void Unfreeze()
    {
        if (!_isFrozen) return;
        _isFrozen = false;
        RestorePushable();
        ApplyMovementParams();
        ApplyRoleAndStateVisuals();
        OnFrozenStateChanged?.Invoke(this);
        PlaySfx(unfreezeSfxKey);
    }

    private void ApplyMovementParams()
    {
        if (_mov == null) return;
        if (_isFrozen)
        {
            _mov.enabled = false;
            ResetPhysicsVelocities();
            return;
        }
        if (!_mov.enabled) _mov.enabled = true;
        // Tomar velocidades del modo Congelados si el manager existe; fallback a TagManager si no.
        float speed;
        if (TagCongelados.Instance != null)
        {
            speed = _isFreezer ? TagCongelados.Instance.MoveSpeedFreezer : TagCongelados.Instance.MoveSpeedNormal;
        }
        else
        {
            var tm = TagManager.Instance;
            speed = _isFreezer ? (tm ? tm.MoveSpeedTagged : _movDefaultTagged) : (tm ? tm.MoveSpeedNormal : _movDefaultNormal);
        }
        _mov.SetMoveSpeed(speed);
    }

    private void ApplyRoleAndStateVisuals()
    {
        // Sprite
        if (spriteRenderer)
        {
            if (_isFrozen && spriteCongelado)
                spriteRenderer.sprite = spriteCongelado;
            else if (_isFrozen && !spriteCongelado && debugLogs)
                Debug.LogWarning($"[PlayerCongelados] P{playerIndex1Based} congelado pero 'spriteCongelado' no asignado");
            else if (spriteNormal)
                spriteRenderer.sprite = spriteNormal;
        }
        // Deshabilitar Animator al estar congelado para evitar que una animación reemplace el sprite
        if (animatorOpcional)
        {
            // Alinear el parámetro del Animator con el rol actual (freezer tiene HasTag=true)
            animatorOpcional.SetBool("HasTag", _isFreezer);
            animatorOpcional.enabled = !_isFrozen;
        }
        // Outline del Freezer actual
        if (outlineFreezerObject)
        {
            outlineFreezerObject.SetActive(_isFreezer);
        }
    }

    private void HandleContact(PlayerCongelados other)
    {
        if (other == null || other == this) return;
        if (Time.frameCount == _lastContactFrame) return; // evitar doble ejecución por trigger+collision en el mismo frame
        if (Time.time - _lastHandleTime < 0.02f) return; // throttling por tiempo para Stay
        _lastContactFrame = Time.frameCount;
        _lastHandleTime = Time.time;

        // Freezer congela a los no congelados
        if (this._isFreezer && !other._isFrozen)
        {
            if (debugLogs) Debug.Log($"[PlayerCongelados] Congelando a P{other.PlayerIndex}");
            other.Freeze(); // 1) no empujable + 2) cambia sprite
            // 3) el freezer rota 180° para seguir su camino
            transform.Rotate(0f, 0f, 180f);
            _mgr?.CheckWinByAllFrozen();
            return;
        }

        // No-freezer descongela al congelado
        if (!this._isFreezer && !this._isFrozen && other._isFrozen)
        {
            if (debugLogs) Debug.Log($"[PlayerCongelados] Descongelando a P{other.PlayerIndex}");
            other.Unfreeze();
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Las plantas deben ser Trigger: reproducir siempre por trigger
        if (other.CompareTag("planta"))
        {
            PlayPlantaSound();
        }

        if (usarColisionFisica && !mantenerTriggerParaDeteccion) return;

        var otherP = other.GetComponentInParent<PlayerCongelados>();
        if (debugLogs && otherP) Debug.Log($"[PlayerCongelados] TriggerEnter contacto con P{otherP.PlayerIndex}. Yo freezer={_isFreezer}, el otro frozen={otherP.IsFrozen}");
        HandleContact(otherP);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (usarColisionFisica && !mantenerTriggerParaDeteccion) return;
        var otherP = other.GetComponentInParent<PlayerCongelados>();
        HandleContact(otherP);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!usarColisionFisica) return;

        if (collision.collider != null && collision.collider.CompareTag("planta"))
        {
            PlayPlantaSound();
        }

        var otherP = collision.collider ? collision.collider.GetComponentInParent<PlayerCongelados>() : null;
        if (debugLogs && otherP) Debug.Log($"[PlayerCongelados] CollisionEnter con P{otherP.PlayerIndex}. Yo freezer={_isFreezer}, el otro frozen={otherP.IsFrozen}");
        HandleContact(otherP);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!usarColisionFisica) return;
        var otherP = collision.collider ? collision.collider.GetComponentInParent<PlayerCongelados>() : null;
        HandleContact(otherP);
    }

    private void PlayPlantaSound()
    {
        PlaySfx(plantaSfxKey);
    }

    private void PlaySfx(string key, float vol = 1f)
    {
        if (string.IsNullOrEmpty(key)) return;
        var sm = SoundManager.instance;
        if (sm != null)
        {
            sm.PlaySfxRateLimited(key, 0.1f, vol);
        }
    }

    private void MakeNonPushable()
    {
        if (_rb)
        {
            if (!_cachedOriginalBodyType) { _originalBodyType = _rb.bodyType; _cachedOriginalBodyType = true; }
            _rb.bodyType = RigidbodyType2D.Kinematic; // no empujable (no recibe impulsos)
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private void RestorePushable()
    {
        if (_rb)
        {
            // Restaurar el tipo de cuerpo según si el empuje entre jugadores está activo en Movimiento
            var wantDynamic = _mov != null && _mov.IsPlayerPushEnabled;
            _rb.bodyType = wantDynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            // Asegurar detener cualquier velocidad residual
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }
}
