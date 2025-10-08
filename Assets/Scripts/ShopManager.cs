using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
    [Tooltip("Botones de comprar por skin, index = number de la skin")]
    [SerializeField] private GameObject[] buyButtons;
    [Tooltip("Precio por skin, por índice")]
    [SerializeField] private int[] skinPrices;

    [System.Serializable]
    public class ShopSkinVisual
    {
        [Tooltip("Image que muestra el sprite de este botón de compra")]
        public Image buttonImage;
        [Tooltip("Sprites por jugador para este botón de skin. Index 0..3 = Jugador 1..4")]
        public Sprite[] spritePerPlayer = new Sprite[4];
    }

    [Header("Visual por skin y jugador")]
    [Tooltip("Debe alinear su longitud con buyButtons/skinPrices por índice de skin")]
    [SerializeField] private ShopSkinVisual[] skinVisuals;

    [Header("Feedback opcional")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject selectPlayerPanel;

    private int selectedPlayer = 0;

    void Start()
    {
        if (selectPlayerPanel) selectPlayerPanel.SetActive(true);
        RefreshMoneyUI();
  
    }

    public void SelectBuyer(int playerIndex)
    {
        selectedPlayer = Mathf.Clamp(playerIndex, 0, 3);
        RefreshShopButtons();
        if (infoText) infoText.text = "Comprador: Jugador " + (selectedPlayer + 1);
        TogglePanel();
    }

    public void TogglePanel()
    {
        if (selectPlayerPanel) selectPlayerPanel.SetActive(!selectPlayerPanel.activeSelf);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
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

            // Actualizar sprite del botón según el jugador seleccionado
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
