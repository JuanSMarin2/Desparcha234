using UnityEngine;

public class LanzamientoTejo : MonoBehaviour
{
    private Vector3 destino;
    private float velocidad;
    private Vector3 escalaInicial;
    private Vector3 escalaFinal;

    private bool enMovimiento = false;

    public void Iniciar(Vector3 origen, Vector3 destinoFinal, float duracion)
    {
        transform.position = origen;
        destino = destinoFinal;
        velocidad = 1f / duracion;

        escalaInicial = transform.localScale;
        escalaFinal = escalaInicial * 0.5f; // se hace más pequeño al llegar

        enMovimiento = true;
        progreso = 0f;
    }

    private float progreso = 0f;

    void Update()
    {
        if (!enMovimiento) return;

        progreso += Time.deltaTime * velocidad;

        // Movimiento interpolado
        transform.position = Vector3.Lerp(transform.position, destino, progreso);

        // Escala interpolada (se va reduciendo)
        transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, progreso);

        if (progreso >= 1f)
        {
            enMovimiento = false;
            // Aquí queda "pegado" al tablero
        }
    }
}
