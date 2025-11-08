using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class IconManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIconSet
    {
        public Image icon;               // Image del jugador en la UI
        public Sprite[] skinSprites;     // Sprites normales por número de skin
        public Sprite[] sadSkinSprites;  // Sprites "tristes" por número de skin
    }

    [Header("Iconos y skins por jugador (0..3)")]
    [SerializeField] private PlayerIconSet[] players = new PlayerIconSet[4];

    [Header("Usar resultados de la ronda para tristeza (Animator)")]
    [SerializeField] private bool useRoundResultsForSad = false;

    [Header("Escena de resultados finales (Animator)")]
    [SerializeField] private bool isFinalResultsScene = false;

    // Cache de ganadores para FinalResults (se calcula en RefreshAllIcons)
    private HashSet<int> cachedFinalWinners = null;

    void OnEnable()
    {
        GameManagerTejo.OnPapeletaDestruida += MostrarIconoTriste;
    }

    void OnDisable()
    {
        GameManagerTejo.OnPapeletaDestruida -= MostrarIconoTriste;
        StopAllCoroutines();
    }

    void Start()
    {
        RefreshAllIcons();
    }

    // ===== API (nombres intactos) =====
    public void SetUseRoundResults(bool value)
    {
        useRoundResultsForSad = value;
        RefreshAllIcons();
    }

    public void SetIsFinalResultsScene(bool value)
    {
        isFinalResultsScene = value;
        RefreshAllIcons();
    }

    public void RefreshAllIcons()
    {
        var rd = RoundData.instance;

        // Precalcular ganadores SOLO si estamos en final results y hay datos
        cachedFinalWinners = null;
        if (isFinalResultsScene && rd != null && rd.totalPoints != null && rd.totalPoints.Length > 0)
        {
            int n = Mathf.Clamp(rd.numPlayers, 1, Mathf.Min(rd.totalPoints.Length, players.Length));
            cachedFinalWinners = GetWinnersByTotal(rd.totalPoints, n);
            // si por alguna razón no hay ganadores válidos, dejamos null y caeremos en neutral
            if (cachedFinalWinners != null && cachedFinalWinners.Count == 0)
                cachedFinalWinners = null;
        }

        if (players == null) return;
        for (int i = 0; i < players.Length; i++)
            UpdatePlayerIcon(i);
    }

    public void UpdatePlayerIcon(int playerIndex)
    {
        if (!ValidPlayer(playerIndex)) return;

        var set = players[playerIndex];
        var rd  = RoundData.instance;

        // Siempre poner sprite base (skin equipada) como textura para el Animator
        int equipped = 0;
        if (GameData.instance != null)
            equipped = Mathf.Clamp(GameData.instance.GetEquipped(playerIndex), 0, 9999);

        var baseSprite = GetSafeSprite(set.skinSprites, equipped);
        if (baseSprite != null) { set.icon.sprite = baseSprite; set.icon.enabled = true; }
        else { set.icon.enabled = false; }

        var anim = FindAnimator(set.icon);

        // 1) FINAL RESULTS: triggers Happy/Sad (sin tocar sprites)
        if (isFinalResultsScene && anim != null && cachedFinalWinners != null)
        {
            bool isWinner = cachedFinalWinners.Contains(playerIndex);
            Fire(anim, isWinner ? "Happy" : "Sad");
            return;
        }

        // 2) ROUND RESULTS (useRoundResultsForSad): Happy/Neutral/Sad por currentPoints
        if (useRoundResultsForSad &&
            rd != null && rd.currentPoints != null &&
            playerIndex < rd.currentPoints.Length && anim != null)
        {
            int n = Mathf.Clamp(rd.numPlayers, 1, Mathf.Min(rd.currentPoints.Length, players.Length));
            int min, max; GetMinMax(rd.currentPoints, n, out min, out max);
            int my = rd.currentPoints[playerIndex];

            if (min == max)        Fire(anim, "Neutral");
            else if (my == max)    Fire(anim, "Happy");
            else if (my == min)    Fire(anim, "Sad");
            else                   Fire(anim, "Neutral");
            return;
        }

        // 3) Modo normal (se conserva lógica de sprites triste/normal)
        bool showSad = false;
        if (rd != null && rd.currentPoints != null && playerIndex < rd.currentPoints.Length)
        {
            int n = Mathf.Clamp(rd.numPlayers, 1, rd.currentPoints.Length);
            showSad = IsLastPlace(playerIndex, rd.currentPoints, n);
        }

        Sprite spriteToUse = showSad ? GetSafeSprite(set.sadSkinSprites, equipped)
                                     : GetSafeSprite(set.skinSprites, equipped);

        if (spriteToUse != null)
        {
            set.icon.sprite = spriteToUse;
            set.icon.enabled = true;
        }
        else
        {
            // fallback: dejamos el base que ya pusimos
            if (baseSprite != null) set.icon.enabled = true;
            else set.icon.enabled = false;
        }
    }

    // ===== Utilidades Animator =====
    private void Fire(Animator anim, string trigger)
    {
        anim.ResetTrigger("Happy");
        anim.ResetTrigger("Neutral");
        anim.ResetTrigger("Sad");
        anim.SetTrigger(trigger);
    }

    private Animator FindAnimator(Image img)
    {
        if (!img) return null;
        var a = img.GetComponent<Animator>();
        if (a) return a;
        return img.GetComponentInParent<Animator>(); // por si el Animator vive en el padre
    }

    // ===== Lógica auxiliar original =====
    private bool IsLastPlace(int playerIndex, int[] currentPoints, int numPlayers)
    {
        if (currentPoints == null || currentPoints.Length == 0) return false;

        numPlayers = Mathf.Clamp(numPlayers, 1,
                     Mathf.Min(currentPoints.Length, players != null ? players.Length : currentPoints.Length));

        int min = int.MaxValue;
        int max = int.MinValue;

        for (int i = 0; i < numPlayers; i++)
        {
            int p = currentPoints[i];
            if (p < min) min = p;
            if (p > max) max = p;
        }

        if (min == max) return false; // todos iguales
        return currentPoints[playerIndex] == min;
    }

    private HashSet<int> GetWinnersByTotal(int[] totalPoints, int numPlayers)
    {
        var winners = new HashSet<int>();
        if (totalPoints == null || totalPoints.Length == 0) return winners;

        numPlayers = Mathf.Clamp(numPlayers, 1,
                     Mathf.Min(totalPoints.Length, players != null ? players.Length : totalPoints.Length));

        int max = int.MinValue;
        for (int i = 0; i < numPlayers; i++)
            if (totalPoints[i] > max) max = totalPoints[i];

        for (int i = 0; i < numPlayers; i++)
            if (totalPoints[i] == max) winners.Add(i);

        return winners;
    }

    private void MostrarIconoTriste(int jugadorIndex)
    {
        if (jugadorIndex < 0 || jugadorIndex >= (players?.Length ?? 0)) return;
        StartCoroutine(MostrarIconoTristeTemporal(jugadorIndex, 2f));
    }

    private IEnumerator MostrarIconoTristeTemporal(int jugadorIndex, float duracion)
    {
        var set = players[jugadorIndex];
        if (set == null || set.icon == null) yield break;

        int equipped = 0;
        if (GameData.instance != null)
            equipped = Mathf.Clamp(GameData.instance.GetEquipped(jugadorIndex), 0, 9999);

        // Este evento puntual mantiene el comportamiento antiguo (cambio de sprite temporal).
        Sprite original = set.icon.sprite;
        var sad = GetSafeSprite(set.sadSkinSprites, equipped);
        if (sad != null) set.icon.sprite = sad;

        yield return new WaitForSeconds(duracion);

        // Volvemos a evaluar para respetar el modo actual (final/round/normal)
        UpdatePlayerIcon(jugadorIndex);
        if (original == null && set.icon.sprite == null)
            set.icon.enabled = false;
    }

    // ===== helpers =====
    private bool ValidPlayer(int idx) =>
        players != null && idx >= 0 && idx < players.Length && players[idx] != null && players[idx].icon != null;

    private Sprite GetSafeSprite(Sprite[] arr, int idx)
    {
        if (arr == null || idx < 0 || idx >= arr.Length) return null;
        return arr[idx];
    }

    private void GetMinMax(int[] pts, int numPlayers, out int min, out int max)
    {
        min = int.MaxValue; max = int.MinValue;
        for (int i = 0; i < numPlayers; i++)
        {
            int p = pts[i];
            if (p < min) min = p;
            if (p > max) max = p;
        }
    }
}
