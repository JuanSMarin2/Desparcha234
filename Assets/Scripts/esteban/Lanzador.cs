using UnityEngine;
using System.Collections;

public class Lanzador : MonoBehaviour
{
    [Header("Prefabs de cada jugador (esferas o fichas)")]
    public GameObject[] jugadorPrefabs;

    [Header("Punto de lanzamiento")]
    public Transform puntoLanzamiento;

    public float fuerza;
    [HideInInspector] public float anguloHorizontal;
    [HideInInspector] public float anguloVertical;

    private bool lanzando = false; //  nuevo flag

    public void Lanzar()
    {
        if (lanzando) return; //  evita lanzar si ya hay uno en curso
        lanzando = true;

        int turnoActual = TurnManager.instance.CurrentTurn() - 1;
        GameObject prefabJugador = jugadorPrefabs[turnoActual];

        GameObject esfera = Instantiate(prefabJugador, puntoLanzamiento.position, Quaternion.identity);

        Tejo tejo = esfera.GetComponent<Tejo>();
        if (tejo != null)
            tejo.jugadorID = turnoActual;

        Collider col = esfera.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(MoverTejo(esfera));
    }

    private IEnumerator MoverTejo(GameObject tejoObj)
    {
        Transform tejo = tejoObj.transform;
        CircleCollider2D collider = tejoObj.GetComponent<CircleCollider2D>();

        if (collider != null)
            collider.enabled = false;

        Vector3 start = puntoLanzamiento.position;
        Vector3 dir = CalcularDestino();
        Vector3 end = start + dir * (fuerza * 15f);

        float duracion = 1.5f;
        float t = 0f;

        Vector3 escalaInicial = tejo.localScale;
        Vector3 escalaFinal = escalaInicial * 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;

            tejo.position = Vector3.Lerp(start, end, t);
            Vector3 pos = tejo.position;
            pos.z = 0f;
            tejo.position = pos;

            tejo.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);

            yield return null;
        }

        if (collider != null)
            collider.enabled = true;

        lanzando = false; //  permitir otro lanzamiento
        TurnManager.instance.NextTurn();
    }

    private Vector3 CalcularDestino()
    {
        float radH = anguloHorizontal * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(-Mathf.Sin(radH), 0, Mathf.Cos(radH));
        dir.y = Mathf.Sin(anguloVertical * Mathf.Deg2Rad);
        return dir.normalized;
    }
}