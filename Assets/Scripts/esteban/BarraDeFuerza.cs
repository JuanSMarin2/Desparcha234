using UnityEngine;
using UnityEngine.UI;

public class BarraDeFuerza : MonoBehaviour
{
    [Header("Config Barra de Fuerza")]
    public Image barraPoder; // Asigna aqu� la imagen 'BarraFuerzaPoder'
    public float velocidadSubida = 1.5f;
    public float velocidadBajada = 0.5f;

    [Header("Colores por turno")]
    public Color[] coloresPorTurno; // Asigna los colores en e

    private float valorActual = 0f;
    private bool subiendo = true;

    void Update()
    {
        // Cambiar color según el turno actual
        CambiarColorPorTurno();

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

    private void CambiarColorPorTurno()
    {
        int turnoActual = 0;
        if (TurnManager.instance != null)
            turnoActual = TurnManager.instance.CurrentTurn() - 1; // 0-based

        

        if (coloresPorTurno != null && coloresPorTurno.Length > turnoActual)
            barraPoder.color = coloresPorTurno[turnoActual];
        else
            barraPoder.color = Color.red;
    }

    // M�todo p�blico para obtener el valor de la barra cuando presiones un bot�n
    public float GetValorFuerza()
    {
        return valorActual;
    }
}
