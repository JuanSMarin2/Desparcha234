using UnityEngine;

public class Tejo : MonoBehaviour
{
    [HideInInspector] public int jugadorID; // quién lanzó este tejo

    private Rigidbody2D rb;
    private bool yaParo = false;

    // 👇 nuevo: hasta que no lo habilitemos desde el lanzador, NO reporta “me detuve”
    private bool puedeReportar = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Llamado por el lanzador cuando termina la animación de vuelo
    public void HabilitarReporteAlDetenerse()
    {
        puedeReportar = true;
        yaParo = false; // por si acaso
    }

    private void Update()
    {
        if (!puedeReportar || yaParo || rb == null) return;

        // Cuando realmente está quieto (después del vuelo/collisions), reporta una sola vez
        if (rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            yaParo = true;
            GameManagerTejo.instance.TejoTermino(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ⚡ Centro
        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
        }

        // ⚡ Papeleta neutra
        if (other.CompareTag("Papeleta"))
        {
            Debug.Log($"Jugador {jugadorID} golpeó papeleta");
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            other.gameObject.SetActive(false);
        }

        // ⚡ Papeletas especiales (restan al dueño de esa papeleta)
        if (other.CompareTag("Papeleta1"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(0, 2);
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Papeleta2"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(1, 2);
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Papeleta3"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(2, 2);
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Papeleta4"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(3, 2);
            other.gameObject.SetActive(false);
        }
    }
}
