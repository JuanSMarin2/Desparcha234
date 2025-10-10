using UnityEngine;

public class PanelTutorial : MonoBehaviour
{
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
    }
}
