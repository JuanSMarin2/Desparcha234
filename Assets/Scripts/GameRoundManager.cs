using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameRoundManager : MonoBehaviour
{
    public static GameRoundManager instance;

    [Tooltip("Índices de los jugadores que han ganado, en orden de llegada (1º, 2º, etc.).")]
    [SerializeField] private List<int> winners = new List<int>();

    [Tooltip("Índices de los jugadores que han perdido, en orden de llegada.")]
    [SerializeField] private List<int> losers = new List<int>();

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

    public void PlayerWin(int winningPlayerIndex)
    {
        winners.Add(winningPlayerIndex);
        Debug.Log("¡El Jugador " + (winningPlayerIndex + 1) + " ha ganado! Posición: " + winners.Count);
        TurnManager.instance.RemovePlayerFromTurn(winningPlayerIndex);
        CheckForRoundEnd();
    }

    public void PlayerLose(int losingPlayerIndex)
    {
        losers.Add(losingPlayerIndex);
        Debug.Log("El Jugador " + (losingPlayerIndex + 1) + " ha sido eliminado.");
        TurnManager.instance.RemovePlayerFromTurn(losingPlayerIndex);
        CheckForRoundEnd();
    }

    private void CheckForRoundEnd()
    {
        if (TurnManager.instance.GetActivePlayerCount() <= 1)
        {
            if (TurnManager.instance.GetActivePlayerCount() == 1)
            {
                PlayerWin(TurnManager.instance.GetActivePlayerIndices()[0]);
            }
            FinalizeRound();
        }
    }

    /// <summary>
    /// Combina las listas de ganadores y perdedores, asigna puntos y carga la siguiente escena.
    /// </summary>
    private void FinalizeRound()
    {
        // 1. Unificamos las posiciones finales.
        List<int> finalPositions = new List<int>();
        finalPositions.AddRange(winners);
        losers.Reverse();
        finalPositions.AddRange(losers);

        // 2. Asignamos los puntos según la posición.
        RoundData.instance.currentPoints = new int[RoundData.instance.numPlayers]; // Limpiamos los puntos de esta ronda
        for (int i = 0; i < finalPositions.Count; i++)
        {
            int playerIndex = finalPositions[i];
            int pointsToAdd = 0;
            switch (i)
            {
                case 0: // Primer lugar
                    pointsToAdd = 3;
                    break;
                case 1: // Segundo lugar
                    pointsToAdd = 2;
                    break;
                case 2: // Tercer lugar
                    pointsToAdd = 1;
                    break;
                case 3: // Cuarto lugar
                    pointsToAdd = 0;
                    break;
                default:
                    pointsToAdd = 0; 
                    break;
            }

            RoundData.instance.currentPoints[playerIndex] = pointsToAdd;


            Debug.Log($"Jugador {playerIndex + 1} obtuvo {pointsToAdd} puntos. Total: {RoundData.instance.totalPoints[playerIndex]}");
        }

        // 3. Guardamos la clasificación final en RoundData para la escena de resultados.
        RoundData.instance.finalPositions.Clear();
        RoundData.instance.finalPositions.AddRange(finalPositions);

        // 4. Cargamos la escena de resultados.
        LoadResultsScene();
    }

    private void LoadResultsScene()
    {
        SceneManager.LoadScene("ResultadosMinijuego");
    }
}