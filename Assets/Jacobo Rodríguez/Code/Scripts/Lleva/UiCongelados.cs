using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UiCongelados : MonoBehaviour
{
    [Header("Score live (TextMeshPro)")]
    [SerializeField] private TMP_Text p1Text;
    [SerializeField] private TMP_Text p2Text;
    [SerializeField] private TMP_Text p3Text;
    [SerializeField] private TMP_Text p4Text;

    [Header("Panel fin de ronda")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endTitleText; // usaremos como 'mensaje'
    [SerializeField] private TMP_Text endWinnersText; // queda como fallback si no hay IconManagerGeneral
    

    [Header("Mensaje (serializable)")]
    [SerializeField] private string mensajeFreezerGana = "¡Gana el Freezer!";
    [SerializeField] private string mensajeRunnersGanan = "¡Ganan los No Congelados!";
    [SerializeField] private string mensajeVictoriaFinal = "¡Victoria Final!"; // nuevo
    [SerializeField] private string mensajeEmpateFinal = "¡Empate Final!"; // nuevo

    [Header("Iconos de ganadores (Images)")]
    [Tooltip("Slots de iconos para mostrar ganadores en el panel (usa 1 si gana el freezer, 3 si ganan los runners)")]
    [SerializeField] private Image[] winnerIconSlots = new Image[3];

    [Header("Nombres opcionales (1..4)")]
    [SerializeField] private string[] playerNames = new string[]{ "Rojo", "Azul", "Amarillo", "Verde" };

    [Header("Auto-cerrar panel de fin (segundos)")]
    [SerializeField] private float autoCloseSeconds = 1.5f;

    [Header("Debug")] [SerializeField] private bool debugLogs = false;

    private void OnEnable()
    {
        TagCongelados.OnRoundStarted += OnRoundStarted;
        TagCongelados.OnRoundEnded += OnRoundEnded;
        RefreshScoresLive();
        if (endPanel) endPanel.SetActive(false);
    }

    private void OnDisable()
    {
        TagCongelados.OnRoundStarted -= OnRoundStarted;
        TagCongelados.OnRoundEnded -= OnRoundEnded;
    }

    private void OnRoundStarted()
    {
        // Ocultar panel de fin si estaba visible y refrescar scoreboard
        if (endPanel) endPanel.SetActive(false);
        RefreshScoresLive();
    }

    private void OnRoundEnded(TagCongelados.RoundEndData data)
    {
        // Mostrar siempre el panel post-turno al finalizar cada ronda
        if (endPanel && !endPanel.activeSelf) endPanel.SetActive(true);
        MakePanelVisible();
        RefreshScoresLive();
        ShowEndPanel(data);
    }

    private void RefreshScoresLive()
    {
        var sm = CongeladosScoreManager.Instance;
        if (sm == null)
        {
            if (debugLogs) Debug.LogWarning("[UiCongelados] No hay CongeladosScoreManager en escena.");
            return;
        }
        if (p1Text) p1Text.text = sm.GetScore(1).ToString();
        if (p2Text) p2Text.text = sm.GetScore(2).ToString();
        if (p3Text) p3Text.text = sm.GetScore(3).ToString();
        if (p4Text) p4Text.text =  sm.GetScore(4).ToString();
    }

    private void ShowEndPanel(TagCongelados.RoundEndData data)
    {
        if (!endPanel) return;
        endPanel.SetActive(true);
        MakePanelVisible();
        Debug.Log("UI Congelados - Mostrando panel de fin de ronda.");
        // Mensaje
        if (endTitleText)
        {
            if (data.type == TagCongelados.RoundEndType.FinalVictory)
            {
                bool multi = data.winners1Based != null && data.winners1Based.Count > 1;
                endTitleText.text = multi ? mensajeEmpateFinal : mensajeVictoriaFinal;
            }
            else if (data.type == TagCongelados.RoundEndType.FreezerWin)
            {
                endTitleText.text = mensajeFreezerGana;
            }
            else
            {
                endTitleText.text = mensajeRunnersGanan;
            }
        }

        // Ganadores como iconos (felices)
        bool showedIcons = SetWinnerIcons(data);
        if (endWinnersText)
        {
            // Fallback a texto si no pudimos mostrar íconos (por falta de IconManagerGeneral o slots)
            if (!showedIcons)
            {
                if (data.winners1Based != null && data.winners1Based.Count > 0)
                {
                    List<string> names = new List<string>();
                    foreach (var idx in data.winners1Based) names.Add(GetPlayerName(idx));
                    endWinnersText.text = "Ganadores: " + string.Join(", ", names);
                }
                else if (data.type == TagCongelados.RoundEndType.FreezerWin)
                {
                    endWinnersText.text = "Ganador: " + GetPlayerName(data.freezerIndex1Based);
                }
                else
                {
                    endWinnersText.text = "";
                }
            }
            else
            {
                endWinnersText.text = "";
            }
        }

        // autocerrar si se desea
        if (autoCloseSeconds > 0f)
        {
            CancelInvoke(nameof(HideEndPanel));
            Invoke(nameof(HideEndPanel), autoCloseSeconds);
        }
    }

    private bool SetWinnerIcons(TagCongelados.RoundEndData data)
    {
        var gm = IconManagerGeneral.Instance;
        // Limpiar todos los slots primero
        if (winnerIconSlots != null)
        {
            foreach (var img in winnerIconSlots) if (img) { img.enabled = false; img.sprite = null; }
        }
        if (gm == null || winnerIconSlots == null || winnerIconSlots.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[UiCongelados] IconManagerGeneral o slots de icono no asignados; usando fallback de texto.");
            return false;
        }

        // Construir lista de ganadores 0-based
        List<int> winners0 = new List<int>();
        if (data.type == TagCongelados.RoundEndType.FreezerWin)
        {
            winners0.Add(Mathf.Clamp(data.freezerIndex1Based - 1, 0, 3));
        }
        else if (data.winners1Based != null)
        {
            foreach (var w in data.winners1Based) winners0.Add(Mathf.Clamp(w - 1, 0, 3));
        }

        // Asignar sprites felices a los slots disponibles
        int count = Mathf.Min(winners0.Count, winnerIconSlots.Length);
        for (int i = 0; i < count; i++)
        {
            var img = winnerIconSlots[i]; if (!img) continue;
            var s = gm.GetHappy(winners0[i]);
            if (s)
            {
                img.sprite = s; img.enabled = true;
            }
        }
        return count > 0;
    }

    private void HideEndPanel()
    {
        if (endPanel) endPanel.SetActive(false);
    }

    private string GetPlayerName(int idx1)
    {
        return (playerNames != null && idx1-1 >= 0 && idx1-1 < playerNames.Length) ? playerNames[idx1-1] : ($"P{idx1}");
    }

    private void MakePanelVisible()
    {
        if (!endPanel) return;
        // Asegurar Canvas habilitado en jerarquía
        var canvas = endPanel.GetComponentInParent<Canvas>(includeInactive: true);
        if (canvas && !canvas.enabled) canvas.enabled = true;
        // Forzar CanvasGroup alpha=1 en este panel (si existe)
        var cg = endPanel.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        // Asegurar habilitado de gráficos base del panel
        var graphics = endPanel.GetComponentsInChildren<Graphic>(includeInactive: true);
        foreach (var g in graphics) if (g) g.enabled = true;
    }
}
