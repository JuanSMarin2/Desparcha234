using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PlayersPanel;
    [SerializeField] private RoundData roundData;
    [SerializeField] private MinigameChooser minigameChooser;

    public void TogglePlayersPanel()
    {
        PlayersPanel.SetActive(!PlayersPanel.activeSelf);
    }




    public void HowManyPlayers(int players)
    {
        roundData = new RoundData(); //Esto resetea todo lo que hay en round data para una ronda nueva
        roundData.numPlayers = players;

        minigameChooser.RandomGameChooser();
    }


}
