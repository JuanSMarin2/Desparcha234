using UnityEngine;

public class SonidosUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    SoundManager sm;
    [SerializeField] private float volumen = 0.55f;
    void Start()
    {
        sm = SoundManager.instance;
    }
    public void ClickBoton()
    {
        if (sm != null) sm.PlaySfx("ui:select", volumen);
    }
}
