using UnityEngine;
using UnityEngine.UI;

public class TutorialManagerTejo : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject[] tutorialPanels;   // Lista de paneles en orden
    [SerializeField] private GameObject blocker;            // Panel transparente que bloquea interacción
    [SerializeField] private Button continuarButton;        // Botón de continuar

    private int currentPanelIndex = 0;

    private void Start()
    {              
         MostrarPanel(0);      
    }

    private void MostrarPanel(int index)
    {
        if (index >= 0 && index < tutorialPanels.Length)
        {
            tutorialPanels[index].SetActive(true);
            if (blocker != null) blocker.SetActive(true);
        }
    }

    public void SiguientePanel()
    {
        // apaga el panel actual
        if (currentPanelIndex < tutorialPanels.Length)
            tutorialPanels[currentPanelIndex].SetActive(false);

        currentPanelIndex++;

        if (currentPanelIndex < tutorialPanels.Length)
        {
            MostrarPanel(currentPanelIndex);
        }
        else
        {
            // Ya no quedan más paneles
            DesactivarTodo();

            // Guardar preferencia
            PlayerPrefs.SetInt("TutorialMostrado", 1);
            PlayerPrefs.Save();
        }
    }

    public void DesactivarTodo()
    {
        foreach (var panel in tutorialPanels)
            panel.SetActive(false);

        if (blocker != null) blocker.SetActive(false);
    }
}
