using UnityEngine;
using UnityEngine.SceneManagement;

public class ScripPruebaSonidos : MonoBehaviour
{

    public void reproducirSonido1()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx("prueba:choque");
        }
    }
    public void reproducirSonido2()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx("prueba:caida");
        }
    }
    public void cambiarEscenaPrueba()
    {
        SceneManager.LoadScene("PruebaSonidos");
    }
}
