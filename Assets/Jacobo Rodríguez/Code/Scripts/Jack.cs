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




    public int Puntos => puntos; // Exponer puntos para Progression

    private SpriteRenderer _sr;
    private Collider2D _col2D;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col2D = GetComponent<Collider2D>();
        // Elegir un sprite aleatorio según el tipo (para Normal se ajustará de nuevo en Start vía updateColor)
        Sprite selected = GetRandomSpriteForType();
        if (_sr != null && selected != null) _sr.sprite = selected;
        if (progression == null) progression = FindAnyObjectByType<Progression>();
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
        var hits = Physics2D.OverlapPointAll(p);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            // Si el hit es este jack o un hijo suyo, recolectar
            if (h.transform == transform || h.transform.IsChildOf(transform))
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
                    sm.PlaySfx("catapis:bombatocado");
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

    // Decolorar y deshabilitar collider (estado: pendiente de lanzar)
    public void Transparentar()
    {
        var sprs = GetComponentsInChildren<SpriteRenderer>(true);
        Color c = new Color(transparentGray.r, transparentGray.g, transparentGray.b, transparentAlpha);
        foreach (var sr in sprs)
        {
            if (sr != null) sr.color = c;
        }
        // Deshabilitar todos los Collider2D (no solo el círculo)
        CircleCollider2D jackCollider = GetComponent<CircleCollider2D>();
        if (jackCollider != null)
        {
            jackCollider.enabled = false;
           
        }
        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in cols)
        {
            if (col != null) col.enabled = false;
        }
    }

    // Restaurar visibilidad y habilitar collider (estado: listo/lanzado)
    public void HabilitarColor()
    {
        // Habilitar todos los Collider2D (incluido el círculo si existe)
        CircleCollider2D jackCollider = GetComponent<CircleCollider2D>();
        if (jackCollider != null)
        {
            jackCollider.enabled = true;
          
        }
        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in cols)
        {
            if (col != null) col.enabled = true;
        }

        // Restaurar color visible (para cualquier tipo)
        var sprs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprs)
        {
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f);
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
}

