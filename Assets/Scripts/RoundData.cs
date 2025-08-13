using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RoundData : MonoBehaviour
{
    public static RoundData instance;
    public int numPlayers = 4;
    public List<string> availableGames = new List<string> { "Canicas", "Catapis", "Tejo", "Zancos" };
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
        availableGames = new List<string> { "Canicas", "Catapis", "Tejo", "Zancos" };
        finalPositions.Clear();
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

        for (int i = 0; i < numPlayers; i++)
        {
            totalPoints[i] += currentPoints[i];
        }

        Debug.Log("TotalPoints actualizado.");

    }
}
