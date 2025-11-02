using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class FinalResultsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI conversionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Flujo")]
    [SerializeField] private string shopSceneName = "Tienda";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Economia")]
    [SerializeField] private int coinsPerPoint = 10;
    [Tooltip("Duración objetivo (seg) para la animación de conversión.")]
    [SerializeField] private float conversionDuration = 1.2f;

    [Header("Iconos de ganadores (opcional)")]
    [SerializeField] private GameObject[] icons;

    [Header("Auto-continue")]
    [SerializeField] private float autoContinueDelay = 5f;

    private bool conversionRunning = false;

    void Start()
    {
        if (continueButton) continueButton.onClick.AddListener(OnContinuePressed);
        if (shopButton) shopButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(shopSceneName)) SceneController.Instance.LoadScene(shopSceneName);
        });
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneController.Instance.LoadScene(mainMenuSceneName);
        });

        if (shopButton) shopButton.gameObject.SetActive(false);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(false);
        if (conversionText) { conversionText.gameObject.SetActive(false); conversionText.text = ""; }

        if (icons != null)
        {
            for (int i = 0; i < icons.Length; i++)
                if (icons[i] != null) icons[i].SetActive(false);
        }

        ShowWinners();

        // Auto iniciar conversión tras X segundos (si no se ha iniciado manualmente)
        StartCoroutine(AutoContinueAfterDelay());
    }

    private IEnumerator AutoContinueAfterDelay()
    {
        float t = Mathf.Max(0f, autoContinueDelay);
        if (t > 0f) yield return new WaitForSeconds(t);
        if (!conversionRunning) OnContinuePressed();
    }

    private void ShowWinners()
    {
        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.totalPoints.Length == 0)
        {
            if (winnerText) winnerText.text = "No hay resultados disponibles.";
            if (continueButton) continueButton.gameObject.SetActive(false);
            return;
        }

        int[] puntos = rd.totalPoints;
        int maxPoints = puntos.Max();

        var ganadores = new List<int>();
        for (int i = 0; i < puntos.Length; i++)
            if (puntos[i] == maxPoints) ganadores.Add(i);

        // Sin textos visibles
        if (winnerText) winnerText.text = "";

        // Iconos ganadores visibles
        if (icons != null)
        {
            foreach (int ganadorIndex in ganadores)
            {
                if (ganadorIndex >= 0 && ganadorIndex < icons.Length && icons[ganadorIndex] != null)
                    icons[ganadorIndex].SetActive(true);
            }
        }
    }

    private void OnContinuePressed()
    {
        if (conversionRunning) return;
        StartCoroutine(AnimateConversionDiscrete());
    }

    private IEnumerator AnimateConversionDiscrete()
    {
        conversionRunning = true;

        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.totalPoints.Length == 0)
        {
            if (winnerText) winnerText.text = "No hay puntos para convertir.";
            conversionRunning = false;
            yield break;
        }

        int pointsTotal = 0;
        for (int i = 0; i < rd.totalPoints.Length; i++)
            pointsTotal += rd.totalPoints[i];

        int baseMoney = GameData.instance != null ? GameData.instance.Money : 0;
        int coinsTotal = pointsTotal * Mathf.Max(1, coinsPerPoint);

        if (winnerText) winnerText.text = "";
        if (conversionText) conversionText.gameObject.SetActive(true);
        if (continueButton) continueButton.gameObject.SetActive(false);

        int pointsLeft = pointsTotal;
        int shownPoints = pointsTotal;
        int shownMoney = baseMoney;

        const float minTickDelay = 0.03f;
        int maxTicks = Mathf.Max(1, Mathf.FloorToInt(conversionDuration / minTickDelay));
        int pointsPerTick = Mathf.Max(1, Mathf.CeilToInt((float)pointsLeft / maxTicks));
        float actualTicks = Mathf.Ceil((float)pointsLeft / pointsPerTick);
        float tickDelay = (conversionDuration <= 0f) ? 0f : (conversionDuration / actualTicks);

        while (pointsLeft > 0)
        {
            int step = Mathf.Min(pointsPerTick, pointsLeft);
            pointsLeft -= step;

            shownPoints -= step;
            shownMoney += step * coinsPerPoint;

            if (conversionText)
                conversionText.text = $"Puntos: {shownPoints} --- Dinero: {shownMoney}";

            // SFX por “tick” de puntos
            SoundManager.instance?.PlaySfx("Final:coins");

            if (tickDelay > 0f) yield return new WaitForSeconds(tickDelay);
            else yield return null;
        }

        if (conversionText)
            conversionText.text = $"Puntos: 0 --- Dinero: {baseMoney + coinsTotal}";

        if (GameData.instance != null && coinsTotal > 0)
            GameData.instance.AddMoney(coinsTotal);

        if (shopButton) shopButton.gameObject.SetActive(true);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(true);
        if (winnerText) winnerText.text = "Puedes comprar skins en la tienda";

        conversionRunning = false;
    }

    public void ShopButton()
    {
        SceneController.Instance.LoadScene("Tienda");
    }
}
