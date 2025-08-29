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
                    Debug.Log("Cay� en el tablero (punto b�sico).");
                    break;
                case TipoZona.Centro:
                    Debug.Log("�Cay� en el centro!");
                    break;
                case TipoZona.Papeleta:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
            }
        }
    }
}
