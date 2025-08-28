using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Clips")]
    [SerializeField] private AudioClip jackTocadoClip;
    [SerializeField] private AudioClip bolitaTocadaClip;
    // Nuevo: clip de error cuando la bolita toca el suelo
    [SerializeField] private AudioClip errorClip;

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource _oneShotSource;

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

        _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.playOnAwake = false;
        _oneShotSource.spatialBlend = 0f; // 2D
    }

    public void PlayOneShot(AudioClip clip, float vol = -1f)
    {
        if (clip == null || _oneShotSource == null) return;
        _oneShotSource.PlayOneShot(clip, vol < 0f ? volume : Mathf.Clamp01(vol));
    }

    public void SonidoJackTocado()
    {
        PlayOneShot(jackTocadoClip);
    }

    public void SonidoBolitaTocada()
    {
        PlayOneShot(bolitaTocadaClip);
    }

    // Nuevo: sonido cuando la bolita falla (toca el suelo)
    public void SonidoError()
    {
        PlayOneShot(errorClip);
    }
}
