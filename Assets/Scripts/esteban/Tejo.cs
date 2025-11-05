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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // ❌ Ya NO desactivamos los prefabs base
    }

    public void HabilitarReporteAlDetenerse()
    {
        puedeReportar = true;
        yaParo = false;
    }

    private void Update()
    {
        if (!puedeReportar || yaParo || rb == null) return;

        if (rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            yaParo = true;
            GameManagerTejo.instance.TejoTermino(this);
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
                GameManagerTejo.instance.MostrarPlusDosParaJugador(jugadorID - 1);

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

    private void ManejarPapeletaDestruida(int idJugador, Collider2D other)
    {
        ReproducirExplosion();
        MostrarEfectoTemporal(efectoExplosionPrefab);

        GameManagerTejo.instance.SumarPuntos(jugadorID - 1, 3);
        GameManagerTejo.instance.RestarPuntos(idJugador, 2);
        other.gameObject.SetActive(false);

        GameManagerTejo.instance.NotificarPapeletaDestruida(idJugador);
    }

    private void ReproducirExplosion()
    {
        var sm = FindAnyObjectByType<SoundManager>();
        if (sm != null)
            sm.PlaySfx("Tejo:Explosion");
    }

    private void MostrarEfectoTemporal(GameObject prefab)
    {
        if (prefab == null) return;

        // Posicionar debajo del tejo
        Vector3 pos = transform.position;
        pos.z -= 1f;

        // Instanciar copia del prefab
        GameObject instancia = Instantiate(prefab, pos, Quaternion.identity);
        instancia.transform.SetParent(null);
        instancia.SetActive(true);

        // Ajustar visibilidad y capa
        SpriteRenderer sr = instancia.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Asegurar visibilidad (alpha = 1)
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;

            //  Si es el efecto de explosión, ponerlo por encima del impacto
            if (prefab == efectoExplosionPrefab)
            {
                sr.sortingOrder += 5; // por ejemplo, 5 niveles más alto
            }
        }

        // Destruir luego del tiempo de duración
        Destroy(instancia, duracionEfecto);
    }
}
