using System.Collections.Generic;
using UnityEngine;

public class RoundData : MonoBehaviour  // Esta clase guarda entre escenas todos los datos relevantes para la ronda
{
  

    public static RoundData instance;
    public int numPlayers = 2; // Le puse default 2 para que no crashee si algo
    public List<string> availableGames = new List<string> { "Canicas", "Catapis", "Tejo" }; //Aqui se guardan los minijuegos para escoger aleatoriamente,
                                                                                          //cuando uno se juega se quita de la lista

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }
    public void MarkGameAsPlayed(string gameName)
    {
        availableGames.Remove(gameName);
    }
}