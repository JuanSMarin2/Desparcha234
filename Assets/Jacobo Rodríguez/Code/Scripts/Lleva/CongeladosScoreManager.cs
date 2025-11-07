using UnityEngine;
using System.Collections.Generic;

// Guarda puntos por jugador para el modo Congelados durante la sesión de juego.
// Reglas:
// - Si gana el freezer (congela a todos), el freezer gana 1 punto.
// - Si se acaba el tiempo, cada no-freezer que NO esté congelado gana 1 punto.
// - Persisten hasta el final de la modalidad (o hasta ResetScores()).
[DisallowMultipleComponent]
public class CongeladosScoreManager : MonoBehaviour
{
    public static CongeladosScoreManager Instance { get; private set; }

    [Tooltip("Puntos por jugador 1..4 (índice 0..3)")]
    [SerializeField] private int[] scores = new int[4];

    [Header("Debug")] [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetScores()
    {
        for (int i = 0; i < scores.Length; i++) scores[i] = 0;
        if (debugLogs) Debug.Log("[CongeladosScoreManager] Scores reseteados");
    }

    public void AddPointToFreezer(int freezerIndex1Based)
    {
        int idx0 = Mathf.Clamp(freezerIndex1Based - 1, 0, 3);
        scores[idx0] += 1;
        if (debugLogs) Debug.Log($"[CongeladosScoreManager] +1 al Freezer P{freezerIndex1Based}. Score={scores[idx0]}");
    }

    public void AddPointsToNonFreezersNotFrozen(List<PlayerCongelados> players)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (!p.IsFreezer && !p.IsFrozen)
            {
                int idx0 = Mathf.Clamp(p.PlayerIndex - 1, 0, 3);
                scores[idx0] += 1;
                if (debugLogs) Debug.Log($"[CongeladosScoreManager] +1 a no-freezer P{p.PlayerIndex} por aguantar. Score={scores[idx0]}");
            }
        }
    }

    public void AddPointsToAllNonFreezers(List<PlayerCongelados> players)
    {
        if (players == null) return;
        foreach (var p in players)
        {
            if (p == null) continue;
            if (!p.IsFreezer)
            {
                int idx0 = Mathf.Clamp(p.PlayerIndex - 1, 0, 3);
                scores[idx0] += 1;
                if (debugLogs) Debug.Log($"[CongeladosScoreManager] +1 (runners win) a P{p.PlayerIndex}. Score={scores[idx0]}");
            }
        }
    }

    public int GetScore(int playerIndex1Based)
    {
        int idx0 = Mathf.Clamp(playerIndex1Based - 1, 0, 3);
        return scores[idx0];
    }

    public int[] GetScoresSnapshot()
    {
        var copy = new int[scores.Length];
        scores.CopyTo(copy, 0);
        return copy;
    }
}
