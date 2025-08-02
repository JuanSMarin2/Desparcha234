using TMPro;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Configuración del Turno")]
    public static TurnManager instance;

    [Header("El turno del jugador actual (0, 1, 2, 3). J1 = 0, J2 = 1, J3 = 2, J4 = 3")] //Chat Gpt me rogó que lo haga desde 0
    [Tooltip("El turno del jugador actual (0, 1, 2, 3). J1 = 0, J2 = 1, J3 = 2, J4 = 3")]
    [SerializeField] private int currentPlayerTurn;

    private int numberOfPlayers;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI turnText; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (RoundData.instance == null)
        {
            Debug.LogError("RoundData.instance no está inicializado.");
            return;
        }

        numberOfPlayers = RoundData.instance.numPlayers;
        currentPlayerTurn = Random.Range(0, numberOfPlayers);

        Debug.Log("Empieza el jugador: Jugador " + (currentPlayerTurn + 1));

        UpdateTurnText();
    }

    public int GetCurrentPlayerTurn()
    {
        return currentPlayerTurn;
    }

    public void NextTurn()
    {
        currentPlayerTurn++;

        if (currentPlayerTurn >= numberOfPlayers)
        {
            currentPlayerTurn = 0;
        }

        Debug.Log("Siguiente turno. Ahora le toca al Jugador " + (currentPlayerTurn + 1));
        UpdateTurnText();
    }

    private void UpdateTurnText()
    {
        if (turnText != null)
        {
            turnText.text = "Turno del jugador: " + (currentPlayerTurn + 1);
        }
    }
} 
 