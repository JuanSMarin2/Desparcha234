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

        if (finished) return;

        if (state)
            innerCircle.color = new Color(0.6f, 0.6f, 0.6f); // gris apagado inicial (o color activo si prefieres)
        else
            innerCircle.color = new Color(0.3f, 0.3f, 0.3f); // gris oscuro para pausados
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

        float diff = Mathf.Abs(outerRadius - innerRadius);
        float hitThreshold = innerRadius * hitMarginPercent;

        if (diff <= hitThreshold && outerRadius >= innerRadius)
            Success();
        else
            Fail();
    }

    private void Success()
    {
        finished = true;
        innerCircle.color = Color.green;
        Debug.Log($"✔ Acierto en círculo {orderIndex + 1}");
        active = false;
        manager.OnCircleResult(this, true);
    }

    private void Fail()
    {
        finished = true;
        innerCircle.color = Color.red;
        Debug.Log($"❌ Fallo en círculo {orderIndex + 1}");
        active = false;
        manager.OnCircleResult(this, false);
    }

    public void ResetForRetry()
    {
        finished = false;
        startSize = Random.Range(220f, 300f);
        targetSize = Random.Range(70f, 90f);
        currentSize = startSize;
        outerRT.sizeDelta = new Vector2(currentSize, currentSize);
        innerCircle.color = new Color(0.3f, 0.3f, 0.3f); // gris oscuro
        active = false;
    }
}
