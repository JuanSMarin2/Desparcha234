using UnityEngine;
using UnityEngine.UI;

public class BarraDeFuerza : MonoBehaviour
{
    [Header("Config Barra de Fuerza")]
    public Image barraPoder; // Asigna aqu� la imagen 'BarraFuerzaPoder'
    public float velocidadSubida = 1.5f;
    public float velocidadBajada = 0.5f;

    private float valorActual = 0f;
    private bool subiendo = true;

    void Update()
    {
        if (subiendo)
        {
            valorActual += velocidadSubida * Time.deltaTime;
            if (valorActual >= 1f)
            {
                valorActual = 1f;
                subiendo = false;
            }
        }
        else
        {
            valorActual -= velocidadBajada * Time.deltaTime;
            if (valorActual <= 0f)
            {
                valorActual = 0f;
                subiendo = true;
            }
        }

        barraPoder.fillAmount = valorActual;
    }

    // M�todo p�blico para obtener el valor de la barra cuando presiones un bot�n
    public float GetValorFuerza()
    {
        return valorActual;
    }
}
