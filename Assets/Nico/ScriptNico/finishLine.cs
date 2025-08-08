using UnityEngine;

public class finishLine : MonoBehaviour
{
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Objeto toco");
        if (collision.CompareTag("Player_1"))
        {
            Debug.Log("Llegó el jugador 1");
        }
        else if (collision.CompareTag("Player_2"))
        {
            Debug.Log("Llegó el jugador 2");
        }
        else if (collision.CompareTag("Player_3"))
        {
            Debug.Log("Llegó el jugador 3");
        }
        else if (collision.CompareTag("Player_4"))
        {
            Debug.Log("Llegó el jugador 4");
        }
    }
}
