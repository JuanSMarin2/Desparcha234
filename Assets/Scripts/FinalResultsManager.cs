using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FinalResultsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI winnerText;      // Se reutiliza para mensaje de ganadores y texto informativo
    [SerializeField] private TextMeshProUGUI conversionText;  // Solo numeros: PuntosTotales: PT --- Dinero: D
    [SerializeField] private Button continueButton;           // Boton para iniciar la conversion
    [SerializeField] private Button shopButton;               // Se activa al terminar conversion
    [SerializeField] private Button mainMenuButton;           // Se activa al terminar conversion

    [Header("Flujo")]
    [SerializeField] private string shopSceneName = "Shop";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Economia")]
    [SerializeField] private int coinsPerPoint = 10;          // Monedas por punto
    [SerializeField] private float conversionRate = 5f;      // Puntos convertidos por segundo en la animacion
    [SerializeField] private GameObject[] icons;

    private bool conversionRunning = false;

    void Start()
    {
        if (continueButton) continueButton.onClick.AddListener(OnContinuePressed);
        if (shopButton) shopButton.onClick.AddListener(() => { if (!string.IsNullOrEmpty(shopSceneName)) SceneManager.LoadScene(shopSceneName); });
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => { if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName); });

        if (shopButton) shopButton.gameObject.SetActive(false);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(false);
        if (conversionText) { conversionText.gameObject.SetActive(false); conversionText.text = ""; }

        // Desactiva todos los iconos de forma segura
        if (icons != null)
        {
            for (int i = 0; i < icons.Length; i++)
                if (icons[i] != null) icons[i].SetActive(false);
        }

        ShowWinners();
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

        var ganadores = new System.Collections.Generic.List<int>();
        for (int i = 0; i < puntos.Length; i++)
            if (puntos[i] == maxPoints) ganadores.Add(i);

        if (ganadores.Count == 1)
        {
            int jugador = ganadores[0] + 1;
            if (winnerText) winnerText.text = "Ganador: Jugador " + jugador;
        }
        else
        {
            string jugadores = string.Join(" y ", ganadores.ConvertAll(g => "Jugador " + (g + 1)));
            if (winnerText) winnerText.text = "Ganadores: " + jugadores;
        }

        // Activa iconos de ganadores de forma segura
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
        StartCoroutine(AnimateConversion());
    }



    private IEnumerator AnimateConversion()
    {
        conversionRunning = true;

        var rd = RoundData.instance;
        if (rd == null || rd.totalPoints == null || rd.totalPoints.Length == 0)
        {
            if (winnerText) winnerText.text = "No hay puntos para convertir.";
            yield break;
        }

        // Sumamos puntos de la última ronda
        int pointsTotal = 0;
        for (int i = 0; i < rd.totalPoints.Length; i++)
            pointsTotal += rd.totalPoints[i];

        int baseMoney = GameData.instance != null ? GameData.instance.Money : 0;
        int coinsTotal = pointsTotal * Mathf.Max(1, coinsPerPoint);

        // Mensaje inicial
        if (winnerText)
            winnerText.text = "Los puntos ganados se convierten en dinero...";

        if (conversionText) conversionText.gameObject.SetActive(true);
        if (continueButton) continueButton.gameObject.SetActive(false);

        // Duración fija de animación
        float duration = 5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Interpolamos entre inicial y final
            int shownPoints = Mathf.RoundToInt(Mathf.Lerp(pointsTotal, 0, t));
            int shownMoney = Mathf.RoundToInt(Mathf.Lerp(baseMoney, baseMoney + coinsTotal, t));

            if (conversionText)
                conversionText.text = $"PuntosTotales: {shownPoints} --- Dinero: {shownMoney}";

            yield return null;
        }

        // Final fijo
        if (conversionText)
            conversionText.text = $"PuntosTotales: 0 --- Dinero: {baseMoney + coinsTotal}";

        if (GameData.instance != null && coinsTotal > 0)
            GameData.instance.AddMoney(coinsTotal);

        if (shopButton) shopButton.gameObject.SetActive(true);
        if (mainMenuButton) mainMenuButton.gameObject.SetActive(true);
        if (winnerText)
            winnerText.text = "Puedes comprar skins en la tienda";

        conversionRunning = false;
    }
}
