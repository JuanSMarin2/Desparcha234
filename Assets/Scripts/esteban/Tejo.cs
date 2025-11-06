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

     
            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Impact");

            Destroy(gameObject, destroyDelay);
            return;
        }

        if (other.CompareTag("Tablero"))
        {
            var sm = FindAnyObjectByType<SoundManager>();
            if (sm != null) sm.PlaySfx("Tejo:Impacto-Arena");

   
            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Impact");

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

  
        Vector3 pos = transform.position;
        pos.z -= 1f;

      
        GameObject instancia = Instantiate(prefab, pos, Quaternion.identity);
        instancia.transform.SetParent(null);
        instancia.SetActive(true);

   
        SpriteRenderer sr = instancia.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
    
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;

       
            if (prefab == efectoExplosionPrefab)
            {
                sr.sortingOrder += 5; 
            }
        }


        Destroy(instancia, duracionEfecto);
    }
}
