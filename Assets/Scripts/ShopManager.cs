using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Feedback opcional")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject selectPlayerPanel;

    private int selectedPlayer = 0;



    void Start()
    {
 


        RefreshMoneyUI();
    }

    public void SelectBuyer(int playerIndex)
    {
        selectedPlayer = Mathf.Clamp(playerIndex, 0, 3);
        RefreshShopButtons();
        if (infoText) infoText.text = "Comprador: Jugador " + (selectedPlayer + 1);
        selectPlayerPanel.gameObject.SetActive(false);
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
            // Desactivar botón por si quedó activo
            SetBuyButtonActive(number, false);
            return;
        }

        if (!gd.TrySpendMoney(price))
        {
            if (infoText) infoText.text = "No tienes dinero suficiente.";
            RefreshMoneyUI();
            return;
        }

        // Comprar
        gd.SetOwned(selectedPlayer, number, true);
        if (infoText) infoText.text = "Comprada skin " + number + " para Jugador " + (selectedPlayer + 1);

        // Desactivar botón de comprar para esta skin
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
        // buyButtons puede ser menor si algunas skins no tienen botón. Solo se validará en SetBuyButtonActive.
        return true;
    }

    private void RefreshShopButtons()
    {
        var gd = GameData.instance;
        if (gd == null || buyButtons == null) return;

        for (int i = 0; i < buyButtons.Length; i++)
        {
            bool owned = gd.IsOwned(selectedPlayer, i);
            SetBuyButtonActive(i, !owned);
        }
    }

    private void SetBuyButtonActive(int index, bool active)
    {
        if (index < 0 || index >= buyButtons.Length) return;
        if (buyButtons[index] != null)
            buyButtons[index].SetActive(active);
    }

    private void RefreshMoneyUI()
    {
        if (moneyText && GameData.instance != null)
            moneyText.text = "Dinero: " + GameData.instance.Money;
    }
 


}
