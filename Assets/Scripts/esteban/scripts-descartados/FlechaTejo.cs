using UnityEngine;

public class FlechaTejo : MonoBehaviour
{
    public enum RotationMode { Horizontal, Vertical }
    public RotationMode rotationMode = RotationMode.Horizontal;

    public Transform pivotPoint;     // Pivote de rotaci�n
    public float rotationSpeed = 50f; // Velocidad de rotaci�n
    public float maxAngle = 70f;      // L�mite m�ximo
    public float minAngle = -70f;     // L�mite m�nimo

    private float currentAngle;
    private int rotationDirection = 1; // 1 = hacia +, -1 = hacia -

    void Start()
    {
        currentAngle = 0f;
    }

    void Update()
    {
        // Calculamos cu�nto rotar este frame
        float deltaRotation = rotationSpeed * Time.deltaTime * rotationDirection;
        currentAngle += deltaRotation;

        // Definimos el eje de rotaci�n seg�n el modo
        Vector3 rotationAxis = (rotationMode == RotationMode.Horizontal) ? Vector3.forward : transform.right;

        // Rotamos alrededor del pivote
        transform.RotateAround(pivotPoint.position, rotationAxis, deltaRotation);

        // Cambiamos de direcci�n si pasamos los l�mites
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
