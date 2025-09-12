using UnityEngine;

public class FlechaVertical : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public Transform pivotPoint;

    private float currentAngle = 0f;

    void Update()
    {
        float verticalInput = Input.GetAxis("Vertical");
        currentAngle += verticalInput * rotationSpeed * Time.deltaTime;

        // Rotar alrededor del pivote (en este caso en el eje X para simular inclinación)
        transform.RotateAround(pivotPoint.position, transform.right, verticalInput * rotationSpeed * Time.deltaTime);
    }

    public float GetVerticalAngle()
    {
        return currentAngle;
    }
}
