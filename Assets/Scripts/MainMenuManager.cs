using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels (RectTransform)")]
    [SerializeField] private RectTransform playersPanel;
    [SerializeField] private RectTransform gamesPanel;

    [Header("Otros")]
    [SerializeField] private MinigameChooser minigameChooser;

    [Header("Animación")]
    [SerializeField] private float enterDuration = 0.55f;
    [SerializeField] private float exitDuration = 0.40f;
    [SerializeField] private Ease enterEase = Ease.OutBack;
    [SerializeField] private Ease exitEase = Ease.InBack;

    private Vector2 screenSize;

    void Awake()
    {
        // Guardamos tamaño base
        var parentRT = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        screenSize = parentRT ? parentRT.rect.size : new Vector2(Screen.width, Screen.height);
    }

    // =====================================================
    // ====================== ENTRADA ======================
    // =====================================================

    public void TogglePlayersPanel()
    {
        if (!playersPanel) return;
        if (!playersPanel.gameObject.activeSelf)
            PanelEnter(playersPanel);
    }

    public void HowManyPlayers(int players)
    {
        Debug.Log(players);
        RoundData.instance.ResetData();
        RoundData.instance.GetNumberOfPlayers(players);

        if (gamesPanel && !gamesPanel.gameObject.activeSelf)
            PanelEnter(gamesPanel);
    }

    // =====================================================
    // ======================= SALIDA ======================
    // =====================================================

    /// <summary>
    /// Saca ambos paneles hacia abajo con Ease.InBack.
    /// </summary>
    public void ExitAllPanels()
    {
        if (playersPanel && playersPanel.gameObject.activeSelf)
            PanelExit(playersPanel);

        if (gamesPanel && gamesPanel.gameObject.activeSelf)
            PanelExit(gamesPanel);
    }

    // =====================================================
    // ==================== ANIMACIONES ====================
    // =====================================================

    private void PanelEnter(RectTransform panel)
    {
        if (!panel) return;

        PrepareFullScreen(panel);
        panel.gameObject.SetActive(true);
        panel.DOKill();

        Vector2 startPos = new Vector2(0f, -screenSize.y); // entra desde abajo
        panel.anchoredPosition = startPos;

        panel.DOAnchorPos(Vector2.zero, enterDuration)
             .SetEase(enterEase)
             .SetUpdate(false);
    }

    private void PanelExit(RectTransform panel)
    {
        if (!panel || !panel.gameObject.activeSelf) return;

        PrepareFullScreen(panel);
        panel.DOKill();

        Vector2 endPos = new Vector2(0f, -screenSize.y); // sale hacia abajo

        panel.DOAnchorPos(endPos, exitDuration)
             .SetEase(exitEase)
             .SetUpdate(false)
             .OnComplete(() =>
             {
                 panel.gameObject.SetActive(false);
                 panel.anchoredPosition = Vector2.zero;
             });
    }

    private void PrepareFullScreen(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    // =====================================================
    // =================== ESCENA TIENDA ===================
    // =====================================================

    public void ShopButton()
    {
        SceneController.Instance.LoadScene("Tienda");

    }
}
