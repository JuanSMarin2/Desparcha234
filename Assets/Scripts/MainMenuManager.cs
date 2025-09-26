using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject PlayersPanel;
    [SerializeField] private MinigameChooser minigameChooser;

    [SerializeField] private GameObject gamesPanel;

    public void TogglePlayersPanel()
    {
        PlayersPanel.SetActive(!PlayersPanel.activeSelf);
    }




    public void HowManyPlayers(int players)
    {
        Debug.Log(players);
        RoundData.instance.ResetData();
        RoundData.instance.GetNumberOfPlayers(players);

        gamesPanel.SetActive(true);
    }

    public void ShopButton()
    {

        SceneManager.LoadScene("Tienda");
    }


}
