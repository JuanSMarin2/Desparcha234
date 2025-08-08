using UnityEngine;

public class BarraFuerza : MonoBehaviour
{
[Header("Referencias UI")]
    [SerializeField] private RectTransform marcador; // Objeto que se mueve
    [SerializeField] private RectTransform barra;    // Barra con gradiente

    [Header("Configuración")]
    [SerializeField] private float velocidad = 200f;   // Velocidad de movimiento (px/seg)
    [SerializeField] private float fuerzaMaxima = 100f; // Fuerza máxima posible

    [Header("Objetivo")]
    [SerializeField] private Bolita bolita; // Referencia a la bolita con Rigidbody2D

    private bool subiendo = true;   // Dirección del marcador
    private bool detenido = false;  // Estado de movimiento
    private float fuerzaActual;     // Fuerza calculada en el momento del click

    private void Update()
    {
        if (detenido) return; // No procesar si ya se detuvo

        MoverMarcador();

        // Detectar click para detener y calcular fuerza
        if (Input.GetMouseButtonDown(0))
        {
            detenido = true;
            CalcularFuerza();
            Debug.Log($"Fuerza calculada: {fuerzaActual}");
            if (bolita != null)
            {
                bolita.DarVelocidadHaciaAbajo(fuerzaActual);
            }
            // Aquí puedes llamar al método de lanzamiento con fuerzaActual
        }
    }

    /// Mueve el marcador arriba y abajo dentro de los límites de la barra.
    private void MoverMarcador()
    {
        float paso = velocidad * Time.deltaTime;
        Vector2 nuevaPos = marcador.anchoredPosition;

        nuevaPos.y += subiendo ? paso : -paso;

        // Verificar límites superior e inferior
        float limiteSuperior = barra.rect.height / 2f;
        float limiteInferior = -barra.rect.height / 2f;

        if (nuevaPos.y >= limiteSuperior)
        {
            nuevaPos.y = limiteSuperior;
            subiendo = false;
        }
        else if (nuevaPos.y <= limiteInferior)
        {
            nuevaPos.y = limiteInferior;
            subiendo = true;
        }

        marcador.anchoredPosition = nuevaPos;
    }

    /// Calcula la fuerza en base a la altura del marcador (0 a fuerzaMaxima).
    private void CalcularFuerza()
    {
        float alturaNormalizada = (marcador.anchoredPosition.y + barra.rect.height / 2f) / barra.rect.height;
        fuerzaActual = alturaNormalizada * fuerzaMaxima;
    }

   
    /// Devuelve la última fuerza calculada.
    
    public float ObtenerFuerza()
    {
        return fuerzaActual;
    }

    /// Reinicia el marcador para un nuevo intento.
    
    public void Reiniciar()
    {
        detenido = false;
        marcador.anchoredPosition = new Vector2(marcador.anchoredPosition.x, -barra.rect.height / 2f);
        subiendo = true;
    }
}
