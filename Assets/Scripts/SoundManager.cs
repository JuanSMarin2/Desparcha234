using UnityEngine;
using System.Collections.Generic;
using System; // agregado para eventos

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
    }

    private void CargarVolumenesPersistidos()
    {
        if (PlayerPrefs.HasKey(PREF_SFX)) sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX, sfxVolume));
        if (PlayerPrefs.HasKey(PREF_MUS)) musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_MUS, musicVolume));
        if (_musicSource != null) _musicSource.volume = musicVolume;
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
            var arr = musField.GetValue(libComp) as System.Collections.IEnumerable;
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

    // Detiene inmediatamente cualquier SFX en reproducción en el canal compartido
    public void StopSfx()
    {
        if (_sfxSource != null) _sfxSource.Stop();
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
        _musicSource.loop = loop;
        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
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
