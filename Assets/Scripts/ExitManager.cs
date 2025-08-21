using UnityEngine;
using UnityEngine.SceneManagement;
public class ExitManager : MonoBehaviour
{
    [SerializeField] private GameObject exitPanel;

    void Start()
    {
        exitPanel.SetActive(false);
    }



    public void Exit()
    {

        exitPanel?.SetActive(true);
        Time.timeScale = 0;
    }


    public void ConfitmAnswer(bool answer)
    {
        Time.timeScale = 1;
        exitPanel?.SetActive(false);
        if (answer) {

            SceneManager.LoadScene("MainMenu");

        } 


    }
}
