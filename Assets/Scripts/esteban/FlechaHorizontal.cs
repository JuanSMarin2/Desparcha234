using UnityEngine;

public class FlechaHorizontal : MonoBehaviour
{
    public float rotationSpeed = 100f; // Velocidad de rotación
    public Transform pivotPoint; // Pivote desde donde rota la flecha

    private float currentAngle = 0f;

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // Flechas teclado o joystick
        currentAngle += horizontalInput * rotationSpeed * Time.deltaTime;

        // Rotar alrededor del pivote
        transform.RotateAround(pivotPoint.position, Vector3.forward, -horizontalInput * rotationSpeed * Time.deltaTime);
    }

    public float GetHorizontalAngle()
    {
        return currentAngle;
    }
}
