using UnityEngine;
using System.Collections.Generic;
using System; // agregado para eventos
using System.Collections; // para IEnumerators locales
using UnityEngine.SceneManagement; // para detectar cargas de escena

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Volúmenes Globales")]
    [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float musicVolume = 1f;

    [Header("Config Música")] [SerializeField] private bool musicLoopDefault = true;

    private AudioSource _sfxSource;      // disparos one-shot (SFX)
    private AudioSource _musicSource;    // canal dedicado música (loop)

    // Diccionarios dinámicos (nombre -> clip)
    private readonly Dictionary<string, AudioClip> _sfxClips = new();
    private readonly Dictionary<string, AudioClip> _musicClips = new();

    // Rastreo de propiedad (para limpiar al salir de escena)
    private readonly Dictionary<object, List<string>> _ownerSfxKeys = new();
    private readonly Dictionary<object, List<string>> _ownerMusicKeys = new();

    private readonly HashSet<string> _warnedMissing = new();

    // NUEVO: rate limiting por clave
    private readonly Dictionary<string, float> _sfxLastPlayTime = new();

    // Eventos para UI cuando cambian los volúmenes
    public static event Action<float> OnSfxVolumeChanged;
    public static event Action<float> OnMusicVolumeChanged;

    // Getters públicos de solo lectura
    public float SfxVolume => sfxVolume;
    public float MusicVolume => musicVolume;

    private const string PREF_SFX = "SFX_VOL";
    private const string PREF_MUS = "MUS_VOL";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        gameObject.name = "SoundManager"; // name for SendMessage lookup

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f; // 2D

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = musicLoopDefault;
        _musicSource.spatialBlend = 0f;

        CargarVolumenesPersistidos();

        // Suscribirse a eventos de carga de escena para re-escanear librerías
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Limpiar suscripción al destruirse (por seguridad)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Auto-registro de todas las bibliotecas presentes en la escena inicial (ej. menú)
        var libs = FindObjectsByType<SceneAudioLibrary>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (libs != null)
        {
            var seen = new HashSet<SceneAudioLibrary>();
            foreach (var lib in libs)
            {
                if (lib == null || seen.Contains(lib)) continue;
                seen.Add(lib);
                RegisterBatch(lib);
                if (!string.IsNullOrWhiteSpace(lib.autoPlayMusicKey))
                {
                    string local = lib.autoPlayMusicKey;
                    // Formar clave completa igual que BuildKey interno del library
                    string full = string.IsNullOrWhiteSpace(lib.gameId) || local.Contains(":") ? local.Trim() : lib.gameId.Trim() + ":" + local.Trim();
                    PlayMusic(full, lib.autoLoopMusic);
                }
            }
            if (libs.Length > 0) Debug.Log($"[SoundManager] Auto-registradas {libs.Length} SceneAudioLibrary al iniciar.");
        }

        // Además de la ejecución inicial, asegurar un escaneo explícito por si alguna SceneAudioLibrary
        // no fue incluida vía FindObjectsByType en plataformas antiguas: reutilizamos la misma rutina.
        ScanAndRegisterSceneAudioLibraries();
    }

    private void CargarVolumenesPersistidos()
    {
        if (PlayerPrefs.HasKey(PREF_SFX)) sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX, sfxVolume));
        if (PlayerPrefs.HasKey(PREF_MUS)) musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_MUS, musicVolume));
        if (_musicSource != null) _musicSource.volume = musicVolume;
    }

    // Nuevo: handler que se ejecuta tras cada carga de escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SoundManager] Escaneando SceneAudioLibrary tras cargar escena: {scene.name}");
        ScanAndRegisterSceneAudioLibraries();
    }

    // Nuevo: re-escanea las SceneAudioLibrary de la escena y registra sus clips; reproduce autoPlayMusicKey si existe
    private void ScanAndRegisterSceneAudioLibraries()
    {
        var libs = FindObjectsByType<SceneAudioLibrary>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (libs == null || libs.Length == 0) return;
        var seen = new HashSet<SceneAudioLibrary>();
        foreach (var lib in libs)
        {
            if (lib == null || seen.Contains(lib)) continue;
            seen.Add(lib);
            RegisterBatch(lib);
            if (!string.IsNullOrWhiteSpace(lib.autoPlayMusicKey))
            {
                string local = lib.autoPlayMusicKey;
                string full = string.IsNullOrWhiteSpace(lib.gameId) || local.Contains(":") ? local.Trim() : lib.gameId.Trim() + ":" + local.Trim();
                // Reproducir la música indicada (PlayMusic maneja reemplazar el clip actual en _musicSource)
                PlayMusic(full, lib.autoLoopMusic);
            }
        }
        Debug.Log($"[SoundManager] ScanAndRegisterSceneAudioLibraries -> registradas {libs.Length} SceneAudioLibrary(s).");
    }

    // ================== REGISTRO DINÁMICO ==================
    public void RegisterClip(object owner, string key, AudioClip clip, bool music = false)
    {
        if (string.IsNullOrWhiteSpace(key) || clip == null) return;
        key = key.Trim();
        if (music)
        {
            _musicClips[key] = clip;
            if (!_ownerMusicKeys.TryGetValue(owner, out var list)) { list = new List<string>(); _ownerMusicKeys[owner] = list; }
            if (!list.Contains(key)) list.Add(key);
        }
        else
        {
            _sfxClips[key] = clip;
            if (!_ownerSfxKeys.TryGetValue(owner, out var list)) { list = new List<string>(); _ownerSfxKeys[owner] = list; }
            if (!list.Contains(key)) list.Add(key);
        }
    }

    // Reflection-based batch registration to avoid hard compile dependency on SceneAudioLibrary in this analysis tool.
    // In Unity runtime this still finds the real fields.
    public void RegisterBatch(Component libComp)
    {
        if (libComp == null) return;
        var t = libComp.GetType();
        var gameIdField = t.GetField("gameId");
        string gameId = gameIdField != null ? gameIdField.GetValue(libComp) as string : null;

        // Helper local function
        string Qualify(string local)
        {
            if (string.IsNullOrWhiteSpace(local)) return null;
            local = local.Trim();
            if (string.IsNullOrWhiteSpace(gameId) || local.Contains(":")) return local;
            return gameId.Trim() + ":" + local;
        }

        // SFX array
        var sfxField = t.GetField("sfxClips");
        if (sfxField != null)
        {
            var arr = sfxField.GetValue(libComp) as System.Collections.IEnumerable;
            if (arr != null)
            {
                foreach (var elem in arr)
                {
                    if (elem == null) continue;
                    var et = elem.GetType();
                    var keyF = et.GetField("key");
                    var clipF = et.GetField("clip");
                    if (keyF == null || clipF == null) continue;
                    string localKey = keyF.GetValue(elem) as string;
                    var clip = clipF.GetValue(elem) as AudioClip;
                    if (clip == null) continue;
                    RegisterClip(libComp, Qualify(localKey), clip, false);
                }
            }
        }

        // Music array
        var musField = t.GetField("musicClips");
        if (musField != null)
        {
            var arr = musField.GetValue(libComp) as IEnumerable;
            if (arr != null)
            {
                foreach (var elem in arr)
                {
                    if (elem == null) continue;
                    var et = elem.GetType();
                    var keyF = et.GetField("key");
                    var clipF = et.GetField("clip");
                    if (keyF == null || clipF == null) continue;
                    string localKey = keyF.GetValue(elem) as string;
                    var clip = clipF.GetValue(elem) as AudioClip;
                    if (clip == null) continue;
                    RegisterClip(libComp, Qualify(localKey), clip, true);
                }
            }
        }
    }

    public void UnregisterBatch(Component libComp)
    {
        if (libComp == null) return;
        if (_ownerSfxKeys.TryGetValue(libComp, out var sfxList))
        {
            foreach (var k in sfxList) _sfxClips.Remove(k);
            _ownerSfxKeys.Remove(libComp);
        }
        if (_ownerMusicKeys.TryGetValue(libComp, out var musList))
        {
            foreach (var k in musList) _musicClips.Remove(k);
            _ownerMusicKeys.Remove(libComp);
        }
    }

    // ================== PLAYBACK SFX ==================
    public void PlaySfx(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!_sfxClips.TryGetValue(key, out var clip))
        {
            if (!_warnedMissing.Contains(key)) { Debug.LogWarning($"[SoundManager] SFX key '{key}' no encontrado."); _warnedMissing.Add(key); }
            return;
        }
        if (clip != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
        }
    }

    // NUEVO: intentar reproducir SFX sin warnings si falta (devuelve true si se reprodujo)
    public bool TryPlaySfx(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (!_sfxClips.TryGetValue(key, out var clip) || clip == null || _sfxSource == null) return false;
        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
        return true;
    }

    // NUEVO: reproducción con rate limit por clave (segundos entre reproducciones). Devuelve true si disparó.
    public bool PlaySfxRateLimited(string key, float minInterval, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        float now = Time.unscaledTime;
        if (_sfxLastPlayTime.TryGetValue(key, out var last))
        {
            if (now - last < Mathf.Max(0f, minInterval)) return false;
        }
        _sfxLastPlayTime[key] = now;
        PlaySfx(key, volumeScale);
        return true;
    }

    // Detiene inmediatamente cualquier SFX en reproducción en el canal compartido
    public void StopSfx()
    {
        if (_sfxSource != null) _sfxSource.Stop();
    }

    // Dispara un SFX cuando el juego esté "despausado" (timeScale >= 1)
    // Útil para que los sonidos de salida de pantallas/gates ocurran al reanudar.
    public void PlaySfxWhenUnpaused(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        StartCoroutine(CoPlaySfxWhenUnpaused(key, volumeScale));
    }

    private IEnumerator CoPlaySfxWhenUnpaused(string key, float volumeScale)
    {
        // Esperar a que timeScale sea 1 (o mayor) usando frames en tiempo real (no dependemos de WaitForSeconds)
        while (Time.timeScale < 1f)
            yield return null; // avanza por frame aunque timeScale=0
        PlaySfx(key, volumeScale);
    }

    // ================== PLAYBACK MÚSICA ==================
    public void PlayMusic(string key, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (!_musicClips.TryGetValue(key, out var clip))
        {
            if (!_warnedMissing.Contains(key)) { Debug.LogWarning($"[SoundManager] Music key '{key}' no encontrado."); _warnedMissing.Add(key); }
            return;
        }
        if (_musicSource == null || clip == null) return;

        // Si es el mismo clip que ya está asignado al AudioSource, no reiniciarlo para que continúe donde iba.
        if (_musicSource.clip == clip)
        {
            // Actualizar propiedades pero evitar Play() si ya está sonando (no reiniciar desde 0).
            _musicSource.loop = loop;
            _musicSource.volume = musicVolume;
            if (!_musicSource.isPlaying)
            {
                // Si no está reproduciéndose (detenido/pausado), iniciarlo ahora.
                _musicSource.Play();
            }
            return;
        }

        // Clip distinto: reemplazar y reproducir desde el inicio
        _musicSource.loop = loop;
        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    // Inicia música cuando el juego vuelva a estar despausado
    public void PlayMusicWhenUnpaused(string key, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        StartCoroutine(CoPlayMusicWhenUnpaused(key, loop));
    }

    private IEnumerator CoPlayMusicWhenUnpaused(string key, bool loop)
    {
        while (Time.timeScale < 1f)
            yield return null;
        PlayMusic(key, loop);
    }

    public void StopMusic()
    {
        if (_musicSource != null) _musicSource.Stop();
    }

    public void SetSfxVolume(float v) 
    { 
        sfxVolume = Mathf.Clamp01(v); 
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume); 
        OnSfxVolumeChanged?.Invoke(sfxVolume); 
    }

    public void SetMusicVolume(float v) 
    { 
        musicVolume = Mathf.Clamp01(v); 
        if (_musicSource != null) _musicSource.volume = musicVolume; 
        PlayerPrefs.SetFloat(PREF_MUS, musicVolume); 
        OnMusicVolumeChanged?.Invoke(musicVolume); 
    }
}
