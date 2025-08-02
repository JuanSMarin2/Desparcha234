using System.Collections;
using TMPro;
using UnityEngine;

public class PointsUIManager : MonoBehaviour
{
    [Header("Textos de puntaje por jugador")]
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private TextMeshProUGUI player3Text;
    [SerializeField] private TextMeshProUGUI player4Text;

    [SerializeField] private int delayToChangeScene = 5;

    [SerializeField] private MinigameChooser minigameChooser;

    private TextMeshProUGUI[] playerTexts;

    void Awake()
    {
        // Guardamos los textos en un array para accederlos con índice
        playerTexts = new TextMeshProUGUI[] { player1Text, player2Text, player3Text, player4Text };

      
    }
    private void Start()
    {
        RoundData.instance.GetTotalPoints();
      
      StartCoroutine(SceneChangeTimer());
    }
    private void Update()
    {
        UpdatePointsUI();
    }

    public void UpdatePointsUI()
    {
        int numPlayers = RoundData.instance.numPlayers;

        for (int i = 0; i < numPlayers; i++)
        {
            if (playerTexts[i] != null)
            {
                playerTexts[i].text = $"Jugador {i + 1}: {RoundData.instance.totalPoints[i]} pts";
                playerTexts[i].gameObject.SetActive(true);
            }
        }

        // Oculta los textos de jugadores que no están en esta ronda
        for (int i = numPlayers; i < playerTexts.Length; i++)
        {
            if (playerTexts[i] != null)
            {
                playerTexts[i].gameObject.SetActive(false);
            }
        }
    }


    private IEnumerator SceneChangeTimer()
    {
        yield return new WaitForSeconds(delayToChangeScene);
        minigameChooser.RandomGameChooser();
    }
}
