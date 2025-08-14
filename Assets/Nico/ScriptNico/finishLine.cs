using UnityEngine;

public class finishLine : MonoBehaviour
{
    public ZancoMove z1;
    public ZancoMove z2;
    public ZancoMove z3;
    public ZancoMove z4;
    public GameRoundManager gameManager;
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Objeto toco");
        if (collision.CompareTag("Player_1"))
        {
            gameManager.PlayerWin(0);
            z1.LlegarMeta();
            Debug.Log("Llegó el jugador 1");
        }
        else if (collision.CompareTag("Player_2"))
        {
            gameManager.PlayerWin(1);
            z2.LlegarMeta();
            Debug.Log("Llegó el jugador 2");
        }
        else if (collision.CompareTag("Player_3"))
        {
            gameManager.PlayerWin(2);
            z3.LlegarMeta();
            Debug.Log("Llegó el jugador 3");
        }
        else if (collision.CompareTag("Player_4"))
        {
            gameManager.PlayerWin(3);
            z4.LlegarMeta();
            Debug.Log("Llegó el jugador 4");
        }
    }
}
