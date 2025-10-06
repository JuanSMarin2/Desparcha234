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

    [Header("Persistencia Opcional")]
    [Tooltip("Si está activo, este contenedor NO se destruye al cambiar de escena y sólo registra sus clips una vez.")]
    [SerializeField] private bool persistAcrossScenes = false;

    // Guard para no registrar/autoplay dos veces cuando es persistente
    private bool _alreadyRegisteredPersistent = false;

    private void Awake()
    {
        if (persistAcrossScenes)
        {
            // Garantizar que sobreviva a los loads de escena
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (SoundManager.instance != null)
        {
            // Evitar doble registro si es persistente y ya se registró antes
            if (!persistAcrossScenes || !_alreadyRegisteredPersistent)
            {
                SoundManager.instance.RegisterBatch(this);
                if (!string.IsNullOrWhiteSpace(autoPlayMusicKey))
                {
                    string full = BuildKey(autoPlayMusicKey);
                    SoundManager.instance.PlayMusic(full, autoLoopMusic);
                }
                if (persistAcrossScenes)
                {
                    _alreadyRegisteredPersistent = true;
                    Debug.Log($"[SceneAudioLibrary] Registro persistente inicial de '{gameId}' completado.");
                }
            }
            else
            {
                // Omitimos registro repetido (clips ya están en diccionario)
                //Debug.Log($"[SceneAudioLibrary] '{gameId}' persistente ya registrado previamente: se omite.");
            }
        }
    }

    private void OnDisable()
    {
        if (SoundManager.instance != null)
        {
            // Bibliotecas persistentes mantienen sus clips incluso si se desactivan manualmente (evita cortes).
            if (persistAcrossScenes)
            {
                return; // no desregistrar
            }
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
