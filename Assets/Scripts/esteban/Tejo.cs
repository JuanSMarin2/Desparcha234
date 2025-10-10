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

    //  nuevo: hasta que no lo habilitemos desde el lanzador, NO reporta “me detuve”
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
        // Si choca con la zona configurada para destruir tejos, lo destruimos (con delay)
        if (!string.IsNullOrEmpty(destroyZoneTag) && other.CompareTag(destroyZoneTag))
        {
            // reproducir SFX al terminar la animación de vuelo (impacto visual)
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Impacto-Other");
            }
            Destroy(gameObject, destroyDelay);
            return;
        }

        //  Centro
        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
            return;
        }

        //  Papeleta neutra
        if (other.CompareTag("Papeleta"))
        {
            // reproducir SFX de explosión al impactar una papeleta
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Explosion");
            }
            Debug.Log($"Jugador {jugadorID} golpeó papeleta");
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            // En vez de SetActive(false), ocultamos su sprite y collider
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            return;
        }

        //  Papeletas especiales (restan al dueño de esa papeleta)
        if (other.CompareTag("Papeleta1"))
        {
            // reproducir SFX de explosión al impactar una papeleta
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Explosion");
            }
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(0, 2);
            other.gameObject.SetActive(false);
            return;
        }

        if (other.CompareTag("Papeleta2"))
        {
            // reproducir SFX de explosión al impactar una papeleta
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Explosion");
            }
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(1, 2);
            other.gameObject.SetActive(false);
            return;
        }

        if (other.CompareTag("Papeleta3"))
        {
            // reproducir SFX de explosión al impactar una papeleta
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Explosion");
            }
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(2, 2);
            other.gameObject.SetActive(false);
            return;
        }

        if (other.CompareTag("Papeleta4"))
        {
            // reproducir SFX de explosión al impactar una papeleta
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null)
            {
                sm.PlaySfx("Tejo:Explosion");
            }
            GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
            GameManagerTejo.instance.RestarPuntos(3, 2);
            other.gameObject.SetActive(false);
            return;
        }
    }
}
