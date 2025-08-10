using UnityEngine;

public class Jack : MonoBehaviour
{

    [Header("Configuración")]

    [SerializeField] Sprite jackSprite; // Sprite del Jack
    [SerializeField] private int puntos;
    [SerializeField] enum tipo { Normal, Especial, bomba };
    [SerializeField] private tipo tipoJack = tipo.Normal; // tipo de este Jack
    [SerializeField] private Progression progression;     // Referencia a Progression

    public int Puntos => puntos; // Exponer puntos para Progression

    private SpriteRenderer _sr;
    private Collider2D _col2D;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col2D = GetComponent<Collider2D>();
        if (_sr != null && jackSprite != null) _sr.sprite = jackSprite;
        if (progression == null) progression = FindAnyObjectByType<Progression>();
    }

    private void Start()
    {
        if (_sr != null) enable(); // Habilitar al inicio
        if (_col2D != null) _col2D.enabled = true; // Asegurar que el collider esté activo
        updateColor(TurnManager.instance.CurrentTurn()); // Actualizar color al inicio
    }
    private void OnMouseDown()
    {
        // Llamado cuando se hace click sobre el collider del jack
        Recolectar();
    }

    public void Recolectar()
    {
        if (progression != null)
        {
            // Llama al método en Progression cuando exista
            // progression.NotificarJackTocado(this);
        }
        disable();
    }

    public void enable()
    {
        if (_sr != null)
        {
            var c = _sr.color;
            c.a = 1f; // alpha 100%
            _sr.color = c;
        }
        if (_col2D != null) _col2D.enabled = true; // activar collider
    }

    public void disable()
    {
        if (_sr != null)
        {
            var c = _sr.color;
            c.a = 0.6f; // alpha 60%
            _sr.color = c;
        }
        if (_col2D != null) _col2D.enabled = false; // desactivar collider
    }

    public void updateColor(int numJugador)
    {
        // Solo los jacks de tipo Normal cambian de color por jugador
        if (tipoJack != tipo.Normal || _sr == null) return;

        Color color = Color.white;
        switch (numJugador)
        {
            case 1: color = Color.red; break;
            case 2: color = Color.blue; break;
            case 3: color = Color.yellow; break;
            case 4: color = Color.green; break;
            default: color = Color.white; break;
        }

        // Mantener el alpha actual (para respetar enable/disable)
        color.a = _sr.color.a;
        _sr.color = color;
    }
}
