// IconManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IconManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIconSet
    {
        public Image icon;              // Image del jugador en la UI
        public Sprite[] skinSprites;    // Sprites normales por numero de skin
        public Sprite[] sadSkinSprites; // Sprites tristes por numero de skin
    }

    [Header("Iconos y skins por jugador (0..3)")]
    [SerializeField] private PlayerIconSet[] players = new PlayerIconSet[4];

    [Header("Usar resultados de la ronda para tristeza")]
    [Tooltip("Si esta activo y hay RoundData.currentPoints, el ultimo lugar de la ronda actual se muestra triste. Si todos empatan, nadie se muestra triste.")]
    [SerializeField] private bool useRoundResultsForSad = false;

    [Header("Escena de resultados finales")]
    [Tooltip("Si esta activo, todos se muestran tristes excepto el o los ganadores segun RoundData.totalPoints.")]
    [SerializeField] private bool isFinalResultsScene = false;

    void Start()
    {
        RefreshAllIcons();
    }

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

        // Modo resultados finales: todos tristes menos el o los ganadores por totalPoints
        if (isFinalResultsScene && rd != null && rd.totalPoints != null && rd.totalPoints.Length > 0)
        {
            HashSet<int> winners = GetWinnersByTotal(rd.totalPoints, Mathf.Clamp(rd.numPlayers, 1, rd.totalPoints.Length));
            // Si hay ganadores validos, solo ellos no estan tristes
            if (winners.Count > 0)
                showSad = !winners.Contains(playerIndex);
            else
                showSad = false; // sin datos consistentes, no marcar triste
        }
        // Modo ronda actual: ultimo lugar por currentPoints
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

        // Fallback por seguridad
        if (spriteToUse == null && set.skinSprites != null && equipped >= 0 && equipped < set.skinSprites.Length)
            spriteToUse = set.skinSprites[equipped];

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

    private bool IsLastPlace(int playerIndex, int[] currentPoints, int numPlayers)
    {
        if (currentPoints == null || currentPoints.Length == 0) return false;

        numPlayers = Mathf.Clamp(numPlayers, 1, Mathf.Min(currentPoints.Length, players != null ? players.Length : currentPoints.Length));

        int min = int.MaxValue;
        int max = int.MinValue;

        for (int i = 0; i < numPlayers; i++)
        {
            int p = currentPoints[i];
            if (p < min) min = p;
            if (p > max) max = p;
        }

        if (min == max) return false; // todos iguales, nadie triste

        int myPts = currentPoints[playerIndex];
        return myPts == min;
    }

    private HashSet<int> GetWinnersByTotal(int[] totalPoints, int numPlayers)
    {
        var winners = new HashSet<int>();
        if (totalPoints == null || totalPoints.Length == 0) return winners;

        numPlayers = Mathf.Clamp(numPlayers, 1, Mathf.Min(totalPoints.Length, players != null ? players.Length : totalPoints.Length));

        int max = int.MinValue;
        for (int i = 0; i < numPlayers; i++)
            if (totalPoints[i] > max) max = totalPoints[i];

        for (int i = 0; i < numPlayers; i++)
            if (totalPoints[i] == max) winners.Add(i);

        return winners;
    }
}
