using UnityEngine;
using UnityEngine.EventSystems;

public class Jack : MonoBehaviour, IPointerDownHandler
{

    [Header("Configuración")]

    // Reemplazo: ahora hay varias opciones por tipo
    [SerializeField] private Sprite[] jackSpritesNormal;
    [SerializeField] private Sprite[] jackSpritesEspecial;
    [SerializeField] private Sprite[] jackSpritesBomba;

    [Header("Sprites por color (Normal)")]
    [SerializeField] private Sprite[] jackSpritesNormalAmarillo;
    [SerializeField] private Sprite[] jackSpritesNormalRojo;
    [SerializeField] private Sprite[] jackSpritesNormalVerde;
    [SerializeField] private Sprite[] jackSpritesNormalAzul;

    [Header("Transparencia (estado pendiente)")]
    [SerializeField, Range(0f, 1f)] private float transparentAlpha = 0.4f;
    [SerializeField] private Color transparentGray = new Color(0.65f, 0.65f, 0.65f, 1f);

    [SerializeField] private int puntos;
    [SerializeField] public enum tipo { Normal, Especial, bomba };
    [SerializeField] public tipo tipoJack = tipo.Normal; // tipo de este Jack
    private Progression progression;

    [Header("Animación (opcional)")]
    [Tooltip("Animator en un hijo para reaccionar a eventos de la bolita (lanzada / reiniciada)")]
    [SerializeField] private Animator _animator;
    [Tooltip("Nombre del parámetro bool que refleja si la bola está en el aire")] 
    [SerializeField] private string bolaEnElAireBool = "EnElAire";

    [Header("Visual Root (si el script está en el padre)")]
    [Tooltip("Hijo que contiene el SpriteRenderer/Animator. Si se deja vacío se detecta el primer SpriteRenderer en hijos.")]
    [SerializeField] private Transform visualRoot;

    public int Puntos => puntos; // Exponer puntos para Progression

    private SpriteRenderer _sr;
    private Collider2D _col2D; // collider del padre

    private void Awake()
    {
        // Ahora el script vive en el padre (con collider) y el SpriteRenderer está en un hijo
        _col2D = GetComponent<Collider2D>();
        if (!visualRoot)
        {
            var srTemp = GetComponentInChildren<SpriteRenderer>(true);
            if (srTemp) visualRoot = srTemp.transform;
        }
        _sr = visualRoot ? visualRoot.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (!_animator && visualRoot)
        {
            _animator = visualRoot.GetComponentInChildren<Animator>(true);
        }
        // Elegir sprite inicial según tipo
        Sprite selected = GetRandomSpriteForType();
        if (_sr && selected) _sr.sprite = selected;
        if (progression == null) progression = FindAnyObjectByType<Progression>();
    }

    private void OnEnable()
    {
        Bolita.BolaLanzada += OnBolaLanzada;
        Bolita.BolaReiniciada += OnBolaReiniciada;
    }

    private void OnDisable()
    {
        Bolita.BolaLanzada -= OnBolaLanzada;
        Bolita.BolaReiniciada -= OnBolaReiniciada;
    }

    private Sprite GetRandomSpriteForType()
    {
        switch (tipoJack)
        {
            case tipo.Normal: return PickRandom(jackSpritesNormal);
            case tipo.Especial: return PickRandom(jackSpritesEspecial);
            case tipo.bomba: return PickRandom(jackSpritesBomba);
            default: return null;
        }
    }

    private Sprite PickRandom(Sprite[] opciones)
    {
        if (opciones == null || opciones.Length == 0) return null;
        int idx = Random.Range(0, opciones.Length);
        return opciones[idx];
    }

    private void Start()
    {
        // No forzar el collider aquí: Transparentar/HabilitarColor controlan su estado
        if (TurnManager.instance != null)
        {
            updateColor(TurnManager.instance.CurrentTurn()); // Actualizar sprite por color al inicio
        }
    }

    // Prioridad máxima: capturar clic/tap vía EventSystem y también con raycast manual como respaldo
    public void OnPointerDown(PointerEventData eventData)
    {
        Recolectar();
        eventData.Use(); // consume el evento para evitar que UI/otros lo capturen
    }

