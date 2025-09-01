using UnityEngine;

public class FlechaTejo : MonoBehaviour
{
    public enum RotationMode { Horizontal, Vertical }
    public RotationMode rotationMode = RotationMode.Horizontal;

    public Transform pivotPoint;     // Pivote de rotación
    public float rotationSpeed = 50f; // Velocidad de rotación
    public float maxAngle = 70f;      // Límite máximo
    public float minAngle = -70f;     // Límite mínimo

    private float currentAngle;
    private int rotationDirection = 1; // 1 = hacia +, -1 = hacia -

    void Start()
    {
        currentAngle = 0f;
    }

    void Update()
    {
        // Calculamos cuánto rotar este frame
        float deltaRotation = rotationSpeed * Time.deltaTime * rotationDirection;
        currentAngle += deltaRotation;

        // Definimos el eje de rotación según el modo
        Vector3 rotationAxis = (rotationMode == RotationMode.Horizontal) ? Vector3.forward : transform.right;

        // Rotamos alrededor del pivote
        transform.RotateAround(pivotPoint.position, rotationAxis, deltaRotation);

        // Cambiamos de dirección si pasamos los límites
        if (currentAngle >= maxAngle)
        {
            rotationDirection = -1;
            currentAngle = maxAngle;
        }
        else if (currentAngle <= minAngle)
        {
            rotationDirection = 1;
            currentAngle = minAngle;
        }
    }

    public float GetAngle()
    {
        return currentAngle;
    }
}
