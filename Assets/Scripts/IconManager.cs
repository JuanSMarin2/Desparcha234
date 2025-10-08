// IconManager.cs
using UnityEngine;
using UnityEngine.UI;

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
    [Tooltip("Si está activo, y hay RoundData.currentPoints, el jugador que quede último mostrará su ícono 'triste'. Si todos empatan, nadie está triste.")]
    [SerializeField] private bool useRoundResultsForSad = false;

    void Start()
    {
        RefreshAllIcons();
    }

    public void SetUseRoundResults(bool value)
    {
        useRoundResultsForSad = value;
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

        if (useRoundResultsForSad &&
            RoundData.instance != null &&
            RoundData.instance.currentPoints != null &&
            RoundData.instance.currentPoints.Length > playerIndex)
        {
            int numPlayers = Mathf.Clamp(RoundData.instance.numPlayers, 1, RoundData.instance.currentPoints.Length);
            showSad = IsLastPlace(playerIndex, RoundData.instance.currentPoints, numPlayers);
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
}
