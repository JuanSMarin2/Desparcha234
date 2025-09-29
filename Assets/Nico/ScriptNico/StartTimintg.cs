using UnityEngine;
using TMPro;

public class StartTimintg : MonoBehaviour
{
    [SerializeField] GameObject[] pantalla ;
    [SerializeField] private float delaySeconds = 3f;

    [SerializeField] TMP_Text text;
    void Start()
    {
        StartCoroutine(DisableAfterDelay());
    }

    private System.Collections.IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        if (pantalla != null)
        for (int i = 0; i < pantalla.Length; i++)
        {
            pantalla[i].SetActive(false);
        }
            
    }
}
