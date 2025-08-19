using UnityEngine;
using TMPro; // Importa el namespace de TextMeshPro

public class PowerUpTextFeedback : MonoBehaviour
{
    // Asegúrate de asignar estas referencias en el Inspector
    [SerializeField] private TMP_Text textMeshPro1;
    [SerializeField] private TMP_Text textMeshPro2;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;

    public static PowerUpTextFeedback instance;

    // Diccionario para almacenar el texto asociado a cada tipo de poder
    private System.Collections.Generic.Dictionary<MarblePowerType, string> powerUpTexts;

    private float timer = 0f;
    private bool isDisplaying = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // No destruyas el objeto si está en la escena, solo si ya hay otro
            // No necesitas DontDestroyOnLoad si solo existe en una escena
        }
        else
        {
            Destroy(gameObject);
        }

        // Inicializa el diccionario con los textos para cada poder
        powerUpTexts = new System.Collections.Generic.Dictionary<MarblePowerType, string>()
        {
            { MarblePowerType.MorePower, "¡Ahora tienes Poder Extra!" },
            { MarblePowerType.Unmovable, "¡Ahora Eres Inamovible!" },
            { MarblePowerType.Ghost, "¡Ahora Puedes atravezar paredes!" }
        };

        // Oculta los textos al inicio
        SetTextVisibility(false);
    }

    // El método 'Update' ahora maneja el temporizador para ocultar el texto
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
        if (powerUpTexts.ContainsKey(type))
        {
            string feedbackText = powerUpTexts[type];

            // Muestra el texto en los dos TextMeshPro
            textMeshPro1.text = feedbackText;
            textMeshPro2.text = feedbackText;

            SetTextVisibility(true);

            // Reinicia el temporizador
            timer = displayDuration;
            isDisplaying = true;
        }
        else
        {
            // Oculta el texto si el tipo de poder es 'None' o no está en el diccionario
            SetTextVisibility(false);
            isDisplaying = false;
        }
    }

    // Método de utilidad para ocultar/mostrar los objetos de texto
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
}