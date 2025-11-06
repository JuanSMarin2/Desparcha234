using UnityEngine;
using System.Collections;

public class Tejo : MonoBehaviour
{
    [HideInInspector] public int jugadorID;

    [Header("Destrucción por zonas")]
    public string destroyZoneTag = "DestroyZone";
    public float destroyDelay = 0.5f;

    [Header("Efectos visuales")]
    public GameObject efectoImpactoPrefab;
    public GameObject efectoExplosionPrefab;
    public float duracionEfecto = 0.5f;

    private Rigidbody2D rb;
    private bool yaParo = false;
    private bool puedeReportar = false;

    // 🔹 Contador de papeletas destruidas en un mismo lanzamiento
    private int papeletasDestruidasEsteLanzamiento = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void HabilitarReporteAlDetenerse()
    {
        puedeReportar = true;
        yaParo = false;
        papeletasDestruidasEsteLanzamiento = 0; // reiniciar contador en cada tiro
    }

    private void Update()
    {
        if (!puedeReportar || yaParo || rb == null) return;

        if (rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            yaParo = true;

            // Cuando el tejo termina su recorrido, reactivamos las papeletas después de un pequeño delay
            StartCoroutine(ReactivarPapeletasTrasDelay(0.5f));

            if (GameManagerTejo.instance != null)
                GameManagerTejo.instance.TejoTermino(this);
        }
    }

    private IEnumerator ReactivarPapeletasTrasDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allObjects)
        {
            if (!go.scene.IsValid()) continue;

            if (go.CompareTag("Papeleta") ||
                go.CompareTag("Papeleta1") ||
                go.CompareTag("Papeleta2") ||
                go.CompareTag("Papeleta3") ||
                go.CompareTag("Papeleta4"))
            {
                go.SetActive(true);

                // Aseguramos visibilidad si fue desactivado parcialmente
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = true;
                var col = go.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }
        }
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!string.IsNullOrEmpty(destroyZoneTag) && other.CompareTag(destroyZoneTag))
        {
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null) sm.PlaySfx("Tejo:Impacto-Other");
            Destroy(gameObject, destroyDelay);
            return;
        }

        if (other.CompareTag("Tablero"))
        {
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null) sm.PlaySfx("Tejo:Impacto-Arena");
            MostrarEfectoTemporal(efectoImpactoPrefab);
            return;
        }

        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
            return;
        }

        if (other.CompareTag("Papeleta"))
        {
            ReproducirExplosion();
            MostrarEfectoTemporal(efectoExplosionPrefab);
            Debug.Log($"Jugador {jugadorID} golpeó papeleta");

            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);

            // Feedback visual: +2 para el jugador que lanzó (índice 0-based)
            if (GameManagerTejo.instance != null)
                GameManagerTejo.instance.MostrarPlusTresParaJugador(jugadorID - 1);

            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            return;
        }

        if (other.CompareTag("Papeleta1")) ManejarPapeletaDestruida(0, other);
        else if (other.CompareTag("Papeleta2")) ManejarPapeletaDestruida(1, other);
        else if (other.CompareTag("Papeleta3")) ManejarPapeletaDestruida(2, other);
        else if (other.CompareTag("Papeleta4")) ManejarPapeletaDestruida(3, other);
    }

    private IEnumerator EsperarAntesDeDestruir()
    {
        yield return new WaitForSeconds(0.25f);
        Destroy(gameObject, destroyDelay);
    }

    private void ManejarPapeletaDestruida(int idJugador, Collider2D other)
    {
        // Sonido y efecto
        ReproducirExplosion();
        MostrarEfectoTemporal(efectoExplosionPrefab);

        // Sumar puntos al lanzador
        if (GameManagerTejo.instance != null)
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);

        // Restar puntos al dueño de la papeleta
        if (idJugador >= 0 && GameManagerTejo.instance != null)
            GameManagerTejo.instance.RestarPuntos(idJugador, 2);

        // Acumular impactos de este lanzamiento
        papeletasDestruidasEsteLanzamiento++;
        int totalPuntos = papeletasDestruidasEsteLanzamiento * 3;

        // Mostrar feedback acumulado (+3, +6, +9...)
        if (GameManagerTejo.instance != null)
            GameManagerTejo.instance.MostrarFeedbackJugador(jugadorID - 1, $"+{totalPuntos}");

        Debug.Log($"[Tejo] Jugador {jugadorID} ha destruido {papeletasDestruidasEsteLanzamiento} papeletas. Feedback acumulado: +{totalPuntos}");

        // Desactivar papeleta afectada
        other.gameObject.SetActive(false);

        // Notificar al GameManager (icono triste del jugador afectado)
        if (idJugador >= 0 && GameManagerTejo.instance != null)
            GameManagerTejo.instance.NotificarPapeletaDestruida(idJugador);
    }

    private void ReproducirExplosion()
    {
        var sm = FindAnyObjectByType<SoundManager>();
        if (sm != null) sm.PlaySfx("Tejo:Explosion");
    }

    private void MostrarEfectoTemporal(GameObject prefab)
    {
        if (prefab == null) return;
        Vector3 pos = transform.position;
        pos.z -= 1f;

        GameObject instancia = Instantiate(prefab, pos, Quaternion.identity);
        instancia.SetActive(true);

        // Poner la explosión por encima del tablero
        SpriteRenderer sr = instancia.GetComponent<SpriteRenderer>();
        if (sr != null && prefab == efectoExplosionPrefab)
            sr.sortingOrder = 30;

        Destroy(instancia, duracionEfecto);
    }
}
