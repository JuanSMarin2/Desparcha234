using UnityEngine;
using UnityEngine.UI;
using TMPro;
// OJO: ya no necesitas usar SceneManager aquí para cambiar de escena.
// using UnityEngine.SceneManagement;  // <- puedes quitarlo si no lo usas en otra parte

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

    [Header("Feedback opcional")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private GameObject selectPlayerPanel;

    private int selectedPlayer = 0;

    void Start()
    {
        // Si tienes botón para volver al menú, su onClick debe llamar a ReturnToMenu()
        selectPlayerPanel.SetActive(true);
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
        selectPlayerPanel.gameObject.SetActive(!selectPlayerPanel.activeSelf);
    }

    // ===== CAMBIO IMPORTANTE: usar SceneController para la transición =====
    public void ReturnToMenu()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.LoadScene("MainMenu");
        else
            Debug.LogWarning("SceneController no existe en la escena. Asegúrate de tener el prefab con TransitionPanel + SceneController persistiendo.");
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
