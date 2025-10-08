using UnityEngine;

public class SonidosUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    SoundManager sm;
    void Start()
    {
        sm = SoundManager.instance;
    }
    public void ClickBoton()
    {
        if (sm != null) sm.PlaySfx("ui:select", 0.75f);
    }
}
