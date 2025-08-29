using UnityEngine;

public class Jack : MonoBehaviour
{

    [Header("Configuración")]

    [SerializeField] Sprite jackSprite; // Sprite del Jack
    [SerializeField] private int puntos;
    [SerializeField] public enum tipo { Normal, Especial, bomba };
    [SerializeField] public tipo tipoJack = tipo.Normal; // tipo de este Jack
    [SerializeField] private Progression progression;   
    
    [SerializeField] private float alphaTransparente = 0f; // Alpha para el estado deshabilitado (ya no se usa para volver transparente)
      // Referencia a Progression

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
        // Ya no usamos enable(); los Jacks inician activos por defecto
        if (_col2D != null) _col2D.enabled = true; // Asegurar que el collider esté activo
        if (TurnManager.instance != null)
        {
            updateColor(TurnManager.instance.CurrentTurn()); // Actualizar color al inicio
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
            // Llama al método en Progression cuando exista
            progression.NotificarJackTocado(this);
        }
        // SFX de jack tocado (sin dependencia de tipo)
        var sm = GameObject.Find("SoundManager");
        if (sm != null)
        {
            if (tipoJack == tipo.bomba)
            {
                sm.SendMessage("SonidoBombaTocada", SendMessageOptions.DontRequireReceiver);
            }
            else
            sm.SendMessage("SonidoJackTocado", SendMessageOptions.DontRequireReceiver);
        }
        disable();
    }


    public void disable()
    {
        Destroy(gameObject);
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

        // Mantener el alpha actual (si existiera) aunque ya no se usa para deshabilitar
        color.a = _sr.color.a;
        _sr.color = color;
    }
}
