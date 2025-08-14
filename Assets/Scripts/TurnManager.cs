using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    [Tooltip("�ndices de los jugadores que a�n est�n en la partida.")]
    [SerializeField] private List<int> activePlayerIndices = new List<int>();

    private int activePlayerListIndex;
    private int totalPlayers;

    [SerializeField] private TextMeshProUGUI turnText;


    public int CurrentTurn() => GetCurrentPlayerIndex() + 1; //Devuelve el numero del jugador que tiene el turno

    public void NextTurn()
    {
        // A�adimos una verificaci�n para evitar el error si solo queda un jugador.
        if (activePlayerIndices.Count <= 1)
        {
            Debug.Log("No hay m�s turnos. Partida finalizada.");
            return;
        }

        activePlayerListIndex++;
        if (activePlayerListIndex >= activePlayerIndices.Count)
        {
            activePlayerListIndex = 0;
        }
    }

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
        totalPlayers = RoundData.instance.numPlayers;

        for (int i = 0; i < totalPlayers; i++)
        {
            activePlayerIndices.Add(i);
        }

        // Verificamos si hay jugadores activos antes de asignar el turno inicial.
        if (activePlayerIndices.Count > 0)
        {
            activePlayerListIndex = Random.Range(0, activePlayerIndices.Count);
            Debug.Log("Juego iniciado con " + totalPlayers + " jugadores.");
            Debug.Log("El turno inicial es para el Jugador " + (GetCurrentPlayerIndex() + 1));
        }
        else
        {
            Debug.LogError("No hay jugadores activos para iniciar la partida.");
        }
    }

    void Update()
    {
        UpdateTurnText();
    }

    private void UpdateTurnText()
    {
        if (turnText != null && activePlayerIndices.Count > 0) // A�adimos una verificaci�n aqu�
        {
            int playerNumber = GetCurrentPlayerIndex() + 1;
            turnText.text = "Turno del jugador: " + playerNumber.ToString();
        }
        else if (turnText != null)
        {
            // Si no hay jugadores, mostramos un mensaje por defecto.
            turnText.text = "Partida finalizada.";
        }
    }

  

    /// <summary>
    /// Devuelve el �ndice del jugador actual (0-based) para su l�gica de juego.
    /// </summary>
    public int GetCurrentPlayerIndex()
    {
        // VERIFICACI�N CLAVE: Nos aseguramos de que haya jugadores activos.
        if (activePlayerIndices.Count > 0)
        {
            
            return activePlayerIndices[activePlayerListIndex];
           
        }
        return -1; // Devolvemos -1 o un valor que indique que no hay jugadores.
    }

    public List<int> GetActivePlayerIndices()
    {
        return activePlayerIndices;
    }

    public int GetActivePlayerCount()
    {
        return activePlayerIndices.Count;
    }

 

    public void RemovePlayerFromTurn(int playerIndexToRemove)
    {
        activePlayerIndices.Remove(playerIndexToRemove);

        if (activePlayerListIndex >= activePlayerIndices.Count)
        {
            activePlayerListIndex = 0;
        }

        NextTurn();
    }
}