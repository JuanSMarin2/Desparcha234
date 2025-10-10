using UnityEngine;

public class AudioManagerSacos : MonoBehaviour
{
    [Header("-----Audio Source-----")]
    [SerializeField] AudioSource SFXsource;

    [Header("-----Audio Clip-----")]
    public AudioClip salto;
    public AudioClip Caida;
    public AudioClip Ganar;

    public void PlaySFX(AudioClip clip)
    {
        SFXsource.PlayOneShot(clip);
    }
}
