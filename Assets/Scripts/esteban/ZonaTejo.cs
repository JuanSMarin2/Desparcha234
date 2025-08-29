using UnityEngine;

public class ZonaTejo : MonoBehaviour
{
    public enum TipoZona { Tablero, Centro, Papeleta }
    public TipoZona tipo;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Tejo"))
        {
            switch (tipo)
            {
                case TipoZona.Tablero:
                    Debug.Log("Cayó en el tablero (punto básico).");
                    break;
                case TipoZona.Centro:
                    Debug.Log("¡Cayó en el centro!");
                    break;
                case TipoZona.Papeleta:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
            }
        }
    }
}
