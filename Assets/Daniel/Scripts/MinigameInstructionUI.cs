using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MinigameKind
{
    Proyectiles,
    Orden,
    Circulos,
    Reducir
}

// Componente genérico para mostrar instrucciones por minijuego y activar el icono del jugador
public class MinigameInstructionUI : MonoBehaviour
{
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

    public void Show(MinigameKind kind, int playerIndex)
    {
        if (messageText != null)
        {
            messageText.text = GetMessage(kind);
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
    }

    private string GetMessage(MinigameKind kind)
    {
        switch (kind)
        {
            case MinigameKind.Proyectiles: return string.IsNullOrWhiteSpace(textProyectiles) ? textProyectiles : textProyectiles;
            case MinigameKind.Orden: return string.IsNullOrWhiteSpace(textOrden) ? textOrden : textOrden;
            case MinigameKind.Circulos: return string.IsNullOrWhiteSpace(textCirculos) ? textCirculos : textCirculos;
            case MinigameKind.Reducir: return string.IsNullOrWhiteSpace(textReducir) ? textReducir : textReducir;
            default: return string.Empty;
        }
    }
}
