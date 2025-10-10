// IconManagerCatapis.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class IconManagerCatapis : MonoBehaviour
{
    [Serializable]
    public class PlayerSkinSet
    {
        // Sprites por jugador y por skin
        public Sprite[] happyBySkin;    // Sprites felices por skin
        public Sprite[] sadBySkin;      // Sprites tristes por skin
    }

    [Header("Image global del icono")]
    [SerializeField] private Image icon; // un solo Image para mostrar el jugador actual

    [Header("Proveedor de defaults (PanelFinalTurno)")]
    [Tooltip("Se usa como fuente de sprites por defecto cuando NO hay skin equipada")]
    [SerializeField] private PanelFinalTurno defaultProvider;

    [Header("Sprites por jugador (0..3) - solo si hay skin equipada")]
    [Tooltip("Cada entrada corresponde a un jugador; por defecto 1 si todos comparten sprites")]
    [SerializeField] private PlayerSkinSet[] players = new PlayerSkinSet[1];

    [Header("Copa Images (Jugador 1..4)")]
    [Tooltip("Si no se asignan, se intentará encontrar objetos por nombre: Copa1..Copa4 (o en minúsculas)")]
    [SerializeField] private Image[] cupImages = new Image[4];

    void Awake()
    {
        // Encontrar el Panel incluso si está inactivo
        if (defaultProvider == null)
        {
#if UNITY_2023_1_OR_NEWER
            defaultProvider = FindFirstObjectByType<PanelFinalTurno>(FindObjectsInactive.Include);
#else
            // Compatibilidad: si el proyecto no soporta FindFirstObjectByType con Include, usar variante sin incluir inactivos
            defaultProvider = FindFirstObjectByType<PanelFinalTurno>();
#endif
        }
        // Si no se asignó un Image, intentar usar el del Panel (si está accesible en la escena)
        if (icon == null && defaultProvider != null)
        {
            // El icono del panel es el que se ve; si queremos sobreescribirlo con skins, reutilizamos ese Image
            var panelIcon = GetPanelIconImage();
            if (panelIcon != null) icon = panelIcon;
        }
    }

    void Start()
    {
        // Asignar sprites de skin a Copa1..Copa4 al iniciar
        for (int i = 0; i < 4; i++)
        {
            var img = EnsureCupImageRef(i);
            if (img != null)
            {
                // Usar el sprite "feliz" (default) de la skin del jugador i; si no hay skin, cae al default del panel
                ApplyToImage(img, i, sad: false);
            }
        }
    }

    void OnEnable()
    {
        PanelFinalTurno.OnPanelShown += OnPanelShown;
        // Inicial: mostrar jugador 1 (0-based) en feliz por defecto
        if (icon != null) RefreshForPlayer(0, forceSad: false);
    }

    void OnDisable()
    {
        PanelFinalTurno.OnPanelShown -= OnPanelShown;
    }

    private void OnPanelShown(int playerIndex1Based, bool success)
    {
        // success=true => feliz; success=false => triste
        int idx0 = Mathf.Clamp(playerIndex1Based - 1, 0, 3);
        RefreshForPlayer(idx0, forceSad: !success);
    }

    private void RefreshForPlayer(int playerIndexZeroBased, bool forceSad)
    {
        if (icon == null) return;

        // ¿Hay skin equipada? Si no, usar defaults del PanelFinalTurno
        int equipped = -1;
        if (GameData.instance != null)
        {
            equipped = GameData.instance.GetEquipped(playerIndexZeroBased);
        }

        Sprite chosen = null;
        if (equipped >= 0)
        {
            // Intentar sprite de skin
            chosen = GetSpriteForPlayer(playerIndexZeroBased, forceSad, equipped);
            // Fallback: intentar estado opuesto dentro de la misma skin
            if (chosen == null)
                chosen = GetSpriteForPlayer(playerIndexZeroBased, !forceSad, equipped);
        }

        // Si no hay skin válida o faltan sprites, usar defaults del PanelFinalTurno
        if (chosen == null)
        {
            chosen = GetDefaultSpriteFromPanel(playerIndexZeroBased, forceSad);
            if (chosen == null)
                chosen = GetDefaultSpriteFromPanel(playerIndexZeroBased, !forceSad); // último recurso: opuesto
        }

        if (chosen != null)
        {
            icon.sprite = chosen;
            icon.enabled = true;
        }
        else
        {
            icon.enabled = false;
        }
    }

    private Image GetPanelIconImage()
    {
        // Intentar acceder al Image del panel si existe (requiere que el campo sea accesible o se asigne desde el inspector)
        // Aquí se asume que el Image es hijo del PanelFinalTurno y está referenciado en el componente PanelFinalTurno.
        // Como el campo en PanelFinalTurno es privado, este método depende de que el usuario asigne el Image en el inspector
        // o que el mismo Image sea referenciado en este script.
        // Intento razonable: buscar un Image en los hijos del panel.
        if (defaultProvider == null) return null;
        var img = defaultProvider.GetComponentInChildren<Image>(includeInactive: true);
        return img;
    }

    private Sprite GetDefaultSpriteFromPanel(int playerIndexZeroBased, bool sad)
    {
        if (defaultProvider == null) return null;
        // Los arrays en PanelFinalTurno son públicos y el orden es Jugador 1..4
        var happy = defaultProvider.iconosFelices;
        var sads = defaultProvider.iconosTristes;
        Sprite pick = null;
        if (sad)
        {
            if (sads != null && playerIndexZeroBased >= 0 && playerIndexZeroBased < sads.Length)
                pick = sads[playerIndexZeroBased];
            if (pick == null && happy != null && playerIndexZeroBased >= 0 && playerIndexZeroBased < happy.Length)
                pick = happy[playerIndexZeroBased];
        }
        else
        {
            if (happy != null && playerIndexZeroBased >= 0 && playerIndexZeroBased < happy.Length)
                pick = happy[playerIndexZeroBased];
        }
        return pick;
    }

    public Sprite GetSpriteForPlayer(int playerIndexZeroBased, bool sad)
    {
        int equipped = -1;
        if (GameData.instance != null)
        {
            equipped = GameData.instance.GetEquipped(playerIndexZeroBased);
        }
        return GetSpriteForPlayer(playerIndexZeroBased, sad, equipped);
    }

    public Sprite GetSpriteForPlayer(int playerIndexZeroBased, bool sad, int equipped)
    {
        // Si no hay skin equipada, no proveemos sprite de skin
        if (equipped < 0) return null;
        if (players == null || players.Length == 0) return null;
        // Si solo hay 1 entrada, se reutiliza para todos los jugadores
        int idxPlayer = Mathf.Clamp(playerIndexZeroBased, 0, players.Length - 1);
        var set = players[idxPlayer] != null ? players[idxPlayer] : (players[0] != null ? players[0] : null);
        if (set == null) return null;

        Sprite pick = null;
        if (sad)
        {
            if (set.sadBySkin != null && equipped >= 0 && equipped < set.sadBySkin.Length)
                pick = set.sadBySkin[equipped];
            if (pick == null && set.happyBySkin != null && equipped >= 0 && equipped < set.happyBySkin.Length)
                pick = set.happyBySkin[equipped];
        }
        else
        {
            if (set.happyBySkin != null && equipped >= 0 && equipped < set.happyBySkin.Length)
                pick = set.happyBySkin[equipped];
        }
        return pick;
    }

    // Utilidad: aplicar sprite a un Image específico externo
    public bool ApplyToImage(Image target, int playerIndexZeroBased, bool sad)
    {
        if (target == null) return false;
        // Intentar skin primero
        var s = GetSpriteForPlayer(playerIndexZeroBased, sad);
        if (s == null)
        {
            // Fallback al default del panel
            s = GetDefaultSpriteFromPanel(playerIndexZeroBased, sad);
            if (s == null) s = GetDefaultSpriteFromPanel(playerIndexZeroBased, !sad);
        }
        if (s == null) return false;
        target.sprite = s;
        target.enabled = true;
        return true;
    }

    private Image EnsureCupImageRef(int playerIndexZeroBased)
    {
        if (playerIndexZeroBased < 0 || playerIndexZeroBased > 3) return null;
        if (cupImages == null || cupImages.Length < 4)
        {
            // Asegurar tamaño del array en caso de que Unity no lo haya serializado
            Array.Resize(ref cupImages, 4);
        }

        if (cupImages[playerIndexZeroBased] == null)
        {
            int oneBased = playerIndexZeroBased + 1;
            // Intentar encontrar por nombres comunes (sensibles a mayúsculas/minúsculas)
            string[] names = new string[] { $"Copa{oneBased}", $"copa{oneBased}" };
            for (int n = 0; n < names.Length && cupImages[playerIndexZeroBased] == null; n++)
            {
                var go = GameObject.Find(names[n]);
                if (go != null)
                {
                    var img = go.GetComponent<Image>();
                    if (img != null)
                    {
                        cupImages[playerIndexZeroBased] = img;
                        break;
                    }
                }
            }
        }
        return cupImages[playerIndexZeroBased];
    }
}
