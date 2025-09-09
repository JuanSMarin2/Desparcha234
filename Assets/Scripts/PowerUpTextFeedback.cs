using UnityEngine;
using TMPro;

public class PowerUpTextFeedback : MonoBehaviour
{
    [Header("Textos por jugador (0..3)")]
    [SerializeField] private TMP_Text player1Text;
    [SerializeField] private TMP_Text player2Text;
    [SerializeField] private TMP_Text player3Text;
    [SerializeField] private TMP_Text player4Text;

    [Header("Duracion")]
    [SerializeField] private float displayDuration = 5f;

    [Header("Colores del power up para el que lo obtiene")]
    [SerializeField] private Color morePowerColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color unmovableColor = Color.gray;
    [SerializeField] private Color ghostColor = new Color(0.5f, 0f, 0.5f);

    [Header("Color para los demas jugadores")]
    [SerializeField] private Color othersTextColor = Color.black;

    public static PowerUpTextFeedback instance;

    private TMP_Text[] texts;
    private float timer;
    private bool showing;

    private readonly string[] playerColorNames = { "Rojo", "Azul", "Amarillo", "Verde" };

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        texts = new TMP_Text[4] { player1Text, player2Text, player3Text, player4Text };
        SetAllActive(false);
    }

    void Update()
    {
        if (!showing) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SetAllActive(false);
            showing = false;
        }
    }

    // Llamar con type y el index del jugador que obtuvo el power up (0..3)
    public void TextFeedback(MarblePowerType type, int winnerIndex)
    {
        if (winnerIndex < 0 || winnerIndex >= texts.Length) return;

        // Mensaje propio y color propio
        string ownText;
        Color ownColor;
        GetOwnMessageAndColor(type, out ownText, out ownColor);

        // Mensaje para los demas
        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (!t) continue;

            if (i == winnerIndex)
            {
                t.text = ownText;
                t.color = ownColor;
            }
            else
            {
                string colorName = playerColorNames[winnerIndex];
                t.text = GetOthersMessage(type, colorName);
                t.color = othersTextColor;
            }
        }

        SetAllActive(true);
        timer = displayDuration;
        showing = true;
    }

    private void GetOwnMessageAndColor(MarblePowerType type, out string msg, out Color color)
    {
        switch (type)
        {
            case MarblePowerType.MorePower:
                msg = "Ahora tienes mas potencia";
                color = morePowerColor;
                break;
            case MarblePowerType.Unmovable:
                msg = "Ahora eres inamovible";
                color = unmovableColor;
                break;
            case MarblePowerType.Ghost:
                msg = "Ahora puedes atravesar piedras";
                color = ghostColor;
                break;
            default:
                msg = "";
                color = Color.white;
                break;
        }
    }

    private string GetOthersMessage(MarblePowerType type, string winnerColorName)
    {
        switch (type)
        {
            case MarblePowerType.MorePower: return "Ahora " + winnerColorName + " tiene mas potencia";
            case MarblePowerType.Unmovable: return "Ahora " + winnerColorName + " es inamovible";
            case MarblePowerType.Ghost: return "Ahora " + winnerColorName + " puede atravesar piedras";
            default: return "";
        }
    }

    private void SetAllActive(bool v)
    {
        foreach (var t in texts) if (t) t.gameObject.SetActive(v);
    }
}
