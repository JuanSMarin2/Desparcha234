using UnityEngine;

public class finishLine : MonoBehaviour
{
    public ZancoMove z1;
    public ZancoMove z2;
    public ZancoMove z3;
    public ZancoMove z4;
    public GameRoundManager gameManager;
    [SerializeField] AudioManagerSacos ams;
    private System.Collections.IEnumerator HandlePlayerWin(int playerIndex)
    {
        Debug.Log("entra a la corrutina");
        yield return new WaitForSeconds(3f);
        gameManager.PlayerWin(playerIndex);
        Debug.Log("Se registra en el manager");
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Objeto toco");
        if (collision.CompareTag("Player_1"))
        {
            StartCoroutine(HandlePlayerWin(0));
            
            z1.LlegarMeta();
            ams.PlaySFX(ams.Ganar);
            Debug.Log("Llegó el jugador 1");
        }
        else if (collision.CompareTag("Player_2"))
        {
            StartCoroutine(HandlePlayerWin(1));
            
            z2.LlegarMeta();
            ams.PlaySFX(ams.Ganar);
            Debug.Log("Llegó el jugador 2");
        }
        else if (collision.CompareTag("Player_3"))
        {
            StartCoroutine(HandlePlayerWin(2));
            
            z3.LlegarMeta();
            ams.PlaySFX(ams.Ganar);
            Debug.Log("Llegó el jugador 3");
        }
        else if (collision.CompareTag("Player_4"))
        {
            StartCoroutine(HandlePlayerWin(3));
            
            z4.LlegarMeta();
            ams.PlaySFX(ams.Ganar);
            Debug.Log("Llegó el jugador 4");
        }
    }
}
