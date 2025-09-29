using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    public ZancoMove zn;
    // current fill amount (0..1)
    public float stamina;

    public Image staminaBar;

    void Update()
    {
        // If we have a ZancoMove reference, update the UI each frame from its values
        if (zn != null)
        {
            UpdateFromZanco();
        }
    }

    // Public helper that reads values from ZancoMove and updates the UI
    public void UpdateFromZanco()
    {
        if (zn == null || staminaBar == null) return;

        // Protect against division by zero and out-of-range values
        float max = Mathf.Max(1f, zn.saltosMax);
        float used = Mathf.Clamp(zn.saltosSeguidos, 0f, max);

        // We want the bar to start full and decrease as 'used' increases
        float percent = 1f - (used / max);

        stamina = Mathf.Clamp01(percent);
        staminaBar.fillAmount = stamina;
    }

}
