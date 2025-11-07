using UnityEngine;
using UnityEngine.UI;
using System;

// IconManagerGeneral
// Determina qué íconos (feliz/neutral/triste) usar para cada jugador (1..4) según si tiene una skin equipada.
// - Define sprites por defecto (sin skin) y colecciones por skin opcionalmente.
// - Al inicio de la escena consulta GameData.GetEquipped(i) y resuelve los íconos elegidos.
// - Otros scripts pueden obtener los sprites resueltos mediante GetHappy/GetNeutral/GetSad o ApplyToImage.
[DisallowMultipleComponent]
public class IconManagerGeneral : MonoBehaviour
{
    public enum Mood { Happy, Neutral, Sad }

    [Serializable]
    public class PlayerSkinSet
    {
        [Tooltip("Sprites FELICES por skin para este jugador (índice = id de skin - 1)")] public Sprite[] happyBySkin;
        [Tooltip("Sprites NEUTRALES por skin para este jugador (índice = id de skin - 1)")] public Sprite[] neutralBySkin;
        [Tooltip("Sprites TRISTES por skin para este jugador (índice = id de skin - 1)")] public Sprite[] sadBySkin;
    }

    public static IconManagerGeneral Instance { get; private set; }

    [Header("Defaults por jugador (1..4) - sin skin")]
    [Tooltip("Sprite por defecto (feliz) por jugador 1..4")] public Sprite[] defaultHappy = new Sprite[4];
    [Tooltip("Sprite por defecto (neutral) por jugador 1..4")] public Sprite[] defaultNeutral = new Sprite[4];
    [Tooltip("Sprite por defecto (triste) por jugador 1..4")] public Sprite[] defaultSad = new Sprite[4];

    [Header("Sprites por jugador si hay skin equipada")]
    [Tooltip("Cada entrada corresponde a un jugador; si hay solo 1 entrada, se usa para todos")] public PlayerSkinSet[] players = new PlayerSkinSet[1];

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // Resultado resuelto al inicio (lo que usarán otros scripts)
    private Sprite[] _resolvedHappy = new Sprite[4];
    private Sprite[] _resolvedNeutral = new Sprite[4];
    private Sprite[] _resolvedSad = new Sprite[4];

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Garantizar GameData para leer equipamiento
        var gd = GameData.EnsureInstance();
        if (gd == null && debugLogs) Debug.LogWarning("[IconManagerGeneral] GameData.EnsureInstance devolvió null");
        ResolveAll();
    }

    private void OnValidate()
    {
        // Mantener arrays en tamaño 4
        EnsureSize(ref defaultHappy, 4);
        EnsureSize(ref defaultNeutral, 4);
        EnsureSize(ref defaultSad, 4);
    }

    private void EnsureSize(ref Sprite[] arr, int size)
    {
        if (arr == null) arr = new Sprite[size];
        else if (arr.Length != size) Array.Resize(ref arr, size);
    }

    private void ResolveAll()
    {
        for (int i = 0; i < 4; i++)
        {
            int equippedRaw = (GameData.instance != null) ? GameData.instance.GetEquipped(i) : 0; // 0 = sin skin
            bool useSkin = equippedRaw > 0;
            int skinIdx = Mathf.Max(0, equippedRaw - 1); // map 1..N -> 0..N-1

            // FELIZ
            _resolvedHappy[i] = useSkin ? (GetSpriteForPlayer(i, Mood.Happy, skinIdx) ?? defaultHappy[i]) : defaultHappy[i];
            // NEUTRAL
            _resolvedNeutral[i] = useSkin ? (GetSpriteForPlayer(i, Mood.Neutral, skinIdx) ?? defaultNeutral[i]) : defaultNeutral[i];
            // TRISTE
            _resolvedSad[i] = useSkin ? (GetSpriteForPlayer(i, Mood.Sad, skinIdx) ?? (defaultSad[i] ? defaultSad[i] : defaultHappy[i]))
                                      : (defaultSad[i] ? defaultSad[i] : defaultHappy[i]);

            if (debugLogs)
            {
                string src = useSkin ? $"skin#{skinIdx+1}" : "default";
                Debug.Log($"[IconManagerGeneral] P{i+1} -> {src} (H:{_resolvedHappy[i]?.name ?? "null"}, N:{_resolvedNeutral[i]?.name ?? "null"}, S:{_resolvedSad[i]?.name ?? "null"})");
            }
        }
    }

    // Resolved getters (0-based index)
    public Sprite GetHappy(int playerIndexZeroBased) => SafePick(_resolvedHappy, playerIndexZeroBased);
    public Sprite GetNeutral(int playerIndexZeroBased) => SafePick(_resolvedNeutral, playerIndexZeroBased);
    public Sprite GetSad(int playerIndexZeroBased) => SafePick(_resolvedSad, playerIndexZeroBased);

    public bool ApplyToImage(Image img, int playerIndexZeroBased, Mood mood)
    {
        if (!img) return false;
        Sprite s = null;
        switch (mood)
        {
            case Mood.Happy: s = GetHappy(playerIndexZeroBased); break;
            case Mood.Neutral: s = GetNeutral(playerIndexZeroBased); break;
            case Mood.Sad: s = GetSad(playerIndexZeroBased); break;
        }
        if (!s) return false;
        img.sprite = s; img.enabled = true; return true;
    }

    private Sprite GetSpriteForPlayer(int playerIndexZeroBased, Mood mood, int skinIdx)
    {
        if (players == null || players.Length == 0) return null;
        int idxPlayer = (players.Length == 1) ? 0 : Mathf.Clamp(playerIndexZeroBased, 0, players.Length - 1);
        var set = players[idxPlayer]; if (set == null) return null;
        Sprite s = null;
        switch (mood)
        {
            case Mood.Happy:
                if (set.happyBySkin != null && skinIdx >= 0 && skinIdx < set.happyBySkin.Length) s = set.happyBySkin[skinIdx];
                break;
            case Mood.Neutral:
                if (set.neutralBySkin != null && skinIdx >= 0 && skinIdx < set.neutralBySkin.Length) s = set.neutralBySkin[skinIdx];
                break;
            case Mood.Sad:
                if (set.sadBySkin != null && skinIdx >= 0 && skinIdx < set.sadBySkin.Length) s = set.sadBySkin[skinIdx];
                break;
        }
        return s;
    }

    private Sprite SafePick(Sprite[] arr, int idx0)
    {
        if (arr == null || idx0 < 0 || idx0 >= arr.Length) return null;
        return arr[idx0];
    }
}
