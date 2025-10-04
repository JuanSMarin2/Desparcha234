using UnityEngine;
using TMPro;

public class VictoryTagPanel : MonoBehaviour
{
    [Header("Referencias")] [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject[] victoryObjects = new GameObject[4]; // 1..4
    [SerializeField] private TMP_Text textoGanador;
    [SerializeField] private string formatoGanador = "Jugador {0} gana";

    private int _currentWinner = -1;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        HideAllVictoryObjects();
        gameObject.SetActive(false);
    }

    private void HideAllVictoryObjects()
    {
        if (victoryObjects == null) return;
        foreach (var go in victoryObjects) if (go) go.SetActive(false);
    }

    public void ShowWinner(int playerIndex1Based)
    {
        _currentWinner = playerIndex1Based;
        gameObject.SetActive(true);
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        HideAllVictoryObjects();
        int idx = playerIndex1Based - 1;
        if (idx >= 0 && idx < victoryObjects.Length && victoryObjects[idx]) victoryObjects[idx].SetActive(true);
        if (textoGanador) textoGanador.text = string.Format(formatoGanador, playerIndex1Based);
        // Pausa asumida fuera (TagManager ya pone Time.timeScale = 0)
    }

    // Método público para anim event final (opcional) si se pone aquí en vez de TagManager
    public void OnVictoryAnimationFinished()
    {
        TagManager.Instance?.OnVictoryAnimationFinished();
    }
}
