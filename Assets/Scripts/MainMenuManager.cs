using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PlayersPanel;
    [SerializeField] private MinigameChooser minigameChooser;

    public void TogglePlayersPanel()
    {
        PlayersPanel.SetActive(!PlayersPanel.activeSelf);
    }




    public void HowManyPlayers(int players)
    {
        Debug.Log(players);
        RoundData.instance.ResetData();
        RoundData.instance.GetNumberOfPlayers(players);

        minigameChooser.RandomGameChooser();
    }


}
