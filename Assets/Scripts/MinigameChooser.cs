using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameChooser : MonoBehaviour
{
    public void RandomGameChooser()
    {
 
        RoundData roundData = RoundData.instance;
        if (roundData.availableGames.Count > 0)
        {
            int randomIndex = Random.Range(0, roundData.availableGames.Count);

  
            string chosenGame = roundData.availableGames[randomIndex];
            roundData.MarkGameAsPlayed(chosenGame);

            Debug.Log("Juego seleccionado: " + chosenGame);
      
            SceneManager.LoadScene(chosenGame); //Los nombres de las escenas tienen que ser iguales a los de la lista que puse en RoundData, no vayan a cambiarlos
        }
        else
        {
            // SceneManager.LoadScene("FinalResults");
        }
    }
}