using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinalResultsLayout : MonoBehaviour
{
    [Header("Barras de jugadores (0..3)")]
    [SerializeField] private RectTransform[] playerBars = new RectTransform[4];

    [Header("Iconos por jugador (0..3)")]
    [SerializeField] private RectTransform[] playerIcons = new RectTransform[4];

    [Header("Medalla unica")]
    [SerializeField] private GameObject medal;

    [Header("Plantillas de porcentajes")]
    [SerializeField] private float[] widthsFor2 = new float[] { 0.75f, 0.25f };
    [SerializeField] private float[] widthsFor3 = new float[] { 0.50f, 0.30f, 0.20f };
    [SerializeField] private float[] widthsFor4 = new float[] { 0.50f, 0.25f, 0.15f, 0.10f };

    [Header("Colores por jugador")]
    [SerializeField] private Color p1 = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color p2 = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color p3 = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color p4 = new Color(0.2f, 0.9f, 0.4f);

    [Header("Medalla dentro de la barra del ganador")]
    [SerializeField] private Vector2 medalAnchoredOffset = new Vector2(0f, -120f);
    [SerializeField] private bool hideMedalOnTie = true;

    void Start()
    {
        LayoutBars();
    }

    private void LayoutBars()
    {
        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null) return;

        int n = Mathf.Clamp(rd.numPlayers, 2, 4);

        int[] scores = new int[n];
        for (int i = 0; i < n; i++) scores[i] = rd.totalPoints[i];

        int[] order = Enumerable.Range(0, n)
                                .OrderByDescending(i => scores[i])
                                .ThenBy(i => i)
                                .ToArray();

        var groups = new List<List<int>>();
        int idx = 0;
        while (idx < n)
        {
            int j = idx + 1;
            while (j < n && scores[order[j]] == scores[order[idx]]) j++;
            groups.Add(order.Skip(idx).Take(j - idx).ToList());
            idx = j;
        }

        float[] tpl = n == 2 ? widthsFor2 : (n == 3 ? widthsFor3 : widthsFor4);
        if (tpl == null || tpl.Length < n) tpl = Enumerable.Repeat(1f / n, n).ToArray();

        float[] widthByRank = new float[n];
        int rankCursor = 0;
        foreach (var g in groups)
        {
            int span = g.Count;
            float sum = 0f;
            for (int k = 0; k < span; k++) sum += tpl[Mathf.Clamp(rankCursor + k, 0, n - 1)];
            float each = sum / span;
            for (int k = 0; k < span; k++) widthByRank[rankCursor + k] = each;
            rankCursor += span;
        }
        if (groups.Count == 1 && groups[0].Count == n)
            for (int r = 0; r < n; r++) widthByRank[r] = 1f / n;

        float x = 0f;
        for (int r = 0; r < n; r++)
        {
            int playerIndex = order[r];
            float w = widthByRank[r];

            var bar = playerBars[playerIndex];
            if (!bar) continue;

            bar.anchorMin = new Vector2(x, 0f);
            bar.anchorMax = new Vector2(x + w, 1f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.gameObject.SetActive(true);

            var img = bar.GetComponent<Image>();
            if (img)
            {
                img.color = playerIndex switch
                {
                    0 => p1,
                    1 => p2,
                    2 => p3,
                    3 => p4,
                    _ => img.color
                };
            }

            var icon = SafeIcon(playerIndex);
            if (icon)
            {
                icon.SetParent(bar, false);
                icon.anchorMin = new Vector2(0.5f, 0.5f);
                icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.pivot = new Vector2(0.5f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.localScale = Vector3.one;
                icon.gameObject.SetActive(true);
            }

            x += w;
        }

        for (int i = n; i < playerBars.Length; i++)
            if (playerBars[i]) playerBars[i].gameObject.SetActive(false);
        for (int i = n; i < playerIcons.Length; i++)
            if (playerIcons[i]) playerIcons[i].gameObject.SetActive(false);

        if (medal)
        {
            bool singleWinner = groups.Count > 0 && groups[0].Count == 1;
            if (singleWinner)
            {
                int winner = groups[0][0];
                var winnerBar = playerBars[winner];
                if (winnerBar)
                {
                    medal.SetActive(true);
                    var mrt = medal.transform as RectTransform;
                    medal.transform.SetParent(winnerBar, false);
                    if (mrt)
                    {
                        mrt.anchorMin = new Vector2(0.5f, 0.5f);
                        mrt.anchorMax = new Vector2(0.5f, 0.5f);
                        mrt.pivot = new Vector2(0.5f, 0.5f);
                        mrt.anchoredPosition = medalAnchoredOffset;
                        mrt.localScale = Vector3.one;
                    }
                }
                else medal.SetActive(false);
            }
            else
            {
                medal.SetActive(!hideMedalOnTie);
            }
        }
    }

    private RectTransform SafeIcon(int playerIndex)
    {
        if (playerIcons == null) return null;
        if (playerIndex < 0 || playerIndex >= playerIcons.Length) return null;
        return playerIcons[playerIndex];
    }
}
