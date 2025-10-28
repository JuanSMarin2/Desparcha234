using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Muestra en un TMP_Text cuál es el jugador siguiente según el orden actual de TurnManager
/// y opcionalmente muestra un sprite asociado al índice del jugador.
/// </summary>
public class NextPlayerPanel : MonoBehaviour
{
    [Tooltip("TMP component donde se mostrará el texto del siguiente jugador.")]
    [SerializeField] private TextMeshProUGUI nextPlayerText;

    [Tooltip("Image UI donde se mostrará el sprite del siguiente jugador (opcional).")]
    [SerializeField] private Image nextPlayerImage;

    [Tooltip("Array de sprites por índice de jugador (0-based). Asignar sprite para cada jugador aquí.)")]
    [SerializeField] private Sprite[] spritesByPlayer;

    [Tooltip("Prefijo de texto mostrado (se añadirá 'Jugador N' después).")]
    [SerializeField] private string prefix = "Siguiente:";

    private int lastCurrent = -2; // para detección de cambios

    void Start()
    {
        if (nextPlayerText == null)
        {
            Debug.LogWarning("NextPlayerPanel: nextPlayerText no asignado.");
        }

        // Si hay una imagen asignada pero no queremos mostrarla inicialmente, se oculta si no hay sprite
        ApplyVisibilityForImage(-1);
        Refresh();
    }

    void Update()
    {
        if (TurnManager.instance == null) return;

        int current = TurnManager.instance.GetCurrentPlayerIndex();
        if (current != lastCurrent)
        {
            Refresh();
            lastCurrent = current;
        }
    }

    private void Refresh()
    {
        if (nextPlayerText == null && nextPlayerImage == null) return;

        if (TurnManager.instance == null)
        {
            if (nextPlayerText != null) nextPlayerText.text = prefix + " - ";
            ApplyVisibilityForImage(-1);
            return;
        }

        var active = TurnManager.instance.GetActivePlayerIndices();
        if (active == null || active.Count == 0)
        {
            if (nextPlayerText != null) nextPlayerText.text = prefix + " - ";
            ApplyVisibilityForImage(-1);
            return;
        }

        int current = TurnManager.instance.GetCurrentPlayerIndex();
        int pos = active.IndexOf(current);
        if (pos == -1) pos = 0;

        int nextPos = (pos + 1) % active.Count;
        int nextPlayerIndex = active[nextPos];

        if (nextPlayerText != null)
            nextPlayerText.text = string.Format("{0} Jugador {1}", prefix, nextPlayerIndex + 1);

        // Mostrar sprite si existe en el array
        if (nextPlayerImage != null)
        {
            if (spritesByPlayer != null && nextPlayerIndex >= 0 && nextPlayerIndex < spritesByPlayer.Length && spritesByPlayer[nextPlayerIndex] != null)
            {
                nextPlayerImage.sprite = spritesByPlayer[nextPlayerIndex];
                ApplyVisibilityForImage(nextPlayerIndex);
            }
            else
            {
                // No hay sprite asignado para ese índice: ocultar la imagen
                ApplyVisibilityForImage(-1);
            }
        }
    }

    private void ApplyVisibilityForImage(int playerIndexWithSprite)
    {
        if (nextPlayerImage == null) return;
        nextPlayerImage.gameObject.SetActive(playerIndexWithSprite >= 0);
    }
}
