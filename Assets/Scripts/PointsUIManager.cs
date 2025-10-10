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

    [Header("Cuadrantes (1 objeto por jugador)")]
    [SerializeField] private RectTransform quadP1; // rojo (abajo-izq)
    [SerializeField] private RectTransform quadP2; // azul (arriba-der)
    [SerializeField] private RectTransform quadP3; // amarillo (arriba-izq)
    [SerializeField] private RectTransform quadP4; // verde (abajo-der)

    [Header("Colores")]
    [SerializeField] private Image quadP1Img;
    [SerializeField] private Image quadP2Img;
    [SerializeField] private Image quadP3Img;
    [SerializeField] private Image quadP4Img;

    [SerializeField] private Color p1Color = Color.red;
    [SerializeField] private Color p2Color = Color.blue;
    [SerializeField] private Color p3Color = Color.yellow;
    [SerializeField] private Color p4Color = Color.green;

    [Header("Tiempos")]
    [SerializeField] private float baseDuration = 1.0f;      // estado base inicial
    [SerializeField] private float growDuration = 0.30f;     // crece a 1.5x
    [SerializeField] private float holdDuration = 1.0f;      // ventana para sumar puntos
    [SerializeField] private float shrinkDuration = 0.30f;   // vuelve a 1x
    [SerializeField] private float midBaseDuration = 0.50f;  // base entre jugadores
    [SerializeField] private float finalBaseDuration = 2.0f; // base final

    [Header("Flujo siguiente escena")]
    [SerializeField] private MinigameChooser minigameChooser;

    private TextMeshProUGUI[] playerTexts;
    private TextMeshProUGUI[] gainTexts;
    private RectTransform[] quads;
    private Image[] quadImages;

    private Vector3 one = Vector3.one;
    private int[] originalSiblings;

    void Awake()
    {
        playerTexts = new[] { player1Text, player2Text, player3Text, player4Text };
        gainTexts = new[] { gain1Text, gain2Text, gain3Text, gain4Text };
        quads = new[] { quadP1, quadP2, quadP3, quadP4 };
        quadImages = new[] { quadP1Img, quadP2Img, quadP3Img, quadP4Img };
    }

    void Start()
    {
        // Colores
        if (quadP1Img) quadP1Img.color = p1Color;
        if (quadP2Img) quadP2Img.color = p2Color;
        if (quadP3Img) quadP3Img.color = p3Color;
        if (quadP4Img) quadP4Img.color = p4Color;

        // Cuadrantes
        SetupQuadrants();

        // Orden original
        originalSiblings = new int[quads.Length];
        for (int i = 0; i < quads.Length; i++)
            if (quads[i]) originalSiblings[i] = quads[i].GetSiblingIndex();

        StartCoroutine(FlowRoutine());
    }

    private void SetupQuadrants()
    {
        SetupQuad(quadP1, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f)); // abajo-izq
        SetupQuad(quadP2, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f));     // arriba-der
        SetupQuad(quadP3, new Vector2(0f, 0.5f), new Vector2(0.5f, 1f));     // arriba-izq
        SetupQuad(quadP4, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f));     // abajo-der
    }

    private void SetupQuad(RectTransform rt, Vector2 min, Vector2 max)
    {
        if (!rt) return;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = one;
        rt.localRotation = Quaternion.identity;
    }

    private IEnumerator FlowRoutine()
    {
        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.currentPoints == null)
            yield break;

        int numPlayers = Mathf.Clamp(rd.numPlayers, 2, 4);

        // Ocultar +N extra
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

        // Estado base
        WriteTotals(baseTotals, numPlayers);
        ResetAllQuadScales();
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

            SetAllGainsActive(false);
            if (gainTexts[i])
            {
                gainTexts[i].text = (gained >= 0) ? $"+{gained}" : gained.ToString();
                gainTexts[i].gameObject.SetActive(true);
            }

            // Llevar al frente
            BringToFront(i);

            // Crecer (a 1.5x)
            yield return ScaleQuad(quads[i], 1f, 1.5f, growDuration);

            // Sumar puntos de forma DISCRETA durante la ventana holdDuration
            yield return AddPointsDiscrete(i, baseTotals, targetTotals[i], numPlayers, holdDuration);

            // Encoger a 1x
            yield return ScaleQuad(quads[i], 1.5f, 1f, shrinkDuration);

            // Consolidar localmente
            baseTotals[i] = targetTotals[i];

            // Reset visual
            SetAllGainsActive(false);
            RestoreOriginalOrder();
            ResetAllQuadScales();
            WriteTotals(baseTotals, numPlayers);

            yield return new WaitForSeconds(midBaseDuration);
        }

        // Final neutro
        SetAllGainsActive(false);
        RestoreOriginalOrder();
        ResetAllQuadScales();
        WriteTotals(targetTotals, numPlayers);

        // Aplicar a RoundData (incluye su 1s interno)
        yield return StartCoroutine(CommitTotalsAfterOneSecond());

        yield return new WaitForSeconds(finalBaseDuration);
        if (minigameChooser)
            minigameChooser.LoadNextScheduledOrFinish();
    }

    // ---- Animación discreta de puntos (+1 por tick con SFX) ----
    private IEnumerator AddPointsDiscrete(int playerIdx, int[] baseTotals, int targetTotal, int numPlayers, float windowSeconds)
    {
        int current = baseTotals[playerIdx];
        int delta = targetTotal - current;

        // Si no hay cambio, solo espera la ventana para respetar el “hold”
        if (delta == 0 || windowSeconds <= 0f)
        {
            WriteTotalsWithSolo(playerIdx, baseTotals, targetTotal, numPlayers);
            yield return new WaitForSeconds(Mathf.Max(0f, windowSeconds));
            yield break;
        }

        int steps = Mathf.Abs(delta);
        // Queremos que sea “rápido”, pero caber dentro de la ventana
        float stepDelay = Mathf.Clamp(windowSeconds / steps, 0.02f, 0.10f); // 10–50 ticks por segundo

        int direction = (delta > 0) ? 1 : -1;

        for (int s = 0; s < steps; s++)
        {
            current += direction;
            baseTotals[playerIdx] = current;

            // Actualiza textos (solo este jugador cambia)
            WriteTotals(baseTotals, numPlayers);

            // Sonido por punto
            SoundManager.instance?.PlaySfx("Puntos:point");

            // Espera breve entre puntos
            yield return new WaitForSeconds(stepDelay);
        }

        // Asegurar el valor exacto final
        baseTotals[playerIdx] = targetTotal;
        WriteTotals(baseTotals, numPlayers);
    }

    // ---- Utilidades de UI / orden / escalado ----
    private void WriteTotals(int[] totals, int numPlayers)
    {
        for (int i = 0; i < numPlayers; i++)
            if (playerTexts[i])
                playerTexts[i].text = $"{totals[i]} pts";
    }

    private void WriteTotalsWithSolo(int soloIdx, int[] baseTotals, int targetTotalForSolo, int numPlayers)
    {
        for (int j = 0; j < numPlayers; j++)
        {
            int val = (j == soloIdx) ? targetTotalForSolo : baseTotals[j];
            if (playerTexts[j]) playerTexts[j].text = $"{val} pts";
        }
    }

    private void SetAllGainsActive(bool active)
    {
        foreach (var t in gainTexts)
            if (t) t.gameObject.SetActive(active);
    }

    private IEnumerator CommitTotalsAfterOneSecond()
    {
        var rd = RoundData.instance;
        if (rd != null)
        {
            rd.GetTotalPoints(); // ya espera 1s internamente
            yield return new WaitForSeconds(1f);
        }
    }

    private void BringToFront(int i)
    {
        if (i < 0 || i >= quads.Length) return;
        if (!quads[i]) return;
        quads[i].SetAsLastSibling();
    }

    private void RestoreOriginalOrder()
    {
        if (originalSiblings == null) return;
        for (int i = 0; i < quads.Length; i++)
        {
            if (quads[i] && originalSiblings[i] >= 0)
            {
                int max = quads[i].parent != null ? quads[i].parent.childCount : 0;
                int idx = Mathf.Clamp(originalSiblings[i], 0, Mathf.Max(0, max - 1));
                quads[i].SetSiblingIndex(idx);
            }
        }
    }

    private void ResetAllQuadScales()
    {
        for (int i = 0; i < quads.Length; i++)
            if (quads[i]) quads[i].localScale = one;
    }

    private IEnumerator ScaleQuad(RectTransform rt, float from, float to, float duration)
    {
        if (!rt) yield break;

        float t = 0f;
        Vector3 start = Vector3.one * from;
        Vector3 end = Vector3.one * to;
        rt.localScale = start;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rt.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        rt.localScale = end;
    }
}
