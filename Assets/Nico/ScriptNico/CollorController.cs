using UnityEngine;

public class CollorController : MonoBehaviour
{
    SpriteRenderer sr;
    public Color blanco;
    ZancoMove zm;
    byte r = 255;
    byte g = 255;
    byte b = 255;
    byte a = 255;
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        zm = GetComponent<ZancoMove>();
    }

    public void calentando()
    {
        sr.color = new Color32(r,g,b,a);
        g -= 17;
        b -= 17;
    }

    public void frio()
    {
        sr.color = blanco;
    }
}
