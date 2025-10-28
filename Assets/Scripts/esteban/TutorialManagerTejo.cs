using UnityEngine;
using UnityEngine.UI;
using System;

public class TutorialManagerTejo : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject[] tutorialPanels;   // Lista de paneles en orden
    [SerializeField] private GameObject blocker;            // Panel transparente que bloquea interacción
    [SerializeField] private Button continuarButton;        // Botón de continuar
    public Button ContinuarButton => continuarButton;

    // Evento para avisar cuando un panel se cierra
    public static event Action<int> OnPanelCerrado;

    private int currentPanelIndex = 0;

    private void Start()
    {
        //MostrarPanel(0);      
    }

    public void MostrarPanel(int index)
    {
        if (index >= 0 && index < tutorialPanels.Length)
        {
            tutorialPanels[index].SetActive(true);
            if (blocker != null) blocker.SetActive(true);
            currentPanelIndex = index; // Guardamos el panel actual
        }
    }

    public void SiguientePanel()
    {
        // Apagar el panel actual
        if (currentPanelIndex < tutorialPanels.Length)
        {
            tutorialPanels[currentPanelIndex].SetActive(false);
            // Lanza el evento al cerrar un panel
            OnPanelCerrado?.Invoke(currentPanelIndex);
        }

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

    public void ReactivarPrimerPanel()
    {
        // Desactiva todos los paneles y el blocker
        DesactivarTodo();

        // Reinicia el índice y muestra el primer panel
        currentPanelIndex = 0;
        MostrarPanel(0);
    }

    public void DesactivarTodo()
    {
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            if (tutorialPanels[i].activeSelf)
            {
                tutorialPanels[i].SetActive(false);
                // Notificar por cada panel activo que se cerró
                OnPanelCerrado?.Invoke(i);
            }
        }

        if (blocker != null) blocker.SetActive(false);
    }

    public void MostrarPanelPorJugador(int jugadorID, int numPlayers)
    {
        // Limpia cualquier panel previo
        DesactivarTodo();

        int index = -1;

        switch (jugadorID)
        {
            case 1:
                switch (numPlayers)
                {
                    case 2: index = 12; break;
                    case 3: index = 1; break;
                    case 4: index = 4; break;
                }
                break;

            case 2:
                switch (numPlayers)
                {
                    case 2: index = 0; break;
                    case 3: index = 2; break;
                    case 4: index = 5; break;
                }
                break;

            case 3:
                switch (numPlayers)
                {
                    case 3: index = 3; break;
                    case 4: index = 6; break;
                }
                break;

            case 4:
                if (numPlayers == 4) index = 7;
                break;
        }

        if (index >= 0)
            MostrarPanel(index);
        else
            Debug.LogWarning($" No hay panel configurado para Jugador {jugadorID} con {numPlayers} jugadores.");
    }
}

