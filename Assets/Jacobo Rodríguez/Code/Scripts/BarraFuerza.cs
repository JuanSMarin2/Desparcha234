using UnityEngine;

public class BarraFuerza : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private RectTransform marcador; // Objeto que se mueve
    [SerializeField] private RectTransform barra;    // Barra con gradiente

    [Header("Configuración")]
    [SerializeField] private float velocidad = 200f;   // Velocidad base (px/seg)
    [SerializeField] private float fuerzaMaxima = 100f; // Fuerza máxima posible
    [Tooltip("Fracción mínima de la velocidad (0-1) para que nunca se detenga del todo")] 
    [SerializeField] private float factorMinVelocidad = 0.2f; 
    [Tooltip("Mayor que 1 para que suba lento y luego rápido; y baje rápido y luego lento")]
    [SerializeField] private float exponenteAceleracion = 2f;

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
                bolita.DarVelocidadHaciaArriba(fuerzaActual);
            }
            // Aquí puedes llamar al método de lanzamiento con fuerzaActual
        }
    }

    /// Mueve el marcador arriba y abajo dentro de los límites de la barra.
    private void MoverMarcador()
    {
        // Verificar límites superior e inferior
        float limiteSuperior = barra.rect.height / 2f;
        float limiteInferior = -barra.rect.height / 2f;

        Vector2 nuevaPos = marcador.anchoredPosition;

        // Normaliza la altura actual a [0,1] (0 = abajo, 1 = arriba)
        float yNorm = Mathf.InverseLerp(limiteInferior, limiteSuperior, nuevaPos.y);

        // Calcula un factor de velocidad no lineal para pasar más tiempo abajo SOLO al subir.
        // Al bajar, mantenemos velocidad constante para que no desacelere.
        float baseFactor = Mathf.Clamp01(factorMinVelocidad);
        float accel = Mathf.Max(1f, exponenteAceleracion);
        float curvaSubida = Mathf.Pow(yNorm, accel);
        float factor = subiendo ? Mathf.Lerp(baseFactor, 1f, curvaSubida) : 1f;

        float paso = velocidad * factor * Time.deltaTime;
        nuevaPos.y += subiendo ? paso : -paso;

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
        Bolita bolita = FindAnyObjectByType<Bolita>(); //OJO CON ESTO
        if (bolita != null)
        {
            bolita.ReiniciarTurno(); // Reinicia la bolita al inicio del turno
        }
        Debug.Log("Barra de fuerza reiniciada.");
    }
}
