using UnityEngine;

public class ArrowColorManager : MonoBehaviour
{
    [Header("Sprites de la flecha horizontal")]
    [SerializeField] private SpriteRenderer horizontalSquare;
    [SerializeField] private SpriteRenderer horizontalTriangle;

    [Header("Sprites de la flecha vertical")]
    [SerializeField] private SpriteRenderer verticalSquare;
    [SerializeField] private SpriteRenderer verticalTriangle;

    [Header("Colores de los jugadores (en orden 1,2,3,4)")]
    [SerializeField] private Color[] playerColors;

    void Update()
    {
        UpdateArrowColor();
    }

    private void UpdateArrowColor()
    {
        int currentPlayer = TurnManager.instance.CurrentTurn();

        if (currentPlayer > 0 && currentPlayer <= playerColors.Length)
        {
            Color currentColor = playerColors[currentPlayer - 1];
            currentColor.a = 1f; // aseguramos que sea opaco

            // Debug para ver que color se aplica
            Debug.Log("Aplicando color: " + currentColor + " al jugador " + currentPlayer);

            horizontalSquare.color = currentColor;
            horizontalTriangle.color = currentColor;
            verticalSquare.color = currentColor;
            verticalTriangle.color = currentColor;
        }
    }
}
