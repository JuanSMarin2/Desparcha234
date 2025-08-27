using UnityEngine;

public class Tejo : MonoBehaviour
{
    [HideInInspector] public int jugadorID; // asignado por Lanzador

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si entra al centro
        if (other.CompareTag("Centro"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 6);
        }

        // Si golpea papeleta
        if (other.CompareTag("Papeleta"))
        {
            GameManagerTejo.instance.SumarPuntos(jugadorID, 3);
            Destroy(other.gameObject); // desaparece la papeleta
        }
    }
}
