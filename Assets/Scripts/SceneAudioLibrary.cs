using UnityEngine;

[DisallowMultipleComponent]
public class SceneAudioLibrary : MonoBehaviour
{
    [System.Serializable]
    public struct NamedClip
    {
        public string key;   // nombre local (sin gameId)
        public AudioClip clip;
    }

    [Header("Identificador (prefijo opcional)")]
    public string gameId; // Ej: "Jacks", "Tejo", etc.

    [Header("Clips SFX")] public NamedClip[] sfxClips;
    [Header("Clips Música")] public NamedClip[] musicClips;

    [Tooltip("Reproducir automáticamente esta música al cargar la escena (usar key local sin prefijo)")]
    public string autoPlayMusicKey;
    public bool autoLoopMusic = true;

    private void OnEnable()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.RegisterBatch(this);
            if (!string.IsNullOrWhiteSpace(autoPlayMusicKey))
            {
                string full = BuildKey(autoPlayMusicKey);
                SoundManager.instance.PlayMusic(full, autoLoopMusic);
            }
        }
    }

    private void OnDisable()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.UnregisterBatch(this);
        }
    }

    private string BuildKey(string local)
    {
        if (string.IsNullOrWhiteSpace(local)) return local;
        if (string.IsNullOrWhiteSpace(gameId)) return local.Trim();
        if (local.Contains(":")) return local; // ya formado
        return gameId.Trim() + ":" + local.Trim();
    }
}
