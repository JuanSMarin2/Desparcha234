using UnityEngine;

public class CentroController : MonoBehaviour
{
    public float minX = -5f;  // límites del tablero en X
    public float maxX = 5f;
    public float minY = -3f;  // límites del tablero en Y
    public float maxY = 3f;

    // este método lo llamamos manualmente
    public void MoverCentro()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        transform.position = new Vector3(randomX, randomY, transform.position.z);
    }
}
