using UnityEngine;
using TMPro;

public class PowerUpTextFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text textMeshPro1;
    [SerializeField] private TMP_Text textMeshPro2;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;

    // Colores para cada tipo de power-up
    [Header("Colors")]
    [SerializeField] private Color morePowerColor = new Color(1f, 0.5f, 0f); // Naranja
    [SerializeField] private Color unmovableColor = Color.black; // Negro
    [SerializeField] private Color ghostColor = new Color(0.5f, 0f, 0.5f); // Morado

    public static PowerUpTextFeedback instance;

    private System.Collections.Generic.Dictionary<MarblePowerType, (string text, Color color)> powerUpData;
    private float timer = 0f;
    private bool isDisplaying = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Inicializa el diccionario con texto y color para cada poder
        powerUpData = new System.Collections.Generic.Dictionary<MarblePowerType, (string, Color)>()
        {
            { MarblePowerType.MorePower, ("¡Ahora tienes más potencia!", morePowerColor) },
            { MarblePowerType.Unmovable, ("¡Ahora Eres Inamovible!", unmovableColor) },
            { MarblePowerType.Ghost, ("¡Ahora Puedes atravesar paredes!", ghostColor) }
        };

        SetTextVisibility(false);
    }

    void Update()
    {
        if (isDisplaying)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SetTextVisibility(false);
                isDisplaying = false;
            }
        }
    }

    public void TextFeedback(MarblePowerType type)
    {
        if (powerUpData.ContainsKey(type))
        {
            var (feedbackText, textColor) = powerUpData[type];

            // Configura texto y color para ambos TextMeshPro
            SetTextProperties(textMeshPro1, feedbackText, textColor);
            SetTextProperties(textMeshPro2, feedbackText, textColor);

            SetTextVisibility(true);

            timer = displayDuration;
            isDisplaying = true;
        }
        else
        {
            SetTextVisibility(false);
            isDisplaying = false;
        }
    }

    private void SetTextProperties(TMP_Text textComponent, string text, Color color)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
        }
    }

    private void SetTextVisibility(bool isVisible)
    {
        if (textMeshPro1 != null)
        {
            textMeshPro1.gameObject.SetActive(isVisible);
        }
        if (textMeshPro2 != null)
        {
            textMeshPro2.gameObject.SetActive(isVisible);
        }
    }

    // Método opcional para cambiar colores en tiempo de ejecución
    public void SetPowerUpColor(MarblePowerType type, Color newColor)
    {
        if (powerUpData.ContainsKey(type))
        {
            var currentData = powerUpData[type];
            powerUpData[type] = (currentData.text, newColor);
        }
    }
}