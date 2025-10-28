using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Fondo por jugador (solo escena minijuegos)")]
    [Tooltip("Imagen de fondo a cambiar según el jugador activo")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Sprites por jugador (índice 0 = Jugador 1, 1 = Jugador 2, ...)")]
    [SerializeField] private Sprite[] backgroundsByPlayer;
    [Tooltip("Sprite por defecto si falta el del jugador actual")]
    [SerializeField] private Sprite defaultBackground;
    private int _lastAppliedIndex = int.MinValue;

    void Start()
    {
        // Aplicar inmediatamente el fondo inicial según el jugador actual
        ApplyBackgroundImmediate(GetCurrentPlayerIndexSafe());
    }

    void Update()
    {
        // Detectar cambios de jugador y actualizar fondo instantáneamente
        int idx = GetCurrentPlayerIndexSafe();
        if (idx != _lastAppliedIndex)
        {
            ApplyBackgroundImmediate(idx);
        }
    }

    private int GetCurrentPlayerIndexSafe()
    {
        return TurnManager.instance != null ? TurnManager.instance.GetCurrentPlayerIndex() : -1;
    }

    private void ApplyBackgroundImmediate(int playerIndex)
    {
        if (backgroundImage == null) return;
        var sprite = GetSpriteForPlayer(playerIndex);
        backgroundImage.sprite = sprite;
        // Asegurar alpha completo al inicio
        var c = backgroundImage.color;
        c.a = 1f;
        backgroundImage.color = c;
        _lastAppliedIndex = playerIndex;
    }

    private Sprite GetSpriteForPlayer(int playerIndex)
    {
        if (backgroundsByPlayer != null && playerIndex >= 0 && playerIndex < backgroundsByPlayer.Length)
        {
            var s = backgroundsByPlayer[playerIndex];
            if (s != null) return s;
        }
        return defaultBackground;
    }
}
