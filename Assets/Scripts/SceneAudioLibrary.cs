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

    [System.Serializable]
    public struct NamedVolume
    {
        public string key;   // nombre local (sin gameId)
        [Range(0f,1f)] public float volumeScale; // 1 = volumen tal cual; <1 baja volumen
    }

    [Header("Identificador (prefijo opcional)")]
    public string gameId; // Ej: "Jacks", "Tejo", etc.

    [Header("Clips SFX")] public NamedClip[] sfxClips;
    [Header("Clips Música")] public NamedClip[] musicClips;

    [Header("Overrides de Volumen Música (opcional)")]
    [Tooltip("Escalas de volumen por canción (0..1). La clave debe coincidir con la de musicClips (local, sin prefijo).")]
    [SerializeField] private NamedVolume[] musicVolumeOverrides;

    [Header("Selección de Música (nuevo)")]
    [Tooltip("Si está activo, al iniciar se elegirá aleatoriamente una canción del arreglo 'musicaAleatoria'. Si está desactivado y 'musicaFija' tiene valor, se usará esa canción fija.")]
    [SerializeField] private bool musicaRandom = false;
    [Tooltip("Pool de canciones para selección aleatoria cuando musicaRandom es true")] 
    [SerializeField] private AudioClip[] musicaAleatoria;
    [Tooltip("Canción fija a reproducir cuando musicaRandom es false")] 
    [SerializeField] private AudioClip musicaFija;

    [Tooltip("Reproducir automáticamente esta música al cargar la escena (usar key local sin prefijo)")]
    public string autoPlayMusicKey;
    public bool autoLoopMusic = true;

    [Header("Persistencia Opcional")]
    [Tooltip("Si está activo, este contenedor NO se destruye al cambiar de escena y sólo registra sus clips una vez.")]
    [SerializeField] private bool persistAcrossScenes = false;

    // Guard para no registrar/autoplay dos veces cuando es persistente
    private bool _alreadyRegisteredPersistent = false;

    private const string AutoMusicLocalKey = "__autoMusic";

    private void Awake()
    {
        if (persistAcrossScenes)
        {
            // Garantizar que sobreviva a los loads de escena
            DontDestroyOnLoad(gameObject);
        }

        // Preparar selección de música según flags
        AudioClip selected = null;
        if (musicaRandom && musicaAleatoria != null && musicaAleatoria.Length > 0)
        {
            int idx = Random.Range(0, musicaAleatoria.Length);
            selected = musicaAleatoria[idx];
        }
        else if (!musicaRandom && musicaFija != null)
        {
            selected = musicaFija;
        }

        if (selected != null)
        {
            AddOrReplaceAutoMusicClip(selected);
            // Configurar para que el sistema de autoplay utilice esta clave
            autoPlayMusicKey = AutoMusicLocalKey;
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

                // Si se usa la selección (random/fija) dejamos que SoundManager.Start haga el autoplay
                bool usingSelection = musicaRandom || (musicaFija != null);
                if (!usingSelection && !string.IsNullOrWhiteSpace(autoPlayMusicKey))
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

    // Inserta o reemplaza una entrada de música con clave interna para autoplay
    private void AddOrReplaceAutoMusicClip(AudioClip clip)
    {
        if (clip == null) return;
        if (musicClips == null) musicClips = new NamedClip[0];

        int found = -1;
        for (int i = 0; i < musicClips.Length; i++)
        {
            if (musicClips[i].key == AutoMusicLocalKey)
            {
                found = i; break;
            }
        }
        if (found >= 0)
        {
            musicClips[found] = new NamedClip { key = AutoMusicLocalKey, clip = clip };
        }
        else
        {
            var newArr = new NamedClip[musicClips.Length + 1];
            // Insertamos al inicio para precedencia visual
            newArr[0] = new NamedClip { key = AutoMusicLocalKey, clip = clip };
            for (int i = 0; i < musicClips.Length; i++) newArr[i + 1] = musicClips[i];
            musicClips = newArr;
        }
    }
}
