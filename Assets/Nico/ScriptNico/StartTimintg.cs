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
        float remaining = delaySeconds;
        // Update the text each frame until the delay elapses
        while (remaining > 0f)
        {
            if (text != null)
            {
                // Show seconds left as an integer (ceil to show 3..2..1)
                text.text = Mathf.CeilToInt(remaining).ToString();
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        // Final update to show 0
        if (text != null) text.text = "0";

        if (pantalla != null)
        {
            for (int i = 0; i < pantalla.Length; i++)
            {
                if (pantalla[i] != null)
                    pantalla[i].SetActive(false);
            }
        }
    }
}
