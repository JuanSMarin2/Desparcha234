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
        losers.Add(losingPlayerIndex); // se acumulan en orden de eliminación
        Debug.Log("El Jugador " + (losingPlayerIndex + 1) + " ha sido eliminado.");
        TurnManager.instance.RemovePlayerFromTurn(losingPlayerIndex);
        CheckForRoundEnd();
    }

    private bool roundEnded = false;

    private void CheckForRoundEnd()
    {
        if (roundEnded) return;

        int active = TurnManager.instance.GetActivePlayerCount();
        if (active <= 1)
        {
            if (active == 1)
            {
                int last = TurnManager.instance.GetActivePlayerIndices()[0];
                // Añade al ganador una sola vez
                if (!winners.Contains(last) && !losers.Contains(last))
                    winners.Add(last);
            }

            roundEnded = true;
            FinalizeRound();
        }
    }

    /// <summary>
    /// Combina las listas de ganadores y perdedores, asigna puntos y carga la siguiente escena.
    /// </summary>
    private void FinalizeRound()
    {
        // losers = [último, anteúltimo, ..., segundo]
        var finalPositions = new List<int>();
        finalPositions.AddRange(winners);       // 1.º, 2.º, ...
        var losersCopy = new List<int>(losers);
        losersCopy.Reverse();                   //  [segundo, tercero, ..., último]
        finalPositions.AddRange(losersCopy);

        RoundData.instance.currentPoints = new int[RoundData.instance.numPlayers];

        for (int i = 0; i < finalPositions.Count; i++)
        {
            int idx = finalPositions[i];
            int pts = (i == 0) ? 3 : (i == 1) ? 2 : (i == 2) ? 1 : 0;
            RoundData.instance.currentPoints[idx] = pts;
            Debug.Log($"Jugador {idx + 1} obtuvo {pts} puntos. Total: {RoundData.instance.totalPoints[idx]}");
        }

        RoundData.instance.finalPositions.Clear();
        RoundData.instance.finalPositions.AddRange(finalPositions);
        LoadResultsScene();
    }

    private void LoadResultsScene()
    {
        SceneManager.LoadScene("ResultadosMinijuego");
    }
}