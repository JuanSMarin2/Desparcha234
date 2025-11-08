using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundData : MonoBehaviour
{
    public static RoundData instance;

    public int[] lastRoundPoints;

    public int numPlayers = 4;

     public bool hasCongelados {get; private set; } = false;

    // Catálogo base disponible (nombres = nombres de escenas)
    // Nota: "Congelados" se agrega solo si hasCongelados == true
    public List<string> availableGames = new List<string> { "Canicas", "Catapis", "Tejo", "Zancos", "Lleva", "TingoTingoTango"};

    
    public List<string> scheduledGames = new List<string>(); // << NUEVO
    public int scheduledIndex = 0;                            // << NUEVO

    public List<int> finalPositions = new List<int>();
    public int[] currentPoints;
    public int[] totalPoints;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            currentPoints = new int[numPlayers];
            totalPoints = new int[numPlayers];
            EnsureAvailableGamesIntegrity();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GetNumberOfPlayers(int numPlayers)
    {
        this.numPlayers = numPlayers;
        currentPoints = new int[numPlayers];
        totalPoints = new int[numPlayers];
    }

    public void MarkGameAsPlayed(string gameName)
    {
        availableGames.Remove(gameName);
    }

    public void ResetData()
    {
        numPlayers = 0;
        availableGames = new List<string> { "Canicas", "Catapis", "Tejo", "Zancos", "Lleva", "TingoTingoTango"};
        EnsureAvailableGamesIntegrity();
        finalPositions.Clear();
        scheduledGames.Clear();     // << NUEVO
        scheduledIndex = 0;         // << NUEVO
        currentPoints = new int[numPlayers];
        totalPoints = new int[numPlayers];
    }

    public void GetTotalPoints()
    {
        StartCoroutine(SumPointsWithDelay());
    }

    private IEnumerator SumPointsWithDelay()
    {
        yield return new WaitForSeconds(1f);

        lastRoundPoints = (int[])currentPoints.Clone();

        for (int i = 0; i < numPlayers; i++)
        {
            totalPoints[i] += currentPoints[i];
            currentPoints[i] = 0;
        }


        Debug.Log("TotalPoints actualizado.");
    }

    public void BuyCongelados(){
        hasCongelados = true;
        EnsureAvailableGamesIntegrity();
    }

    private void EnsureAvailableGamesIntegrity()
    {
        const string congelados = "Congelados";
        if (hasCongelados)
        {
            if (!availableGames.Contains(congelados))
                availableGames.Add(congelados);
        }
        else
        {
            // Remover si estuviera presente por estado previo
            availableGames.RemoveAll(g => g == congelados);
        }
    }
}
