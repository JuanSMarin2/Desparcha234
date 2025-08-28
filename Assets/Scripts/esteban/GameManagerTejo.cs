using UnityEngine;
using UnityEngine.UI;

public class GameManagerTejo : MonoBehaviour
{
    public static GameManagerTejo instance;

    [Header("Puntajes")]
    public int[] puntajes = new int[4]; // 4 jugadores
    public int puntajeMaximo = 21;

    [Header("UI Puntajes")]
    public Text[] puntajeTextos; // Arrastra 4 Text de UI en el inspector

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void SumarPuntos(int jugadorID, int puntos)
    {

        Debug.Log($"Jugador {jugadorID} gana {puntos} puntos");

        puntajes[jugadorID] += puntos;
        puntajeTextos[jugadorID].text = $"J{jugadorID + 1}: {puntajes[jugadorID]}";

        // Chequear si este jugador llegó a 21
        if (puntajes[jugadorID] >= 21)
        {
            // Mandar el número del ganador a GameRoundManager
            GameRoundManager.instance.PlayerWin(jugadorID);
        }
    }

    // Caso especial: cuando nadie cae en centro ni toca papeletas,
    // revisamos cuál quedó más cerca y damos +1
    public void DarPuntoAlMasCercano(Vector3[] posicionesTejos, Vector3 centro)
    {
        float distanciaMin = float.MaxValue;
        int jugadorCercano = -1;

        for (int i = 0; i < posicionesTejos.Length; i++)
        {
            float dist = Vector3.Distance(posicionesTejos[i], centro);
            if (dist < distanciaMin)
            {
                distanciaMin = dist;
                jugadorCercano = i;
            }
        }

        if (jugadorCercano >= 0)
        {
            SumarPuntos(jugadorCercano, 1);
        }
    }
}
