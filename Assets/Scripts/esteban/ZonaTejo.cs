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
                    Debug.Log("Cay� en el tablero (punto b�sico).");
                    Destroy(other.gameObject, 0.5f);
                    break;
                case TipoZona.Centro:
                    Debug.Log("�Cay� en el centro!");
                    break;
                case TipoZona.Papeleta:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
                case TipoZona.Papeleta1:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
                case TipoZona.Papeleta2:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
                case TipoZona.Papeleta3:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
                case TipoZona.Papeleta4:
                    Debug.Log("�Le peg� a una papeleta con p�lvora!");
                    break;
            }
        }
    }
}
