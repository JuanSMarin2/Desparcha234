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

        if (vallas == null || vallas.Length == 0) return;

        float[] probs = new float[vallas.Length];
        bool anyActive = false;

        for (int i = 0; i < vallas.Length; i++)
        {
            float prob = probabilidadBase;
            // Si esta valla es la favorecida para el jugador actual, suma probabilidad extra
            if (jugadorTurno >= 0 && jugadorTurno < vallaFavorJugador.Length && vallaFavorJugador[jugadorTurno] == i)
            {
                prob = Mathf.Clamp01(prob + probabilidadFavor);
            }
            probs[i] = prob;
            bool activar = Random.value < prob;
            if (vallas[i] != null) vallas[i].SetActive(activar);
            if (activar) anyActive = true;
            Debug.Log($"Valla {i} activada: {activar} (prob={prob})");
        }

        // Si ninguna valla resultó activada, forzamos activar una usando selección ponderada por probabilidad
        if (!anyActive)
        {
            float total = 0f;
            for (int i = 0; i < probs.Length; i++) total += probs[i];
            // Si todas las probabilidades son 0 (por alguna configuración), activamos la primera válida
            if (total <= 0f)
            {
                for (int i = 0; i < vallas.Length; i++)
                {
                    if (vallas[i] != null)
                    {
                        vallas[i].SetActive(true);
                        Debug.Log($"Ninguna activa: forzando valla {i} activa por defecto.");
                        break;
                    }
                }
            }
            else
            {
                float r = Random.value * total;
                float acc = 0f;
                for (int i = 0; i < probs.Length; i++)
                {
                    acc += probs[i];
                    if (r <= acc)
                    {
                        if (vallas[i] != null) vallas[i].SetActive(true);
                        Debug.Log($"Ninguna activa: seleccionada valla {i} por peso (prob={probs[i]}). Desired r={r} acc={acc}");
                        break;
                    }
                }
            }
        }
    }


}
