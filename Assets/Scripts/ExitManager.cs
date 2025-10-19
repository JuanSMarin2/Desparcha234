using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitManager : MonoBehaviour
{
    [SerializeField] private GameObject exitPanel;
    [SerializeField] private Image muteButtonImage;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;


    public static bool ForceReturnToMainMenu { get; private set; }
    private bool isMuted = false;

    void Start()
    {
        ForceReturnToMainMenu = false;
        exitPanel.SetActive(false);
        // Asegura que el estado del audio y el icono sean correctos al inicio.
        UpdateMuteState();
    }

    // --- Funcionalidad para la confirmación de salida ---

    public void Exit()
    {
        exitPanel?.SetActive(true);
        Time.timeScale = 0;
    }

    public void ConfirmAnswer(bool answer)
    {
        ForceReturnToMainMenu = answer;
       Time.timeScale = 1;
        exitPanel?.SetActive(false);
        if (answer)
        {
            Debug.Log("Cargando escena mainmenu");
            SceneController.Instance.LoadScene("MainMenu");
        }
    }

    // --- Funcionalidad para mutear el juego ---

    public void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.pause = isMuted;
        UpdateMuteState();
    }

    private void UpdateMuteState()
    {
        // Actualiza el icono del botón de mutear basado en el estado actual.
        if (muteButtonImage != null)
        {
            muteButtonImage.sprite = isMuted ? mutedSprite : unmutedSprite;
        }
    }
}