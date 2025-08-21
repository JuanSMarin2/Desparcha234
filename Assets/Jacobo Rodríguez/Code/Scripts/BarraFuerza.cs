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

    [Header("Shake (Móvil)")]
    [SerializeField] private bool usarShakeParaLanzar = true;
    [SerializeField] private float shakeThreshold = 2.2f;      // Intensidad aproximada (g) para disparar
    [SerializeField] private float shakeCooldown = 0.8f;       // Segundos entre lanzamientos por sacudida
    [SerializeField] private float lowPassFactor = 0.15f;      // 0-1 (menor = más suavizado)
    private Vector3 _lowPassAccel;
    private float _ultimoShakeTime;

    private bool subiendo = true;   // Dirección del marcador
    private bool detenido = false;  // Estado de movimiento
    private float fuerzaActual;     // Fuerza calculada en el momento del click

    private void Awake()
    {
        if (bolita == null)
            bolita = FindAnyObjectByType<Bolita>();
        _lowPassAccel = Input.acceleration; // inicial para filtro
    }

    private void Update()
    {
        if (detenido) return; // No procesar si ya se detuvo
        if (marcador == null || barra == null) return;

        MoverMarcador();
        DetectarShakeYLanzar(); // soporte móvil

        // if (Input.GetMouseButtonDown(0))
        // {
        //     Debug.Log("Click presionado");
        //     // Solo consideramos el click para lanzar si la bolita está lista para ser lanzada
        //     if (bolita != null && bolita.Estado == Bolita.EstadoLanzamiento.PendienteDeLanzar)
        //     {
        //         detenido = true; // ahora sí detenemos la barra
        //         CalcularFuerza();
        //         bolita.DarVelocidadHaciaArriba(fuerzaActual);
        //     }
        //     else
        //     {
        //         // Ignorar el click (por ejemplo fue para tocar la bolita en el aire o un jack)
        //         // Importante: NO detenemos la barra aquí
        //     }
        // }
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

    private void DetectarShakeYLanzar()
    {
        if (!usarShakeParaLanzar) return;
        if (bolita == null) return;
        if (bolita.Estado != Bolita.EstadoLanzamiento.PendienteDeLanzar) return;
        if (Time.time - _ultimoShakeTime < shakeCooldown) return;

        // Filtro pasa-bajos para separar aceleración lenta de cambios bruscos
        _lowPassAccel = Vector3.Lerp(_lowPassAccel, Input.acceleration, lowPassFactor);
        Vector3 delta = Input.acceleration - _lowPassAccel;

        float sqrMag = delta.sqrMagnitude;                // magnitud al cuadrado
        float thresholdSqr = shakeThreshold * shakeThreshold;

        if (sqrMag > thresholdSqr)
        {
            _ultimoShakeTime = Time.time;
            detenido = true;
            CalcularFuerza();
            bolita.DarVelocidadHaciaArriba(fuerzaActual);
            Debug.Log($"Shake detectado (magnitud≈{Mathf.Sqrt(sqrMag):F2}) fuerza={fuerzaActual:F1}");
        }
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
        if (marcador != null && barra != null)
            marcador.anchoredPosition = new Vector2(marcador.anchoredPosition.x, -barra.rect.height / 2f);
        subiendo = true;
        if (bolita == null) bolita = FindAnyObjectByType<Bolita>();
        if (bolita != null && bolita.Estado != Bolita.EstadoLanzamiento.EnElAire)
        {
            bolita.ReiniciarBola();
        }
        fuerzaActual = 0f; // Reiniciar fuerza
        Debug.Log("Barra de fuerza reiniciada.");
    }
}
