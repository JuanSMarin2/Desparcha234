using UnityEngine;
using System.Collections;
public class Lanzador : MonoBehaviour
{
    [Header("Prefabs de cada jugador (esferas o fichas)")]
    public GameObject[] jugadorPrefabs; // ahora son GameObject, no Rigidbody

    [Header("Punto de lanzamiento")]
    public Transform puntoLanzamiento;

    [HideInInspector] public float fuerza;
    [HideInInspector] public float anguloHorizontal;
    [HideInInspector] public float anguloVertical;

    

    public void Lanzar()
    {
        // Obtener el índice de turno actual (0-3)
        int turnoActual = TurnManager.instance.CurrentTurn() - 1;

        // Elegir el prefab correcto
        GameObject prefabJugador = jugadorPrefabs[turnoActual];

        // Instanciar el prefab
        GameObject esfera = Instantiate(prefabJugador, puntoLanzamiento.position, Quaternion.identity);

        // Iniciar el movimiento simulado
        StartCoroutine(MoverTejo(esfera.transform));
    }

    // Corutina para simular movimiento "falso"
    private IEnumerator MoverTejo(Transform tejo)
    {
        Vector3 start = puntoLanzamiento.position;
        Vector3 dir = CalcularDestino(); // usa tus ángulos
        Vector3 end = start + dir * (fuerza * 5f); // 5 = factor de distancia, puedes ajustar

        float duracion = 1.5f; // tiempo de trayecto
        float t = 0f;

        Vector3 escalaInicial = tejo.localScale;
        Vector3 escalaFinal = escalaInicial * 0.5f; // se hace más pequeño al alejarse

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;

            // mover posición
            tejo.position = Vector3.Lerp(start, end, t);

            // reducir escala
            tejo.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);

            yield return null;
        }

        // Aquí el tejo ya "llegó" -> podrías avisar a ZonaTejo si quieres
    }

    private Vector3 CalcularDestino()
    {
        // Convertimos ángulos y fuerza en un desplazamiento X-Y
        float radH = anguloHorizontal * Mathf.Deg2Rad;
        float radV = anguloVertical * Mathf.Deg2Rad;

        float distancia = fuerza * 5f; // factor de escala ajustable

        float dx = Mathf.Cos(radH) * distancia;
        float dy = Mathf.Sin(radV) * distancia;

        return puntoLanzamiento.position + new Vector3(dx, dy, 0);
    }
}
