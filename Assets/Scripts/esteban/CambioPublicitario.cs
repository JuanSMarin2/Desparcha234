using UnityEngine;

public class CambioPublicitario : MonoBehaviour
{
    [Header("Vallas publicitarias")]
    public GameObject[] vallas;

    [Tooltip("Probabilidad base de que una vaya se active (0-1)")]
    [Range(0f,1f)] public float probabilidadBase = 0.3f;
    [Tooltip("Probabilidad extra si es el jugador favorecido")]
    [Range(0f,1f)] public float probabilidadFavor = 0.5f;
    [Tooltip("Índice de la valla favorecida por jugador (0-based, uno por jugador)")]
    public int[] vallaFavorJugador = new int[4]; // Asignar en inspector: para cada jugador, qué valla tiene más chance

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int jugadorTurno = TurnManager.instance != null ? TurnManager.instance.CurrentTurn() - 1 : 0; // 0-based
        for (int i = 0; i < vallas.Length; i++)
        {
            float prob = probabilidadBase;
            // Si esta valla es la favorecida para el jugador actual, suma probabilidad extra
            if (jugadorTurno >= 0 && jugadorTurno < vallaFavorJugador.Length && vallaFavorJugador[jugadorTurno] == i)
            {
                prob += probabilidadFavor;
            }
            bool activar = Random.value < prob;
            if (vallas[i] != null) vallas[i].SetActive(activar);        
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
