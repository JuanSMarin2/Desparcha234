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

    [Header("Usar resultados de la ronda para tristeza")]
    [Tooltip("Si está activo y hay RoundData.currentPoints, el último lugar de la ronda se muestra triste. Si todos empatan, nadie triste.")]
    [SerializeField] private bool useRoundResultsForSad = false;

    [Header("Escena de resultados finales")]
    [Tooltip("Si está activo, todos se muestran tristes salvo el/los ganadores según RoundData.totalPoints.")]
    [SerializeField] private bool isFinalResultsScene = false;

    // --------- Ciclo de vida ---------
    void OnEnable()
    {
        // Si no existe el evento en tu proyecto, quita estas 2 líneas.
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

    // --------- API pública util ---------
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
        if (players == null) return;
        for (int i = 0; i < players.Length; i++)
            UpdatePlayerIcon(i);
    }

    public void UpdatePlayerIcon(int playerIndex)
    {
        if (players == null || playerIndex < 0 || playerIndex >= players.Length) return;

        var set = players[playerIndex];
        if (set == null || set.icon == null) return;

        int equipped = 0;
        if (GameData.instance != null)
            equipped = Mathf.Clamp(GameData.instance.GetEquipped(playerIndex), 0, 9999);

        bool showSad = false;
        var rd = RoundData.instance;

        // 1) Escena final: todos tristes salvo ganadores por totalPoints
        if (isFinalResultsScene && rd != null && rd.totalPoints != null && rd.totalPoints.Length > 0)
        {
            HashSet<int> winners = GetWinnersByTotal(rd.totalPoints, Mathf.Clamp(rd.numPlayers, 1, rd.totalPoints.Length));
            showSad = winners.Count > 0 ? !winners.Contains(playerIndex) : false;
        }
        // 2) Ronda actual: último lugar por currentPoints (con empate nadie triste)
        else if (useRoundResultsForSad &&
                 rd != null &&
                 rd.currentPoints != null &&
                 rd.currentPoints.Length > playerIndex)
        {
            int numPlayers = Mathf.Clamp(rd.numPlayers, 1, rd.currentPoints.Length);
            showSad = IsLastPlace(playerIndex, rd.currentPoints, numPlayers);
        }
        else
        {
            showSad = false;
        }

        // Elegir sprite
        Sprite spriteToUse = null;
        if (showSad)
        {
            if (set.sadSkinSprites != null && equipped >= 0 && equipped < set.sadSkinSprites.Length)
                spriteToUse = set.sadSkinSprites[equipped];
        }
        else
        {
            if (set.skinSprites != null && equipped >= 0 && equipped < set.skinSprites.Length)
                spriteToUse = set.skinSprites[equipped];
        }

        // Fallback
        if (spriteToUse == null && set.skinSprites != null && equipped >= 0 && equipped < set.skinSprites.Length)
            spriteToUse = set.skinSprites[equipped];

        // Asignar
        if (spriteToUse != null)
        {
            set.icon.sprite = spriteToUse;
            set.icon.enabled = true;
        }
        else
        {
            set.icon.enabled = false;
        }
    }

    // --------- Lógica de “triste” por ronda ---------
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

        if (min == max) return false; // todos iguales → nadie triste

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

    // --------- Evento “papeleta destruida” triste temporal ---------
    // Llamado desde GameManagerTejo
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

        // Guardar sprite original y aplicar triste (si existe)
        Sprite spriteOriginal = set.icon.sprite;
        if (set.sadSkinSprites != null && equipped < set.sadSkinSprites.Length && set.sadSkinSprites[equipped] != null)
            set.icon.sprite = set.sadSkinSprites[equipped];

        yield return new WaitForSeconds(duracion);

        // Restaurar
        if (spriteOriginal != null)
            set.icon.sprite = spriteOriginal;
        else
            UpdatePlayerIcon(jugadorIndex); // fallback por si no había sprite
    }
}
