using UnityEngine;
using System.Collections.Generic;


public class GameRoundManager : MonoBehaviour
{
    public static GameRoundManager instance;


    [SerializeField] private bool advanceTurnOnRemove = false;

    [Tooltip("�ndices de los jugadores que han ganado, en orden de llegada (1�, 2�, etc.).")]
    [SerializeField] private List<int> winners = new List<int>();

    [Tooltip("�ndices de los jugadores que han perdido, en orden de llegada.")]
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

    public void RemovePlayerFromTurn(int playerIndexToRemove)
    {
        TurnManager.instance.GetActivePlayerIndices().Remove(playerIndexToRemove);

        // reubicar indice de lista si hace falta
        if (TurnManager.instance.GetActivePlayerCount() > 0)
        {
            // normalizar activePlayerListIndex si usas ese patr�n internamente
        }

        // IMPORTANTE: solo avanzar si est� permitido (en Canicas: false)
        if (advanceTurnOnRemove)
        {
            TurnManager.instance.NextTurn();
        }
    }

    public void PlayerWin(int winningPlayerIndex)
    {
        winners.Add(winningPlayerIndex);
        Debug.Log("�El Jugador " + (winningPlayerIndex + 1) + " ha ganado! Posici�n: " + winners.Count);
        TurnManager.instance.RemovePlayerFromTurn(winningPlayerIndex);
        CheckForRoundEnd();
    }

    public void PlayerLose(int losingPlayerIndex)
    {
        losers.Add(losingPlayerIndex); // se acumulan en orden de eliminaci?n
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
                // A?ade al ganador una sola vez
                if (!winners.Contains(last) && !losers.Contains(last))
                    winners.Add(last);
            }

