using UnityEngine;
using TMPro;

public class StartTimintg : MonoBehaviour
{
    [SerializeField] GameObject[] pantalla ;
    [SerializeField] private float delaySeconds = 3f;

    [SerializeField] TMP_Text text;
    // Expose remaining and total seconds so other scripts (like Semaforo) can read the timer
    public float RemainingSeconds { get; private set; }
    public float TotalSeconds => delaySeconds;
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
            // expose current remaining time
            RemainingSeconds = remaining;

            if (text != null)
            {
                // Show seconds left as an integer (ceil to show 3..2..1)
                text.text = Mathf.CeilToInt(remaining).ToString();
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

    // Final update to show 0
    RemainingSeconds = 0f;
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
