using UnityEngine;
using TMPro;

public class FlechaUI : MonoBehaviour
{
    public FlechaTejo flecha; // Referencia a la flecha
    public TMP_Text angleText;

    void Update()
    {
        if (flecha != null && angleText != null)
        {
            float angulo = flecha.GetAngle();
            angleText.text = $"Ángulo: {angulo:F1}°";
        }
    }
}
