using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PointsUIManager : MonoBehaviour
{
    [Header("Textos de puntaje por jugador")]
    [SerializeField] private TextMeshProUGUI player1Text;
    [SerializeField] private TextMeshProUGUI player2Text;
    [SerializeField] private TextMeshProUGUI player3Text;
    [SerializeField] private TextMeshProUGUI player4Text;

    [Header("Textos +N por jugador")]
    [SerializeField] private TextMeshProUGUI gain1Text;
    [SerializeField] private TextMeshProUGUI gain2Text;
    [SerializeField] private TextMeshProUGUI gain3Text;
    [SerializeField] private TextMeshProUGUI gain4Text;

    [Header("Fondo")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite p1Sprite;
    [SerializeField] private Sprite p2Sprite;
    [SerializeField] private Sprite p3Sprite;
    [SerializeField] private Sprite p4Sprite;

    [Header("Íconos por jugador (0..3)")]
    [SerializeField] private GameObject[] icons = new GameObject[4];

    [Header("Tiempos")]
    [SerializeField] private float baseDuration = 3f;
    [SerializeField] private float perPlayerDuration = 3f;
    [SerializeField] private float holdBeforeAnim = 1f;
    [SerializeField] private float midBaseDuration = 0.5f;
    [SerializeField] private float finalBaseDuration = 3f;

    [Header("Flujo siguiente escena")]
    [SerializeField] private MinigameChooser minigameChooser;

    private TextMeshProUGUI[] playerTexts;
    private TextMeshProUGUI[] gainTexts;

    void Awake()
    {
        playerTexts = new[] { player1Text, player2Text, player3Text, player4Text };
        gainTexts = new[] { gain1Text, gain2Text, gain3Text, gain4Text };
    }

    void Start()
    {
        StartCoroutine(FlowRoutine());
    }

    private IEnumerator FlowRoutine()
    {
        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.currentPoints == null)
            yield break;

        int numPlayers = Mathf.Clamp(rd.numPlayers, 2, 4);

        // Ocultar textos extra y limpiar +N
        for (int i = 0; i < playerTexts.Length; i++)
        {
            bool active = (i < numPlayers);
            if (playerTexts[i]) playerTexts[i].gameObject.SetActive(active);
            if (gainTexts[i]) gainTexts[i].gameObject.SetActive(false);
        }

        // Copias locales
        int[] baseTotals = new int[numPlayers];
        int[] gains = new int[numPlayers];
        int[] targetTotals = new int[numPlayers];

        for (int i = 0; i < numPlayers; i++)
        {
            baseTotals[i] = rd.totalPoints[i];
            gains[i] = rd.currentPoints[i];
            targetTotals[i] = baseTotals[i] + gains[i];
        }

        // Estado neutro inicial
        SetBackground(baseSprite);
        SetIconsNeutral(numPlayers);
        WriteTotals(baseTotals, numPlayers);
        yield return new WaitForSeconds(baseDuration);

        // Orden por menos puntos ganados
        var order = Enumerable.Range(0, numPlayers)
                              .Select(i => new { idx = i, pts = gains[i] })
                              .OrderBy(x => x.pts)
                              .ThenBy(x => x.idx)
                              .ToList();

        foreach (var entry in order)
        {
            int i = entry.idx;
            int gained = entry.pts;

            // Fondo e ícono del jugador
            SetBackground(PerPlayerSprite(i));
            SetIconsOnly(i, numPlayers);

            // Mostrar +N del jugador i
            SetAllGainsActive(false);
            if (gainTexts[i])
            {
                gainTexts[i].text = gained >= 0 ? $"+{gained}" : gained.ToString();
                gainTexts[i].gameObject.SetActive(true);
            }

            // Escribir totales actuales locales
            WriteTotals(baseTotals, numPlayers);

            // Espera previa a la animación en su etapa
            float stageTotal = Mathf.Max(0f, perPlayerDuration);
            float wait = Mathf.Min(holdBeforeAnim, stageTotal);
            yield return new WaitForSeconds(wait);

            // Animar solo el total del jugador i
            float animDur = Mathf.Max(0f, stageTotal - wait);
            if (animDur > 0f)
            {
                float t = 0f;
                while (t < animDur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / animDur);

                    int shown = Mathf.RoundToInt(Mathf.Lerp(baseTotals[i], targetTotals[i], k));

                    for (int j = 0; j < numPlayers; j++)
                    {
                        int val = (j == i) ? shown : baseTotals[j];
                        if (playerTexts[j]) playerTexts[j].text = $"{val} pts";
                    }
                    yield return null;
                }
            }

            // Consolidar localmente
            baseTotals[i] = targetTotals[i];

            // Vuelta a neutro entre jugadores
            SetAllGainsActive(false);
            SetBackground(baseSprite);
            SetIconsNeutral(numPlayers);
            WriteTotals(baseTotals, numPlayers);
            yield return new WaitForSeconds(midBaseDuration);
        }

        // Base final con totales finales y neutro
        SetAllGainsActive(false);
        SetBackground(baseSprite);
        SetIconsNeutral(numPlayers);
        WriteTotals(targetTotals, numPlayers);

        // Aplicar la suma real a RoundData dentro de esta rutina (y esperar 1s)
        yield return StartCoroutine(CommitTotalsAfterOneSecond());

        // Espera final y pasar a siguiente
        yield return new WaitForSeconds(finalBaseDuration);
        if (minigameChooser)
            minigameChooser.LoadNextScheduledOrFinish();
    }

    private IEnumerator CommitTotalsAfterOneSecond()
    {
        var rd = RoundData.instance;
        if (rd != null)
        {
            rd.GetTotalPoints(); // esta corrutina ya espera 1s internamente
            yield return new WaitForSeconds(1f);
        }
    }

    private void WriteTotals(int[] totals, int numPlayers)
    {
        for (int i = 0; i < numPlayers; i++)
        {
            if (playerTexts[i])
            {
                playerTexts[i].text = $"{totals[i]} pts";
                playerTexts[i].gameObject.SetActive(true);
            }
        }
    }

    private void SetAllGainsActive(bool active)
    {
        foreach (var t in gainTexts)
            if (t) t.gameObject.SetActive(active);
    }

    private void SetBackground(Sprite s)
    {
        if (backgroundImage && s) backgroundImage.sprite = s;
    }

    private Sprite PerPlayerSprite(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0: return p1Sprite ? p1Sprite : baseSprite;
            case 1: return p2Sprite ? p2Sprite : baseSprite;
            case 2: return p3Sprite ? p3Sprite : baseSprite;
            case 3: return p4Sprite ? p4Sprite : baseSprite;
            default: return baseSprite;
        }
    }

    // Activa solo el ícono del jugador indicado; desactiva el resto
    private void SetIconsOnly(int playerIndex, int numPlayers)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (!icons[i]) continue;
            bool shouldBeActive = (i == playerIndex) && (i < numPlayers);
            icons[i].SetActive(shouldBeActive);
        }
    }

    // Estado neutro: activa todos los íconos de jugadores participantes; desactiva el resto
    private void SetIconsNeutral(int numPlayers)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (!icons[i]) continue;
            bool shouldBeActive = (i < numPlayers);
            icons[i].SetActive(shouldBeActive);
        }
    }
}
