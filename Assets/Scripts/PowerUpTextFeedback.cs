using UnityEngine;
using TMPro;

public class PowerUpTextFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text textTMP;                         // único TMP
    [SerializeField] private RectTransform[] playerAnchors = new RectTransform[4]; // anclajes UI para J1..J4
    [Header("Settings")]
    [SerializeField] private float displayDuration = 5f;

    // Colores para cada tipo de power-up
    [Header("Colors")]
    [SerializeField] private Color morePowerColor = new Color(1f, 0.5f, 0f); // Naranja
    [SerializeField] private Color unmovableColor = Color.black;             // Negro
    [SerializeField] private Color ghostColor = new Color(0.5f, 0f, 0.5f);   // Morado

    public static PowerUpTextFeedback instance;

    private System.Collections.Generic.Dictionary<MarblePowerType, (string text, Color color)> powerUpData;
    private float timer = 0f;
    private bool isDisplaying = false;

    // Rotaciones Z por jugador (index 0..3 ? Jugador 1..4)
    private readonly float[] playerAngles = new float[4] { -55f, 130f, -130f, 55f };

    void Awake()
    {
        if (instance == null) instance = this; else { Destroy(gameObject); return; }

        powerUpData = new System.Collections.Generic.Dictionary<MarblePowerType, (string, Color)>()
        {
            { MarblePowerType.MorePower, ("¡Ahora tienes más potencia!", morePowerColor) },
            { MarblePowerType.Unmovable, ("¡Ahora eres Inamovible!",    unmovableColor) },
            { MarblePowerType.Ghost,     ("¡Ahora puedes atravesar piedras!", ghostColor) }
        };

        SetVisible(false);
    }

    void Update()
    {
        if (!isDisplaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SetVisible(false);
            isDisplaying = false;
        }
    }

    // NUEVO: recibe también el playerIndex (0..3)
    public void TextFeedback(MarblePowerType type, int playerIndex)
    {
        if (!powerUpData.TryGetValue(type, out var data)) { SetVisible(false); isDisplaying = false; return; }
        if (!textTMP) return;
        if (playerIndex < 0 || playerIndex >= playerAnchors.Length) return;

        // Texto + color
        textTMP.text = data.text;
        textTMP.color = data.color;

        // Posicionar y rotar en el anclaje del jugador
        RectTransform self = textTMP.rectTransform;
        RectTransform anchor = playerAnchors[playerIndex];
        if (anchor != null && self != null)
        {
            self.position = anchor.position; // ambos deben estar en el mismo Canvas
            float z = playerAngles[playerIndex];
            self.rotation = Quaternion.Euler(0f, 0f, z);
        }

        SetVisible(true);
        timer = displayDuration;
        isDisplaying = true;
    }

    private void SetVisible(bool v)
    {
        if (textTMP) textTMP.gameObject.SetActive(v);
    }
}
