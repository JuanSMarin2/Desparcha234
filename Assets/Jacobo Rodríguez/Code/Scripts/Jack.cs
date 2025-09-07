using UnityEngine;

public class Jack : MonoBehaviour
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
        // Ya no usamos enable(); los Jacks inician activos por defecto
        if (_col2D != null) _col2D.enabled = true; // Asegurar que el collider esté activo
        if (TurnManager.instance != null)
        {
            updateColor(TurnManager.instance.CurrentTurn()); // Actualizar sprite por color al inicio
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
        // Para tipo Normal: elegir el set de sprites por color del jugador
        if (_sr == null) return;
        if (tipoJack != tipo.Normal)
        {
            return; // Especial y bomba no cambian por color
        }

        Sprite[] setPorColor = null;
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