    private void Update()
    {
        // Respaldo para clic/tap aunque haya UI encima
        if (Input.GetMouseButtonDown(0))
        {
            TryCollectAt(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            TryCollectAt(Input.touches[0].position);
        }
    }

    private void TryCollectAt(Vector2 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector2 p = new Vector2(wp.x, wp.y);
        // Raycast puntual: asociar a este Jack via GetComponentInParent
        var hits = Physics2D.OverlapPointAll(p);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            var jack = h.GetComponentInParent<Jack>();
            if (jack == this)
            {
                Recolectar();
                return;
            }
        }
    }

    private void OnMouseDown()
    {
        Recolectar();
    }

    public void Recolectar()
    {
        if (progression != null)
        {
            progression.NotificarJackTocado(this);
        }
        // Nuevo sistema de audio: usar claves dinámicas registradas via SceneAudioLibrary
        var sm = SoundManager.instance;
        if (sm != null)
        {
            switch (tipoJack)
            {
                case tipo.bomba:
                    sm.PlaySfx("catapis:bombatocado", 2f);
                    break;
                case tipo.Especial:
                    sm.PlaySfx("catapis:especialtocado");
                    break;
                case tipo.Normal:
                    sm.PlaySfx("catapis:normaltocado");
                    break;
            }
        }
        else
        {
            Debug.LogWarning("[Jack] SoundManager.instance no encontrado para reproducir SFX.");
        }
        disable();
    }

    public void disable()
    {
        Destroy(gameObject);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        // Solo colliders del objeto donde está el script (padre)
        var cols = GetComponents<Collider2D>();
        foreach (var c in cols)
        {
            if (c) c.enabled = enabled;
        }
    }

    // Decolorar y deshabilitar collider (estado: pendiente de lanzar)
    public void Transparentar()
    {
        var root = visualRoot ? visualRoot : transform;
        var sprs = root.GetComponentsInChildren<SpriteRenderer>(true);
        Color c = new Color(transparentGray.r, transparentGray.g, transparentGray.b, transparentAlpha);
        foreach (var sr in sprs)
        {
            if (sr) sr.color = c;
        }
        SetCollidersEnabled(false);
    }

    // Restaurar visibilidad y habilitar collider (estado: listo/lanzado)
    public void HabilitarColor()
    {
        SetCollidersEnabled(true);
        var root = visualRoot ? visualRoot : transform;
        var sprs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprs)
        {
            if (sr) sr.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public void updateColor(int numJugador)
    {
        // Para tipo Normal: elegir el set de sprites por color del jugador
        if (_sr == null) return;
        if (tipoJack != tipo.Normal)
        {
            return; // Especial y bomba no cambian por color
        }

        Sprite[] setPorColor;
        switch (numJugador)
        {
            case 1: setPorColor = jackSpritesNormalRojo; break;
            case 2: setPorColor = jackSpritesNormalAzul; break;
            case 3: setPorColor = jackSpritesNormalAmarillo; break;
            case 4: setPorColor = jackSpritesNormalVerde; break;
            default: setPorColor = jackSpritesNormal; break; // fallback
        }

        if (setPorColor != null && setPorColor.Length > 0)
        {
            // Actualizar el vector base y asignar un sprite al azar de ese color
            jackSpritesNormal = setPorColor;
            var elegido = PickRandom(jackSpritesNormal);
            if (elegido != null)
            {
                _sr.sprite = elegido;

            }
        }
        
    }

    private bool AnimatorHasBool(Animator anim, string param)
    {
        if (anim == null || string.IsNullOrEmpty(param)) return false;
        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == param) return true;
        }
        return false;
    }

    private void OnBolaLanzada()
    {
        if (_animator != null && AnimatorHasBool(_animator, bolaEnElAireBool))
        {
            _animator.SetBool(bolaEnElAireBool, true);
        }
    }

    private void OnBolaReiniciada()
    {
        if (_animator != null && AnimatorHasBool(_animator, bolaEnElAireBool))
        {
            _animator.SetBool(bolaEnElAireBool, false);
        }
    }
}

