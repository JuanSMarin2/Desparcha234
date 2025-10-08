using UnityEngine;
using UnityEngine.UI;

public class WinnerPanel : MonoBehaviour
{
    [Header("Destino donde poner el icono")]
    [SerializeField] private RectTransform anchor;        // si es null, se usa el RectTransform del propio panel

    [Header("Iconos de jugadores (UI)")]
    [SerializeField] private GameObject[] playerIcons;    // 0=J1, 1=J2, 2=J3, 3=J4

    [Header("Opcional")]
    [SerializeField] private bool reparentToAnchor = false;   // si quieres que el icono quede hijo del panel
    [SerializeField] private bool hideOthers = false;         // si quieres ocultar otros iconos mientras se muestra el ganador
    [SerializeField] private GameObject blocker;

    [Header("Colores por jugador")]
    [SerializeField] private Color colorJugador1 = Color.red;
    [SerializeField] private Color colorJugador2 = Color.blue;
    [SerializeField] private Color colorJugador3 = Color.yellow;
    [SerializeField] private Color colorJugador4 = Color.green;

    // índice que nos “prepara” MarbleShooter antes de activar el panel
    private int preparedWinnerIndex = -1;

    // referencia al Image del propio panel
    private Image panelImage;

    // Llamado por MarbleShooter ANTES de SetActive(true)
    public void Prepare(int winnerIndex)
    {
        preparedWinnerIndex = winnerIndex;
    }

    private void Start()
    {
        blocker.SetActive(true);
        if (anchor == null) anchor = GetComponent<RectTransform>();
        panelImage = GetComponent<Image>(); // obtiene el Image de "this"

        if (preparedWinnerIndex >= 0)
            ApplyWinner(preparedWinnerIndex);
    }

    private void ApplyWinner(int winnerIndex)
    {
        if (playerIcons == null || winnerIndex < 0 || winnerIndex >= playerIcons.Length) return;
        var iconGO = playerIcons[winnerIndex];
        if (!iconGO) return;

        if (hideOthers)
        {
            for (int i = 0; i < playerIcons.Length; i++)
                if (playerIcons[i]) playerIcons[i].SetActive(i == winnerIndex);
        }

        iconGO.SetActive(true);
        var iconRT = iconGO.transform as RectTransform;
        if (iconRT != null && anchor != null)
        {
            if (reparentToAnchor)
            {
                iconRT.SetParent(anchor, worldPositionStays: false);
                iconRT.anchoredPosition = Vector2.zero;
                iconRT.localScale = Vector3.one;
            }
            else
            {
                iconRT.position = anchor.position;
                iconRT.localScale = Vector3.one;
            }
        }

        //  Cambiar el color del panel según el jugador
        if (panelImage != null)
        {
            switch (winnerIndex)
            {
                case 0: panelImage.color = colorJugador1; break;
                case 1: panelImage.color = colorJugador2; break;
                case 2: panelImage.color = colorJugador3; break;
                case 3: panelImage.color = colorJugador4; break;
            }
        }
    }
}
