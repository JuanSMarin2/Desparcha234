using UnityEngine;

public class ZonaTejo : MonoBehaviour
{
    public enum TipoZona { Tablero, Centro, Papeleta, Papeleta1, Papeleta2, Papeleta3, Papeleta4 }
    public TipoZona tipo;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Tejo"))
        {
            switch (tipo)
            {
                case TipoZona.Tablero:
                    Debug.Log("Cayó en el tablero (punto básico).");
                    Destroy(other.gameObject, 0.5f);
                    break;
                case TipoZona.Centro:
                    Debug.Log("¡Cayó en el centro!");
                    break;
                case TipoZona.Papeleta:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
                case TipoZona.Papeleta1:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
                case TipoZona.Papeleta2:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
                case TipoZona.Papeleta3:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
                case TipoZona.Papeleta4:
                    Debug.Log("¡Le pegó a una papeleta con pólvora!");
                    break;
            }
        }
    }
}
