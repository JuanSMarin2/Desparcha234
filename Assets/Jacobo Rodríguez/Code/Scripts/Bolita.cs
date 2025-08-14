using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class Bolita : MonoBehaviour
{
    public enum EstadoLanzamiento
    {
        PendienteDeLanzar, // "pendiente de lanzar"
        EnElAire,          // "en el aire"
        TocadaPorJugador,  // "tocada por el jugador"
        Fallado             // "fallado"
    }

    [Header("Física")]
    [SerializeField] private string tagSuelo = "Suelo"; // Asegúrate de poner este tag al objeto de suelo
    [SerializeField] private Transform puntoReinicio;    // Opcional: posición para reiniciar

    private Rigidbody2D _rb;
    private EstadoLanzamiento _estado = EstadoLanzamiento.PendienteDeLanzar;

    // Notifica cuando cambia el estado (útil para UI Manager)
    public event Action<EstadoLanzamiento> OnEstadoCambio;

    
    public EstadoLanzamiento Estado => _estado;
    [SerializeField] private SpriteRenderer _sprite;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _rb.gravityScale = 0f; // Desactivar gravedad al inicio
        _rb.linearVelocity = Vector2.zero;
        NotificarEstado(_estado);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Nuevo: lanzar hacia ARRIBA.
    public void DarVelocidadHaciaArriba(float fuerza)
    {
        if (_rb == null) return;

        CambiarEstado(EstadoLanzamiento.EnElAire);
        _rb.gravityScale = 1f; // Activar gravedad al lanzar

        // Interpretamos "fuerza" como velocidad objetivo en m/s hacia arriba
        Vector2 v = _rb.linearVelocity;
        v.y = Mathf.Abs(fuerza);
        _rb.linearVelocity = v;
    }

    // Obsoleto: mantener por compatibilidad, pero ahora lanza hacia arriba internamente.
    [Obsolete("Usa DarVelocidadHaciaArriba(float fuerza)")]
    public void DarVelocidadHaciaAbajo(float fuerza)
    {
        DarVelocidadHaciaArriba(fuerza);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_estado != EstadoLanzamiento.EnElAire) return;
        if (!collision.collider.CompareTag(tagSuelo)) return;

        // Toca suelo -> pierde turno
        CambiarEstado(EstadoLanzamiento.Fallado);

        Progression progression = FindAnyObjectByType<Progression>();
        if (progression != null)
        {
            progression.PerderPorTocarSuelo();
        }
    }

    private void OnMouseDown()
    {
        // Click del jugador: solo válido si ya fue lanzada
        if (_estado == EstadoLanzamiento.PendienteDeLanzar) return;
        if (_estado != EstadoLanzamiento.EnElAire) return;

        CambiarEstado(EstadoLanzamiento.TocadaPorJugador);

        Progression progression = FindAnyObjectByType<Progression>();
        if (progression != null)
        {
            progression.NotificarBolitaTocada();
        }

        if (_sprite != null)
        {
            _sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b, 0.5f);
        }
    }

    public void ReiniciarTurno()
    {
        
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.gravityScale = 0f;
        if (puntoReinicio != null)
        {
            transform.position = puntoReinicio.position;
            transform.rotation = puntoReinicio.rotation;
        }
        if (_sprite != null)
        {
            _sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b, 1f); // Restaurar opacidad completa
        }
        CambiarEstado(EstadoLanzamiento.PendienteDeLanzar);
    }

    private void CambiarEstado(EstadoLanzamiento nuevo)
    {
        if (_estado == nuevo) return;
        _estado = nuevo;
        NotificarEstado(_estado);
    }

    private void NotificarEstado(EstadoLanzamiento e)
    {
        OnEstadoCambio?.Invoke(e);
    }
}
