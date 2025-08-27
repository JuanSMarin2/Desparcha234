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

        //  Guardar quién lanzó en el Tejo
        Tejo tejo = esfera.GetComponent<Tejo>();
        if (tejo != null)
            tejo.jugadorID = turnoActual;

        // Desactivar colisión mientras está en el "vuelo"
        Collider col = esfera.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Iniciar el movimiento simulado
        StartCoroutine(MoverTejo(esfera));
    }

    // Corutina para simular movimiento "falso"
    private IEnumerator MoverTejo(GameObject tejoObj)
    {
        Transform tejo = tejoObj.transform;
        CircleCollider2D collider = tejoObj.GetComponent<CircleCollider2D>();

        //  Desactivar el collider durante el lanzamiento
        if (collider != null)
            collider.enabled = false;

        Vector3 start = puntoLanzamiento.position;
        Vector3 dir = CalcularDestino();
        Vector3 end = start + dir * (fuerza * 10f); // usa fuerza + factor de escala

        float duracion = 1.5f;
        float t = 0f;

        Vector3 escalaInicial = tejo.localScale;
        Vector3 escalaFinal = escalaInicial * 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;

            // Movimiento interpolado
            tejo.position = Vector3.Lerp(start, end, t);

            // fijar en el plano 2D (Z=0)
            Vector3 pos = tejo.position;
            pos.z = 0f;
            tejo.position = pos;

            // Simular que se aleja con escala
            tejo.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);

            yield return null;
        }

        //  Reactivar el collider al terminar el movimiento
        if (collider != null)
            collider.enabled = true;

        TurnManager.instance.NextTurn();
    }

    private Vector3 CalcularDestino()
    {
        float radH = anguloHorizontal * Mathf.Deg2Rad;

        // Invertimos el eje X para que coincida con la flecha
        Vector3 dir = new Vector3(-Mathf.Sin(radH), 0, Mathf.Cos(radH));

        dir.y = Mathf.Sin(anguloVertical * Mathf.Deg2Rad);

        return dir.normalized;
    }
}