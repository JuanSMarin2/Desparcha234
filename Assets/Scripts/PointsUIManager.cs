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

    [Header("Fondos base por cantidad de jugadores")]
    [SerializeField] private Sprite baseFor2;
    [SerializeField] private Sprite baseFor3;
    [SerializeField] private Sprite baseFor4;

    [Header("Fondos por jugador y cantidad de jugadores")]
    [SerializeField] private Sprite p1_For2;
    [SerializeField] private Sprite p1_For3;
    [SerializeField] private Sprite p1_For4;

    [SerializeField] private Sprite p2_For2;
    [SerializeField] private Sprite p2_For3;
    [SerializeField] private Sprite p2_For4;

    [SerializeField] private Sprite p3_For3;
    [SerializeField] private Sprite p3_For4;

    [SerializeField] private Sprite p4_Any;

    [Header("Tiempos")]
    [SerializeField] private float baseDuration = 3f;       // base inicial
    [SerializeField] private float perPlayerDuration = 3f;  // total de la etapa por jugador
    [SerializeField] private float holdBeforeAnim = 1f;     // espera antes de animar (dentro de la etapa)
    [SerializeField] private float midBaseDuration = 0.5f;  // vuelta a base entre jugadores
    [SerializeField] private float finalBaseDuration = 3f;  // base final

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

        // Ocultar extras según cantidad
        for (int i = 0; i < playerTexts.Length; i++)
        {
            bool active = (i < numPlayers);
            if (playerTexts[i]) playerTexts[i].gameObject.SetActive(active);
            if (gainTexts[i]) gainTexts[i].gameObject.SetActive(false);
        }

        // Copias locales para mostrar y animar
        int[] baseTotals = new int[numPlayers];
        int[] gains = new int[numPlayers];
        int[] targetTotals = new int[numPlayers];

        for (int i = 0; i < numPlayers; i++)
        {
            baseTotals[i] = rd.totalPoints[i];
            gains[i] = rd.currentPoints[i];
            targetTotals[i] = baseTotals[i] + gains[i];
        }

        // 1) Fondo base inicial con totales "base"
        SetBackground(BaseSpriteFor(numPlayers));
        WriteTotals(baseTotals, numPlayers);
        yield return new WaitForSeconds(baseDuration);

        // Orden: de menos puntos ganados a más
        var order = Enumerable.Range(0, numPlayers)
                              .Select(i => new { idx = i, pts = gains[i] })
                              .OrderBy(x => x.pts)
                              .ThenBy(x => x.idx)
                              .ToList();

        foreach (var entry in order)
        {
            int i = entry.idx;
            int gained = entry.pts;

            // Fondo del jugador
            SetBackground(PerPlayerSprite(i, numPlayers));

            // +N visible solo del jugador actual
            SetAllGainsActive(false);
            if (gainTexts[i])
            {
                gainTexts[i].text = gained >= 0 ? $"+{gained}" : gained.ToString();
                gainTexts[i].gameObject.SetActive(true);
            }

            // Mostrar totales actuales locales (sin tocar los demás)
            WriteTotals(baseTotals, numPlayers);

            // 2) Esperar 1s sin animar (su "momento" antes del cambio visual)
            float stage = Mathf.Max(0f, perPlayerDuration);
            float wait = Mathf.Min(holdBeforeAnim, stage);
            yield return new WaitForSeconds(wait);

            // 3) Animar SOLO el jugador i, durante el resto de su etapa
            float animDur = Mathf.Max(0f, stage - wait);
            if (animDur > 0f)
            {
                float t = 0f;
                while (t < animDur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / animDur);

                    int shown = Mathf.RoundToInt(Mathf.Lerp(baseTotals[i], targetTotals[i], k));
                    // Pintar los demas en su valor base, y el actual animado
                    for (int j = 0; j < numPlayers; j++)
                    {
                        int val = (j == i) ? shown : baseTotals[j];
                        if (playerTexts[j]) playerTexts[j].text = $"{val} pts";
                    }
                    yield return null;
                }
            }

            // Consolidar el total local del jugador i
            baseTotals[i] = targetTotals[i];

            // 4) Vuelta a base entre jugadores (0.5s)
            SetAllGainsActive(false);
            SetBackground(BaseSpriteFor(numPlayers));
            WriteTotals(baseTotals, numPlayers);
            yield return new WaitForSeconds(midBaseDuration);
        }

        // 5) Base final mostrando todos los targetTotals locales
        SetAllGainsActive(false);
        SetBackground(BaseSpriteFor(numPlayers));
        WriteTotals(targetTotals, numPlayers);

        // 6) Aplicar la suma "real" a RoundData tras 1s dentro de esta rutina
        yield return StartCoroutine(CommitTotalsAfterOneSecond(rd));

        // 7) Espera final y avanzar
        yield return new WaitForSeconds(finalBaseDuration);
        if (minigameChooser)
            minigameChooser.LoadNextScheduledOrFinish();
    }

    private IEnumerator CommitTotalsAfterOneSecond(RoundData rd)
    {
        // Llamamos a su propia rutina que ya hace WaitForSeconds(1f) y suma current → total
        rd.GetTotalPoints();
        yield return new WaitForSeconds(1f);
        // No tocamos UI aquí; ya está mostrando los targetTotals locales
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
        if (backgroundImage) backgroundImage.sprite = s;
    }

    private Sprite BaseSpriteFor(int numPlayers)
    {
        switch (numPlayers)
        {
            case 2: return baseFor2 ? baseFor2 : backgroundImage?.sprite;
            case 3: return baseFor3 ? baseFor3 : backgroundImage?.sprite;
            case 4: return baseFor4 ? baseFor4 : backgroundImage?.sprite;
            default: return backgroundImage?.sprite;
        }
    }

    private Sprite PerPlayerSprite(int playerIndex, int numPlayers)
    {
        switch (playerIndex)
        {
            case 0:
                if (numPlayers == 2) return p1_For2 ? p1_For2 : backgroundImage?.sprite;
                if (numPlayers == 3) return p1_For3 ? p1_For3 : backgroundImage?.sprite;
                return p1_For4 ? p1_For4 : backgroundImage?.sprite;

            case 1:
                if (numPlayers == 2) return p2_For2 ? p2_For2 : backgroundImage?.sprite;
                if (numPlayers == 3) return p2_For3 ? p2_For3 : backgroundImage?.sprite;
                return p2_For4 ? p2_For4 : backgroundImage?.sprite;

            case 2:
                if (numPlayers == 3) return p3_For3 ? p3_For3 : backgroundImage?.sprite;
                return p3_For4 ? p3_For4 : backgroundImage?.sprite;

            case 3:
                return p4_Any ? p4_Any : backgroundImage?.sprite;
        }
        return backgroundImage?.sprite;
    }
}
