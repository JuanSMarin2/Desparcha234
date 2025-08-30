using UnityEngine;

public class CentroController : MonoBehaviour
{
    public float minX = -5f;  // l�mites del tablero en X
    public float maxX = 5f;
    public float minY = -3f;  // l�mites del tablero en Y
    public float maxY = 3f;

    public float intervalo = 2f; // cada cu�ntos segundos se mover�
    private float tiempoTranscurrido = 0f;

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;

        if (tiempoTranscurrido >= intervalo)
        {
            MoverCentro();
            tiempoTranscurrido = 0f;
        }
    }

    void MoverCentro()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        transform.position = new Vector3(randomX, randomY, transform.position.z);
    }
}
