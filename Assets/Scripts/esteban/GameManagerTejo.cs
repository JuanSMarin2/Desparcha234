using UnityEngine;
using UnityEngine.UI;
using System.Collections; // <- importante para usar coroutines

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

        if (puntajes[jugadorID] >= puntajeMaximo)
        {
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

    //  Nuevo: llamado por Tejo cuando ya se detuvo
    public void TejoTermino(Tejo tejo)
    {
        StartCoroutine(MoverCentroDespuesDeRetraso(1.5f));
    }

    private IEnumerator MoverCentroDespuesDeRetraso(float delay)
    {
        yield return new WaitForSeconds(delay);

        CentroController centro = FindObjectOfType<CentroController>();
        if (centro != null)
        {
            centro.MoverCentro();
        }
    }
}