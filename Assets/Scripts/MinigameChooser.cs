using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MinigameChooser : MonoBehaviour
{
    // Botón de un minijuego concreto (OnClick)
    public void StartSingle(string gameName)
    {
        var rd = RoundData.instance;
        if (rd == null) return;

        rd.scheduledGames.Clear();
        rd.scheduledGames.Add(gameName);

        Debug.Log("Juego seleccionado (single): " + gameName);
        SceneManager.LoadScene(gameName);
    }

    // Botón Torneo: agenda todos aleatorios y empieza
    public void StartTournament()
    {
        var rd = RoundData.instance;
        if (rd == null) return;

        // Copia TODOS los disponibles a la agenda y baraja
        rd.scheduledGames = new List<string>(rd.availableGames);
        Shuffle(rd.scheduledGames);

        if (rd.scheduledGames.Count > 0)
        {
            string first = rd.scheduledGames[0];
            Debug.Log("Torneo iniciado. Primero: " + first);
            SceneManager.LoadScene(first);
        }
        else
        {
            Debug.LogWarning("No hay minijuegos disponibles para torneo.");
            SceneManager.LoadScene("FinalResults"); // ajusta si usas otro nombre
        }
    }

    //  Llamado desde la escena de resultados (PointsUIManager)
    public void LoadNextScheduledOrFinish()
    {
        var rd = RoundData.instance;
        if (rd == null) return;

        // Quitar de la lista el que acaba de jugarse (el primero)
        if (rd.scheduledGames.Count > 0)
        {
            string justPlayed = rd.scheduledGames[0];
            rd.scheduledGames.RemoveAt(0);
            Debug.Log("Removido de agenda: " + justPlayed);
        }

        // Si queda alguno, cargar el siguiente (que ahora es el índice 0)
        if (rd.scheduledGames.Count > 0)
        {
            string next = rd.scheduledGames[0];
            Debug.Log("Siguiente en agenda: " + next);
            SceneManager.LoadScene(next);
        }
        else
        {
            Debug.Log("Torneo/Single finalizado. Mostrando resultados finales.");
            SceneManager.LoadScene("FinalResults"); // ajusta si usas otro nombre
        }
    }

    // (Si aún quieres este random “suelto”, lo puedes mantener, pero ya no lo usa el flujo de torneo)
    public void RandomGameChooser()
    {
        var rd = RoundData.instance;
        if (rd == null) return;

        if (rd.availableGames.Count > 0)
        {
            int randomIndex = Random.Range(0, rd.availableGames.Count);
            string chosenGame = rd.availableGames[randomIndex];

            Debug.Log("Juego aleatorio: " + chosenGame);
            SceneManager.LoadScene(chosenGame);
        }
        else
        {
            SceneManager.LoadScene("FinalResults");
        }
    }

    private void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
