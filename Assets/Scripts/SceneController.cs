using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }
    private bool isLoading = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Llama la transición (panel sube y cubre) y, cuando termina de cubrir, carga la escena.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        isLoading = true;

        if (!TransitionPanel.Instance)
        {
            Debug.LogWarning("No hay TransitionPanel en la escena. Cargando directo la escena.");
            SceneManager.LoadScene(sceneName);
            isLoading = false;
            return;
        }

        TransitionPanel.Instance.CoverThen(() =>
        {
            // Carga y al entrar a la nueva escena, el TransitionPanel (persistente) hará PlayOut en Start
            SceneManager.LoadScene(sceneName);
            isLoading = false;
        });
    }
}
