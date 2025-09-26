// IconManager.cs
using UnityEngine;
using UnityEngine.UI;

public class IconManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIconSet
    {
        public Image icon;            // Image del jugador
        public Sprite[] skinSprites;  // Sprites de ese jugador por número de skin
    }

    [Header("Iconos y skins por jugador (0..3)")]
    [SerializeField] private PlayerIconSet[] players = new PlayerIconSet[4];

    void Start()
    {
        RefreshAllIcons();
    }

    public void RefreshAllIcons()
    {
        if (GameData.instance == null) return;

        for (int i = 0; i < players.Length; i++)
        {
            UpdatePlayerIcon(i);
        }
    }

    public void UpdatePlayerIcon(int playerIndex)
    {
        if (GameData.instance == null) return;
        if (playerIndex < 0 || playerIndex >= players.Length) return;

        var set = players[playerIndex];
        if (set == null || set.icon == null || set.skinSprites == null || set.skinSprites.Length == 0) return;

        int skin = GameData.instance.GetEquipped(playerIndex);
        if (skin < 0 || skin >= set.skinSprites.Length) return;

        set.icon.sprite = set.skinSprites[skin];
        set.icon.enabled = true;
    }
}
