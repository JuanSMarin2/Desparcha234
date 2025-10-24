using UnityEngine;

public class Tejo : MonoBehaviour
{
    [HideInInspector] public int jugadorID; // quién lanzó este tejo

    [Header("Destrucción por zonas")]
    [Tooltip("Tag del collider que hace desaparecer el tejo al entrar (ej: 'DestroyZone').")]
    public string destroyZoneTag = "DestroyZone";
    [Tooltip("Retraso en segundos antes de destruir el tejo al entrar en la zona")]
    public float destroyDelay = 0.5f;

    private Rigidbody2D rb;
    private bool yaParo = false;
    private bool puedeReportar = false; // hasta que no lo habilitemos desde el lanzador, NO reporta “me detuve”

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Llamado por el lanzador cuando termina la animación de vuelo
    public void HabilitarReporteAlDetenerse()
    {
        puedeReportar = true;
        yaParo = false;
    }

    private void Update()
    {
        if (!puedeReportar || yaParo || rb == null) return;

        // Cuando realmente está quieto, reporta una sola vez
        if (rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            yaParo = true;
            GameManagerTejo.instance.TejoTermino(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // === ZONA DE DESTRUCCIÓN ===
        if (!string.IsNullOrEmpty(destroyZoneTag) && other.CompareTag(destroyZoneTag))
        {
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null) sm.PlaySfx("Tejo:Impacto-Other");

            Destroy(gameObject, destroyDelay);
            return;
        }

        // === TABLERO ===
        if (other.CompareTag("Tablero"))
        {
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null) sm.PlaySfx("Tejo:Impacto-Arena");
            return;
        }

        // === CENTRO ===
        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
            return;
        }

        // === PAPELETA NEUTRA ===
        if (other.CompareTag("Papeleta"))
        {
            ReproducirExplosion();
            Debug.Log($"Jugador {jugadorID} golpeó papeleta");

            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);

            // Ocultamos sprite y collider
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            return;
        }

        // === PAPELETAS ESPECIALES ===
        if (other.CompareTag("Papeleta1")) ManejarPapeletaDestruida(0, other);
        else if (other.CompareTag("Papeleta2")) ManejarPapeletaDestruida(1, other);
        else if (other.CompareTag("Papeleta3")) ManejarPapeletaDestruida(2, other);
        else if (other.CompareTag("Papeleta4")) ManejarPapeletaDestruida(3, other);
    }

    // ---- MÉTODOS ENCAPSULADOS ----

    /// <summary>
    /// Maneja la destrucción de una papeleta específica, restando puntos y notificando al GameManager.
    /// </summary>
    private void ManejarPapeletaDestruida(int idJugador, Collider2D other)
    {
        ReproducirExplosion();

        GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
        GameManagerTejo.instance.RestarPuntos(idJugador, 2);
        other.gameObject.SetActive(false);

        //  Nuevo: notificar destrucción para mostrar icono triste
        GameManagerTejo.instance.NotificarPapeletaDestruida(idJugador);
    }

    /// <summary>
    /// Reproduce el SFX de explosión, si existe el SoundManager.
    /// </summary>
    private void ReproducirExplosion()
    {
        var sm = FindAnyObjectByType<SoundManager>();
        if (sm != null)
        {
            sm.PlaySfx("Tejo:Explosion");
        }
    }
}
