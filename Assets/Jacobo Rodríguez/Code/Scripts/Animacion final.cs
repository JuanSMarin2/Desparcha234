using UnityEngine;

public class Animacionfinal : MonoBehaviour
{
    [Header("Jugadores ganadores (1..4)")]
    [SerializeField] private GameObject jugador1;
    [SerializeField] private GameObject jugador2;
    [SerializeField] private GameObject jugador3;
    [SerializeField] private GameObject jugador4;

    [SerializeField] private GameObject[] ObjetoOcultar;
    private void Awake()
    {
        // Asegurar estado inicial oculto
        if (jugador1) jugador1.SetActive(false);
        if (jugador2) jugador2.SetActive(false);
        if (jugador3) jugador3.SetActive(false);
        if (jugador4) jugador4.SetActive(false);
    }

    // Nueva sobrecarga: activa animación para múltiples ganadores (1-based)
    public void AnimacionFinal(int[] ganadores)
    {
        Debug.Log($"[Animacionfinal] AnimacionFinal múltiples: [{string.Join(",", ganadores ?? new int[0])}]");
        // Reset estado
        if (jugador1) jugador1.SetActive(false);
        if (jugador2) jugador2.SetActive(false);
        if (jugador3) jugador3.SetActive(false);
        if (jugador4) jugador4.SetActive(false);

        if (ganadores != null)
        {
            foreach (var g in ganadores)
            {
                if (g == 1 && jugador1) jugador1.SetActive(true);
                else if (g == 2 && jugador2) jugador2.SetActive(true);
                else if (g == 3 && jugador3) jugador3.SetActive(true);
                else if (g == 4 && jugador4) jugador4.SetActive(true);
                else Debug.LogWarning($"[Animacionfinal] Índice de ganador fuera de rango o GameObject no asignado: {g}");
            }
        }

        if (ObjetoOcultar.Length > 0)
        {
            foreach (GameObject obj in ObjetoOcultar)
            {
                if (obj) obj.SetActive(false);
                Debug.Log($"[Animacionfinal] Objeto a ocultar: {obj.name}");
            }
        }
        ;
    }

    // Activa la animación del ganador indicado (1-based). Suponemos que cada objeto tiene un Animator
    // que reproducirá su estado por defecto al activarse.
    public void AnimacionFinal(int ganador)
    {
        // Delegar en la versión múltiple para unificar lógica
        AnimacionFinal(new int[] { ganador });
    }

    // Este método puede ser llamado desde un Animation Event al final de la animación de victoria
    // para pedir el cierre del mini-juego por puntaje.
    public void OnAnimacionFinalizada()
    {
        var prog = FindAnyObjectByType<Progression>();
        if (prog != null)
        {
            prog.FinalizarPorAnimacion();
        }
        else
        {
            Debug.LogWarning("[Animacionfinal] Progression no encontrado al finalizar animación; no se pudo finalizar por evento.");
        }
    }
}