            roundEnded = true;
            FinalizeRound();
        }
    }

    // Nuevo: finalizar la ronda desde puntajes (con empates)
    public void FinalizeRoundFromScores(long[] scores)
    {
        if (RoundData.instance == null || scores == null)
        {
            Debug.LogWarning("No hay RoundData o scores para finalizar la ronda.");
            return;
        }

        int numPlayers = RoundData.instance.numPlayers;
        if (scores.Length < numPlayers)
        {
            Debug.LogWarning("Scores no coincide con numPlayers; se ajustar� al m�nimo disponible.");
            numPlayers = scores.Length;
        }

        // �ndices 0..n-1 ordenados por score DESC, luego �ndice ASC
        int[] order = new int[numPlayers];
        for (int i = 0; i < numPlayers; i++) order[i] = i;
        System.Array.Sort(order, (a, b) =>
        {
            int cmp = scores[b].CompareTo(scores[a]);
            return (cmp != 0) ? cmp : a.CompareTo(b);
        });

        // Preparar currentPoints (puntos de ronda) y finalPositions (orden visual)
        RoundData.instance.currentPoints = new int[RoundData.instance.numPlayers];
        RoundData.instance.finalPositions.Clear();
        for (int i = 0; i < order.Length; i++) RoundData.instance.finalPositions.Add(order[i]);

        // Asignar puntos por rango con empates: 1�=3, 2�=2, 3�=1, 4�+=0
        int rank = 1; // rango actual (1-based)
        for (int i = 0; i < order.Length;)
        {
            long score = scores[order[i]];
            int j = i + 1;
            while (j < order.Length && scores[order[j]] == score) j++; // grupo empatado [i, j)
            int groupSize = j - i;

            int points = PointsForRank(rank);
            for (int k = i; k < j; k++)
            {
                int player = order[k];
                if (player >= 0 && player < RoundData.instance.currentPoints.Length)
                    RoundData.instance.currentPoints[player] = points;
            }

            rank += groupSize; // siguiente rango salta el tama�o del grupo empatado (1,1,3,...) 
            i = j;
        }

        LoadResultsScene();
    }

    // Fallback antiguo (sin scores): usa winners/losers
    public void FinalizeRound()
    {
        List<int> finalPositions = new List<int>();

        // 1) Preferir ranking pre-calculado (por ejemplo, por currentScore descendente)
        if (RoundData.instance != null && RoundData.instance.finalPositions != null && RoundData.instance.finalPositions.Count > 0)
        {
            finalPositions.AddRange(RoundData.instance.finalPositions);
        }
        else
        {
            // 2) Fallback: unificar winners/losers (flujo de eliminaci�n cl�sico)
            finalPositions.AddRange(winners);
            losers.Reverse();
            finalPositions.AddRange(losers);
        }

        // Asegurar tama�o acorde a numPlayers
        int numPlayers = RoundData.instance != null ? RoundData.instance.numPlayers : finalPositions.Count;
        if (RoundData.instance != null)
        {
            RoundData.instance.currentPoints = new int[numPlayers]; // limpiar puntaje de ronda (3/2/1/0)
        }

        // 3) Asignar puntos de ronda seg�n orden (top1->3, top2->2, top3->1, top4->0)
        for (int i = 0; i < finalPositions.Count && i < numPlayers; i++)
        {
            int playerIndex = finalPositions[i];
            int points = PointsForRank(i + 1);
            if (RoundData.instance != null && playerIndex >= 0 && playerIndex < numPlayers)
            {
                RoundData.instance.currentPoints[playerIndex] = points;
            }
        }

        // 4) Guardar clasificaci�n en RoundData (ya viene del ranking si exist�a)
        if (RoundData.instance != null)
        {
            RoundData.instance.finalPositions.Clear();
            RoundData.instance.finalPositions.AddRange(finalPositions);
        }

        // 5) Cargar escena de resultados
        LoadResultsScene();
    }

    private static int PointsForRank(int rank)
    {
        switch (rank)
        {
            case 1: return 3;
            case 2: return 2;
            case 3: return 1;
            default: return 0;
        }
    }

    private void LoadResultsScene()
    {
        StopAllCoroutines();
        SceneController.Instance.LoadScene("ResultadosMinijuego");
    }

    // Nuevo: Finalizar ronda para modo Tag (sin dependencia de TurnManager)
    // eliminationOrder0Based: primer elemento = primer eliminado (último lugar)
    // winner0Based: índice 0-based del ganador (último sobreviviente)
    public void FinalizeTagRound(List<int> eliminationOrder0Based, int winner0Based)
    {
        if (RoundData.instance == null)
        {
            Debug.LogWarning("[GameRoundManager] No RoundData para FinalizeTagRound");
            return;
        }
        int numPlayers = RoundData.instance.numPlayers;
        if (numPlayers <= 0)
        {
            Debug.LogWarning("[GameRoundManager] numPlayers inválido en RoundData");
            return;
        }
        List<int> finalPositions = new List<int>(numPlayers);
        // 1) Ganador primero
        if (winner0Based >= 0 && winner0Based < numPlayers)
            finalPositions.Add(winner0Based);
        else
            Debug.LogWarning($"[GameRoundManager] winner0Based {winner0Based} fuera de rango");
        // 2) Resto: eliminados en orden inverso para reflejar posiciones (último eliminado = 2do lugar)
        if (eliminationOrder0Based != null)
        {
            for (int i = eliminationOrder0Based.Count - 1; i >= 0; i--)
            {
                int idx = eliminationOrder0Based[i];
                if (idx >= 0 && idx < numPlayers && !finalPositions.Contains(idx))
                    finalPositions.Add(idx);
            }
        }
        // 3) Validar faltantes (en caso de inconsistencia)
        for (int i = 0; i < numPlayers; i++)
        {
            if (!finalPositions.Contains(i)) finalPositions.Add(i);
        }
        // 4) Asignar puntos (3/2/1/0)
        RoundData.instance.currentPoints = new int[numPlayers];
        for (int pos = 0; pos < finalPositions.Count && pos < numPlayers; pos++)
        {
            int pIdx = finalPositions[pos];
            int pts = PointsForRank(pos + 1);
            if (pIdx >= 0 && pIdx < numPlayers) RoundData.instance.currentPoints[pIdx] = pts;
        }
        // 5) Guardar finalPositions
        RoundData.instance.finalPositions.Clear();
        RoundData.instance.finalPositions.AddRange(finalPositions);
        Debug.Log("[GameRoundManager] FinalizeTagRound -> ranking: " + string.Join(",", finalPositions));
        LoadResultsScene();
    }
}