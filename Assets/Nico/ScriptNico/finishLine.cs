using UnityEngine;

public class finishLine : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (CompareTag("Player_1"))
        {
            Debug.Log("Llegó el jugador 1");
        }
        else if (CompareTag("Player_2"))
        {
            Debug.Log("Llegó el jugador 2");
        }
        else if (CompareTag("Player_3"))
        {
            Debug.Log("Llegó el jugador 3");
        }
        else if (CompareTag("Player_4"))
        {
            Debug.Log("Llegó el jugador 4");
        }
    }
}
