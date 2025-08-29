using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FinalResultsManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI winnerText;

    void Start()
    {
        ShowWinners();
    }

    private void ShowWinners()
    {
        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.totalPoints.Length == 0)
        {
            winnerText.text = "No hay resultados disponibles.";
            return;
        }

        int[] puntos = rd.totalPoints;

        // 1. Buscar puntaje máximo
        int maxPoints = puntos.Max();

        // 2. Buscar jugadores con ese puntaje
        List<int> ganadores = new List<int>();
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == maxPoints)
            {
                ganadores.Add(i);
            }
        }

        // 3. Construir mensaje
        if (ganadores.Count == 1)
        {
            // +1 porque el index 0 es Jugador 1
            int jugador = ganadores[0] + 1;
            winnerText.text = $"Ganador: Jugador {jugador}";
        }
        else
        {
            string jugadores = string.Join(" y ",
                ganadores.Select(g => $"Jugador {g + 1}"));
            winnerText.text = $"Ganadores: {jugadores}";
        }
    }
}
