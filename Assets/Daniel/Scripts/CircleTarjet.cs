using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircleTarget : MonoBehaviour
{
    [Header("Referencias")]
    public Image innerCircle;
    public Image outerRing;
    public TextMeshProUGUI orderText; // número dentro del círculo

    [Header("Configuración del juego")]
    [Tooltip("Velocidad con que se cierra la circunferencia.")]
    public float shrinkSpeed = 1f;

    [Tooltip("Margen de acierto (en porcentaje del radio interno). Más bajo = más difícil.")]
    [Range(0.01f, 0.3f)]
    public float hitMarginPercent = 0.08f;

    [Header("Ventana de acierto avanzada")]
    [Tooltip("Porcentaje del radio interno que permite un acierto ANTES de la posición ideal (ej. 0.05 = 5% del radio).")]
    [Range(0f, 0.5f)]
    public float hitWindowBeforePercent = 0.05f;

    [Tooltip("Porcentaje del radio interno que permite un acierto DESPUÉS de la posición ideal (ej. 0.05 = 5% del radio).")]
    [Range(0f, 0.5f)]
    public float hitWindowAfterPercent = 0.05f;

    private RectTransform innerRT;
    private RectTransform outerRT;
    private bool active = false;
    private bool finished = false;

    private float startSize;
    private float targetSize;
    private float currentSize;

    private CircleGameManagerUI manager;

    public int orderIndex { get; private set; }

    public void Initialize(CircleGameManagerUI mgr, int index)
    {
        manager = mgr;
        orderIndex = index;
        orderText.text = (index + 1).ToString();

        innerRT = innerCircle.GetComponent<RectTransform>();
        outerRT = outerRing.GetComponent<RectTransform>();

        startSize = Random.Range(190f, 230f);
        targetSize = Random.Range(70f, 90f);
        currentSize = startSize;

        outerRT.sizeDelta = new Vector2(currentSize, currentSize);
        SetActiveState(false);
    }

    public void SetActiveState(bool state)
    {
        active = state;

        // No sobrescribir colores finales (rojo/verde) cuando ya terminó
        if (finished) return;

        if (state)
            innerCircle.color = new Color(0.6f, 0.6f, 0.6f); // gris activo
        else
            innerCircle.color = new Color(0.3f, 0.3f, 0.3f); // gris inactivo
    }

    void Update()
    {
        if (!active || finished) return;

        currentSize = Mathf.MoveTowards(currentSize, targetSize, shrinkSpeed * Time.deltaTime * 100f);
        outerRT.sizeDelta = new Vector2(currentSize, currentSize);

        if (currentSize <= targetSize + 0.5f)
        {
            Fail();
        }
    }

    public void OnClick()
    {
        if (!active || finished) return;

        float innerRadius = innerRT.sizeDelta.x / 2f;
        float outerRadius = outerRT.sizeDelta.x / 2f;

        // Distancia entre el borde externo y el radio interno en valor absoluto
        float diff = outerRadius - innerRadius; // positivo si el anillo está por fuera del inner

        // Si no se usan las ventanas avanzadas, mantener compatibilidad con hitMarginPercent
        if (Mathf.Approximately(hitWindowBeforePercent, 0f) && Mathf.Approximately(hitWindowAfterPercent, 0f))
        {
            float hitThreshold = innerRadius * hitMarginPercent;
            if (Mathf.Abs(diff) <= hitThreshold && outerRadius >= innerRadius)
                Success();
            else
                Fail();
            return;
        }

        // Ventana antes: permite aciertos cuando el anillo aún está algo más grande que el inner (anticipación)
        float beforeThreshold = innerRadius * hitWindowBeforePercent; // cuánto 'por encima' del innerRadius se acepta
        // Ventana después: permite aciertos cuando el anillo ya se ha hecho más pequeño que el inner (retardo)
        float afterThreshold = innerRadius * hitWindowAfterPercent; // cuánto 'por debajo' del innerRadius se acepta

        // Caso de éxito: si diff está dentro de [-afterThreshold, +beforeThreshold]
        // (diff negativo => outerRadius < innerRadius => ya pasó el objetivo)
        if (diff <= beforeThreshold && diff >= -afterThreshold)
        {
            Success();
        }
        else
        {
            Fail();
        }
    }

    private void Success()
    {
        finished = true;
        innerCircle.color = Color.green;
    // acierto: feedback visual verde ya aplicado
        active = false;
        // Sonido de acierto
        if (manager != null) manager.PlayResultSfx(true);
        manager.OnCircleResult(this, true);
    }

    private void Fail()
    {
        finished = true;
        innerCircle.color = Color.red;
    // fallo: feedback visual rojo ya aplicado
        active = false;
        // Sonido de fallo
        if (manager != null) manager.PlayResultSfx(false);
        manager.OnCircleResult(this, false);
    }

    public void ResetForRetry()
    {
        finished = false;
        startSize = Random.Range(220f, 300f);
        targetSize = Random.Range(70f, 90f);
        currentSize = startSize;
        outerRT.sizeDelta = new Vector2(currentSize, currentSize);
        innerCircle.color = new Color(0.3f, 0.3f, 0.3f); // gris inactivo
        active = false;
    }
}
