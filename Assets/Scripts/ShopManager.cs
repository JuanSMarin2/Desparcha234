using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening; // DOTween

public class ShopManager : MonoBehaviour
{
    [Header("Selección de comprador")]
    [SerializeField] private Button player1Btn;
    [SerializeField] private Button player2Btn;
    [SerializeField] private Button player3Btn;
    [SerializeField] private Button player4Btn;

    [Header("UI dinero")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Skins")]
    [SerializeField] private GameObject[] buyButtons;
    [SerializeField] private int[] skinPrices;

    [System.Serializable]
    public class ShopSkinVisual
    {
        public Image buttonImage;
        public Sprite[] spritePerPlayer = new Sprite[4];
    }

    [Header("Visual por skin y jugador")]
    [SerializeField] private ShopSkinVisual[] skinVisuals;

    [Header("Feedback opcional")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject selectPlayerPanel;

    // ===== Animación de paneles (igual que MainMenu) =====
    [Header("Animación de paneles (DOTween)")]
    [SerializeField] private RectTransform[] animatedPanels;
    [SerializeField] private float enterDuration = 0.6f;
    [SerializeField] private float exitDuration = 0.5f;
    [SerializeField] private float enterOvershoot = 1.2f;

    private Vector2[] targetPos; // posiciones onscreen
    private bool panelsEntered = false;

    // --- Panel de selección (animación con TogglePanel) ---
    private RectTransform selectPanelRT;
    private Vector2 selectPanelTargetPos = Vector2.zero; // SIEMPRE pantalla completa -> (0,0)
    private bool selectPanelIsOnscreen = false;

    private int selectedPlayer = 0;

    void Start()
    {
        if (selectPlayerPanel)
        {
            selectPanelRT = selectPlayerPanel.GetComponent<RectTransform>();
            if (selectPanelRT != null)
            {
                // Asegurar que siempre ocupa toda la pantalla (stretch + offsets 0)
                EnsureFullScreen(selectPanelRT);
                selectPanelTargetPos = Vector2.zero;

                if (selectPlayerPanel.activeSelf)
                {
                    PrepareRectForEnter(selectPanelRT); // arranca arriba
                    EnterRect(selectPanelRT);
                    selectPanelIsOnscreen = true;
                }
                else
                {
                    selectPanelIsOnscreen = false;
                }
            }
        }

        RefreshMoneyUI();

        // Paneles principales
        SetupPanelsForEnter();
        EnterPanels();
    }

    void OnDestroy()
    {
        if (animatedPanels != null)
        {
            foreach (var rt in animatedPanels)
                if (rt) rt.DOKill();
        }
        if (selectPanelRT) selectPanelRT.DOKill();
    }

    // =================== PANEL ANIMATIONS (principales) ===================

    private void SetupPanelsForEnter()
    {
        if (animatedPanels == null || animatedPanels.Length == 0) return;

        if (targetPos == null || targetPos.Length != animatedPanels.Length)
            targetPos = new Vector2[animatedPanels.Length];

        float screenH = Screen.height;

        for (int i = 0; i < animatedPanels.Length; i++)
        {
            var rt = animatedPanels[i];
            if (!rt) continue;

            targetPos[i] = rt.anchoredPosition;
            rt.anchoredPosition = targetPos[i] + Vector2.up * screenH * 1.2f; // arriba
        }
    }

    private void EnterPanels()
    {
        if (animatedPanels == null || animatedPanels.Length == 0) return;

        for (int i = 0; i < animatedPanels.Length; i++)
        {
            var rt = animatedPanels[i];
            if (!rt) continue;

            rt.DOKill();
            rt.DOAnchorPos(targetPos[i], enterDuration)
              .SetEase(Ease.OutBack, overshoot: enterOvershoot);
        }

        panelsEntered = true;
    }

    public void PlayExit()
    {
        if (!panelsEntered || animatedPanels == null || animatedPanels.Length == 0) return;

        float screenH = Screen.height;

        for (int i = 0; i < animatedPanels.Length; i++)
        {
            var rt = animatedPanels[i];
            if (!rt) continue;

            rt.DOKill();
            rt.DOAnchorPos(targetPos[i] + Vector2.down * screenH * 1.2f, exitDuration)
              .SetEase(Ease.InBack);
        }

        panelsEntered = false;
    }

    // =================== PANEL DE SELECCIÓN (TogglePanel con animación) ===================

    /// Activa/Desactiva con transición. Al entrar, ocupa TODA la pantalla y entra desde arriba.
    public void TogglePanel()
    {
        if (!selectPlayerPanel || selectPanelRT == null) return;

        if (!selectPlayerPanel.activeSelf || !selectPanelIsOnscreen)
        {
            // ENCENDER con entrada
            selectPlayerPanel.SetActive(true);

            // Forzar que ocupe pantalla completa SIEMPRE antes de animar
            EnsureFullScreen(selectPanelRT);
            selectPanelTargetPos = Vector2.zero;

            PrepareRectForEnter(selectPanelRT); // coloca arriba
            EnterRect(selectPanelRT);           // baja a (0,0)
            selectPanelIsOnscreen = true;
        }
        else
        {
            // APAGAR con salida hacia abajo y desactivar al terminar
            ExitRectDown(selectPanelRT, () =>
            {
                selectPlayerPanel.SetActive(false);
                selectPanelIsOnscreen = false;
            });
        }
    }

    private void PrepareRectForEnter(RectTransform rt)
    {
        float screenH = Screen.height;
        rt.DOKill();
        // Arranca fuera por ARRIBA respecto a su target (0,0)
        rt.anchoredPosition = selectPanelTargetPos + Vector2.up * screenH * 1.2f;
    }

    private void EnterRect(RectTransform rt)
    {
        rt.DOKill();
        rt.DOAnchorPos(selectPanelTargetPos, enterDuration)
          .SetEase(Ease.OutBack, overshoot: enterOvershoot);
    }

    private void ExitRectDown(RectTransform rt, System.Action onComplete)
    {
        float screenH = Screen.height;
        rt.DOKill();
        rt.DOAnchorPos(selectPanelTargetPos + Vector2.down * screenH * 1.2f, exitDuration)
          .SetEase(Ease.InBack)
          .OnComplete(() => onComplete?.Invoke());
    }

    /// Fuerza a ocupar toda la pantalla (anchors stretch + offsets 0) y posición target (0,0)
    private void EnsureFullScreen(RectTransform rt)
    {
        // Importante: no cambiamos parent ni layout, solo anchors/offsets
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; // target de pantalla completa
        rt.ForceUpdateRectTransforms();
    }

    // =================== LÓGICA DE TIENDA ===================

    public void SelectBuyer(int playerIndex)
    {
        selectedPlayer = Mathf.Clamp(playerIndex, 0, 3);
        RefreshShopButtons();
        if (infoText) infoText.text = "Comprador: Jugador " + (selectedPlayer + 1);
        TogglePanel(); // ahora hace animación de salida
    }

    public void ReturnToMenu()
    {
        SceneController.Instance.LoadScene("MainMenu");
    }

    public void BuySkin(int number)
    {
        if (!ValidateSkinIndex(number)) return;

        int price = skinPrices[number];
        var gd = GameData.instance;
        if (gd == null) return;

        if (gd.IsOwned(selectedPlayer, number))
        {
            if (infoText) infoText.text = "Ya posees esta skin.";
            SetBuyButtonActive(number, false);
            return;
        }

        if (!gd.TrySpendMoney(price))
        {
            if (infoText) infoText.text = "No tienes dinero suficiente.";
            RefreshMoneyUI();
            return;
        }

        gd.SetOwned(selectedPlayer, number, true);
        if (infoText) infoText.text = "Comprada skin " + number + " para Jugador " + (selectedPlayer + 1);

        SoundManager.instance?.PlaySfx("ui:comprar");
        SetBuyButtonActive(number, false);
        RefreshMoneyUI();
    }

    public void EquipSkin(int number)
    {
        if (!ValidateSkinIndex(number)) return;

        var gd = GameData.instance;
        if (gd == null) return;

        if (!gd.IsOwned(selectedPlayer, number))
        {
            if (infoText) infoText.text = "No posees esta skin. Cómprala primero.";
            return;
        }

        gd.EquipSkin(selectedPlayer, number);
        if (infoText) infoText.text = "Equipaste skin " + number + " en Jugador " + (selectedPlayer + 1);
        FindAnyObjectByType<IconManager>()?.UpdatePlayerIcon(selectedPlayer);
    }

    private bool ValidateSkinIndex(int index)
    {
        if (buyButtons == null || skinPrices == null)
        {
            Debug.LogWarning("Configura buyButtons y skinPrices en el inspector.");
            return false;
        }

        if (index < 0 || index >= skinPrices.Length)
        {
            Debug.LogWarning("Indice de skin fuera de rango: " + index);
            return false;
        }
        return true;
    }

    private void RefreshShopButtons()
    {
        var gd = GameData.instance;
        if (gd == null) return;

        int skinsCount = skinPrices != null ? skinPrices.Length : 0;

        for (int i = 0; i < skinsCount; i++)
        {
            bool owned = gd.IsOwned(selectedPlayer, i);
            SetBuyButtonActive(i, !owned);

            if (skinVisuals != null && i < skinVisuals.Length && skinVisuals[i] != null)
            {
                var vis = skinVisuals[i];
                if (vis.buttonImage != null &&
                    vis.spritePerPlayer != null &&
                    selectedPlayer < vis.spritePerPlayer.Length &&
                    vis.spritePerPlayer[selectedPlayer] != null)
                {
                    vis.buttonImage.sprite = vis.spritePerPlayer[selectedPlayer];
                }
            }
        }
    }

    private void SetBuyButtonActive(int index, bool active)
    {
        if (buyButtons == null || index < 0 || index >= buyButtons.Length) return;
        if (buyButtons[index] != null)
            buyButtons[index].SetActive(active);
    }

    private void RefreshMoneyUI()
    {
        if (moneyText && GameData.instance != null)
            moneyText.text = "Dinero: " + GameData.instance.Money;
    }
}
