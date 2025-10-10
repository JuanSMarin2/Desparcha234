using UnityEngine;

public class PanelTutorial : MonoBehaviour
{
    [SerializeField] bool sonidoalDesactivar = true;
    [SerializeField] string nombreSonido;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 0f;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDisable()
    {
        Time.timeScale = 1f;
        if (sonidoalDesactivar)
        {
             var sm = FindFirstObjectByType<SoundManager>();
            if (sm != null) 
			sm.PlaySfx(nombreSonido);
        }
         
    }
}
