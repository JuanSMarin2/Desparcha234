using UnityEngine;
using UnityEngine.UI;

public class PowerUpUIManager : MonoBehaviour
{
    public static PowerUpUIManager instance;

    [Header("Referencias UI (1 por jugador)")]
    [SerializeField] private Image[] playerPowerImages = new Image[4];

    [Header("Sprites para poderes (en orden: MorePower, Unmovable, Ghost)")]
    [SerializeField] private Sprite spriteMorePower;
    [SerializeField] private Sprite spriteUnmovable;
    [SerializeField] private Sprite spriteGhost;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Asegurarse de que arranquen ocultos
        foreach (var img in playerPowerImages)
        {
            if (img != null) img.gameObject.SetActive(false);
        }
    }

    public void SetPlayerPower(int playerIndex, MarblePowerType type)
    {
        if (playerIndex < 0 || playerIndex >= playerPowerImages.Length) return;
        var img = playerPowerImages[playerIndex];
        if (img == null) return;

        if (type == MarblePowerType.None)
        {
            img.gameObject.SetActive(false);
            return;
        }

        // Elegir sprite según poder
        switch (type)
        {
            case MarblePowerType.MorePower: img.sprite = spriteMorePower; break;
            case MarblePowerType.Unmovable: img.sprite = spriteUnmovable; break;
            case MarblePowerType.Ghost: img.sprite = spriteGhost; break;
        }

        img.gameObject.SetActive(true);
    }
}
