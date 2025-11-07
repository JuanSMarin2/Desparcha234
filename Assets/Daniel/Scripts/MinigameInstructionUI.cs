using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MinigameKind
{
    Proyectiles,
    Orden,
    Circulos,
    Reducir,
    Ritmo
}

// Componente genérico para mostrar instrucciones por minijuego y activar el icono del jugador
public class MinigameInstructionUI : MonoBehaviour
{
    // Colores por índice de jugador (0..3):
    // 0: rojo   #985252
    // 1: azul   #366679
    // 2: amarillo #A8A63A
    // 3: verde  #67A650
    private static readonly Color PlayerColor0 = new Color(152f/255f, 82f/255f, 82f/255f);
    private static readonly Color PlayerColor1 = new Color(54f/255f, 102f/255f, 121f/255f);
    private static readonly Color PlayerColor2 = new Color(168f/255f, 166f/255f, 58f/255f);
    private static readonly Color PlayerColor3 = new Color(103f/255f, 166f/255f, 80f/255f);

    [Header("Contenedores UI")]
    [SerializeField, Tooltip("Panel contenedor de las instrucciones (se activa/desactiva).")]
    private GameObject panel;
    [SerializeField, Tooltip("Texto de instrucciones (TMP_Text).")]
    private TMP_Text messageText;

    [Header("Icono por jugador (sprite por índice)")]
    [SerializeField, Tooltip("Imagen única donde se asignará el sprite según el índice del jugador.")]
    private Image playerIconImage;
    [SerializeField, Tooltip("Sprites por índice de jugador (0-based) para la imagen de icono.")]
    private Sprite[] playerIconSpritesByIndex;

    [Header("(Opcional) Compatibilidad: objetos por jugador")]
    [SerializeField, Tooltip("Alternativa antigua: lista de objetos (uno por jugador). Si no hay Image/sprites, se activará solo el del jugador actual.")]
    private GameObject[] playerIconObjects;

    [Header("Mensajes por minijuego")]
    [TextArea] [SerializeField] private string textProyectiles = "Atrapa los proyectiles tocándolos.";
    [TextArea] [SerializeField] private string textOrden = "Toca los botones en orden (1..N).";
    [TextArea] [SerializeField] private string textCirculos = "Toca los círculos en el orden correcto.";
    [TextArea] [SerializeField] private string textReducir = "Presiona repetidamente para reducir los botones.";
    [TextArea] [SerializeField] private string textRitmo = "Mira el patrón y repítelo tocando las celdas en orden.";

    public void Show(MinigameKind kind, int playerIndex)
    {
        if (messageText != null)
        {
            messageText.text = GetMessage(kind);
            messageText.color = GetInstructionColor(playerIndex);
        }

        // Preferencia: usar una sola Image con sprites por índice
        bool iconSet = false;
        // Reset icon state first
        if (playerIconImage != null)
        {
            playerIconImage.enabled = false;
            playerIconImage.sprite = null;
        }
        if (playerIconObjects != null)
        {
            for (int i = 0; i < playerIconObjects.Length; i++)
            {
                if (playerIconObjects[i] != null) playerIconObjects[i].SetActive(false);
            }
        }
        if (playerIconImage != null && playerIconSpritesByIndex != null && playerIndex >= 0 && playerIndex < playerIconSpritesByIndex.Length)
        {
            playerIconImage.enabled = true;
            playerIconImage.sprite = playerIconSpritesByIndex[playerIndex];
            iconSet = true;
        }
        // Fallback: activar sólo el objeto del jugador
        if (!iconSet && playerIconObjects != null)
        {
            for (int i = 0; i < playerIconObjects.Length; i++)
            {
                if (playerIconObjects[i] != null)
                    playerIconObjects[i].SetActive(i == playerIndex && playerIndex >= 0);
            }
        }

        if (panel != null)
            panel.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
        else
            gameObject.SetActive(false);

        // Reset icon state to avoid stale visuals
        if (playerIconImage != null)
        {
            playerIconImage.enabled = false;
            playerIconImage.sprite = null;
        }
        if (playerIconObjects != null)
        {
            for (int i = 0; i < playerIconObjects.Length; i++)
            {
                if (playerIconObjects[i] != null) playerIconObjects[i].SetActive(false);
            }
        }
    }

    // Actualiza solo el icono (no cambia el texto ni la visibilidad del panel)
    public void UpdatePlayerIcon(int playerIndex)
    {
        // Reset previo
        if (playerIconImage != null)
        {
            playerIconImage.enabled = false;
            playerIconImage.sprite = null;
        }
        if (playerIconObjects != null)
        {
            for (int i = 0; i < playerIconObjects.Length; i++)
            {
                if (playerIconObjects[i] != null) playerIconObjects[i].SetActive(false);
            }
        }

        bool iconSet = false;
        if (playerIconImage != null && playerIconSpritesByIndex != null && playerIndex >= 0 && playerIndex < playerIconSpritesByIndex.Length)
        {
            playerIconImage.enabled = true;
            playerIconImage.sprite = playerIconSpritesByIndex[playerIndex];
            iconSet = true;
        }

        if (!iconSet && playerIndex >= 0 && playerIconObjects != null)
        {
            for (int i = 0; i < playerIconObjects.Length; i++)
            {
                if (playerIconObjects[i] != null)
                    playerIconObjects[i].SetActive(i == playerIndex);
            }
        }

        // Mantener el color del texto sincronizado si cambia el jugador
        if (messageText != null)
        {
            messageText.color = GetInstructionColor(playerIndex);
        }
    }

    private string GetMessage(MinigameKind kind)
    {
        switch (kind)
        {
            case MinigameKind.Proyectiles: return string.IsNullOrWhiteSpace(textProyectiles) ? textProyectiles : textProyectiles;
            case MinigameKind.Orden: return string.IsNullOrWhiteSpace(textOrden) ? textOrden : textOrden;
            case MinigameKind.Circulos: return string.IsNullOrWhiteSpace(textCirculos) ? textCirculos : textCirculos;
            case MinigameKind.Reducir: return string.IsNullOrWhiteSpace(textReducir) ? textReducir : textReducir;
            case MinigameKind.Ritmo: return string.IsNullOrWhiteSpace(textRitmo) ? textRitmo : textRitmo;
            default: return string.Empty;
        }
    }

    private Color GetInstructionColor(int playerIndex)
    {
        if (playerIndex < 0) return Color.white;
        switch (playerIndex % 4)
        {
            case 0: return PlayerColor0; // rojo #985252
            case 1: return PlayerColor1; // azul #366679
            case 2: return PlayerColor2; // amarillo #A8A63A
            case 3: return PlayerColor3; // verde #67A650
            default: return Color.white;
        }
    }
}
