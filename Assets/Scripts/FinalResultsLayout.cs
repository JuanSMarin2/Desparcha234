using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FinalResultsLayout : MonoBehaviour
{
    [Header("Barras de jugadores (0..3)")]
    [SerializeField] private RectTransform[] playerBars = new RectTransform[4];

    [Header("Iconos por jugador (0..3)")]
    [SerializeField] private RectTransform[] playerIcons = new RectTransform[4];

    [Header("Medallas (hasta 4, una por posible ganador)")]
    [SerializeField] private RectTransform[] medals = new RectTransform[4];

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

    [Header("Pulse (iconos ganadores)")]
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 6f;

    private readonly List<Coroutine> pulseRoutines = new List<Coroutine>();

    void OnDisable()
    {
        // detener pulsos si se desactiva el GO
        for (int i = 0; i < pulseRoutines.Count; i++)
            if (pulseRoutines[i] != null) StopCoroutine(pulseRoutines[i]);
        pulseRoutines.Clear();
    }

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

        // Orden por puntuaci�n desc
        int[] order = Enumerable.Range(0, n)
                                .OrderByDescending(i => scores[i])
                                .ThenBy(i => i)
                                .ToArray();

        // Agrupar empates por rangos
        var groups = new List<List<int>>();
        int idx = 0;
        while (idx < n)
        {
            int j = idx + 1;
            while (j < n && scores[order[j]] == scores[order[idx]]) j++;
            groups.Add(order.Skip(idx).Take(j - idx).ToList());
            idx = j;
        }

        // Plantilla de anchos
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

        // Posicionar barras e iconos
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

        // Desactivar sobras
        for (int i = n; i < playerBars.Length; i++)
            if (playerBars[i]) playerBars[i].gameObject.SetActive(false);
        for (int i = n; i < playerIcons.Length; i++)
            if (playerIcons[i]) playerIcons[i].gameObject.SetActive(false);

        // Medallas e iconos �palpitando� para TODOS los ganadores (grupo 0)
        var winners = (groups.Count > 0) ? groups[0] : null;
        if (winners != null && winners.Count > 0)
        {
            // Setear triggers de animación: ganadores -> Happy, resto -> Sad
            var winnerSet = new HashSet<int>(winners);
            for (int i = 0; i < n; i++)
            {
                bool isHappy = winnerSet.Contains(i);
                SetIconMood(i, isHappy);
            }

            // Medallas (una por ganador, hasta la cantidad disponible)
            for (int m = 0; m < medals.Length; m++)
                if (medals[m]) medals[m].gameObject.SetActive(false);

            int medalSlot = 0;
            foreach (var winPlayer in winners)
            {
                if (medalSlot >= medals.Length) break;

                var bar = playerBars[winPlayer];
                var medal = medals[medalSlot];
                if (bar && medal)
                {
                    medal.gameObject.SetActive(true);
                    medal.SetParent(bar, false);
                    medal.anchorMin = medal.anchorMax = medal.pivot = new Vector2(0.5f, 0.5f);
                    medal.anchoredPosition = medalAnchoredOffset;
                    medal.localScale = Vector3.one;
                }
                medalSlot++;

                // Pulse para el icono del ganador
                var icon = SafeIcon(winPlayer);
                if (icon)
                {
                    var co = StartCoroutine(PulseIcon(icon));
                    pulseRoutines.Add(co);
                }
            }
        }
        else
        {
            // Sin ganadores �nicos � ocultar todas las medallas
            for (int m = 0; m < medals.Length; m++)
                if (medals[m]) medals[m].gameObject.SetActive(false);

            // Si no hay ganadores definidos, todos en Sad
            for (int i = 0; i < n; i++)
            {
                SetIconMood(i, false);
            }
        }
    }

    private IEnumerator PulseIcon(RectTransform icon)
    {
        // Latido infinito (r�pido). Puedes detenerlo en OnDisable.
        float t = 0f;
        while (icon && icon.gameObject.activeInHierarchy)
        {
            t += Time.deltaTime * pulseSpeed;
            float s = Mathf.Lerp(1f, pulseScale, 0.5f * (1f + Mathf.Sin(t)));
            icon.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (icon) icon.localScale = Vector3.one;
    }

    private RectTransform SafeIcon(int playerIndex)
    {
        if (playerIcons == null) return null;
        if (playerIndex < 0 || playerIndex >= playerIcons.Length) return null;
        return playerIcons[playerIndex];
    }

    private void SetIconMood(int playerIndex, bool isHappy)
    {
        var icon = SafeIcon(playerIndex);
        if (!icon) return;
        Animator anim = icon.GetComponent<Animator>();
        if (anim == null) anim = icon.GetComponentInChildren<Animator>(true);
        if (anim == null) return;
        // Resetear todos los posibles triggers para evitar residuos
        anim.ResetTrigger("Happy");
        anim.ResetTrigger("Sad");
        anim.ResetTrigger("Neutral");
        anim.ResetTrigger("Happy2");
        anim.ResetTrigger("Sad2");
        anim.ResetTrigger("Neutral2");

        int equipped = 0;
        if (GameData.instance != null)
        {
            equipped = GameData.instance.GetEquipped(playerIndex);
        }

        string logical = isHappy ? "Happy" : "Sad";
        string finalTrigger = (equipped == 1) ? logical + "2" : logical;
        anim.SetTrigger(finalTrigger);
    }
}
