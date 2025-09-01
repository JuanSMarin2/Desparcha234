using UnityEngine;

public class Tejo : MonoBehaviour
{
    [HideInInspector] public int jugadorID;
    private Rigidbody2D rb;
    private bool yaParo = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!yaParo && rb.linearVelocity.magnitude < 0.1f) // casi quieto
        {
            yaParo = true;
            GameManagerTejo.instance.TejoTermino(this); // avisamos al GameManager
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
        }

        if (other.CompareTag("Papeleta"))
        {
            Debug.Log($"Jugador {jugadorID} golpe� papeleta");
            GameManagerTejo.instance.SumarPuntos(jugadorID, 3);
            other.gameObject.SetActive(false); // desactivar en vez de destruir
        }
    }
}
