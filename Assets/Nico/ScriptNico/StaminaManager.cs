using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    public ZancoMove zn;
    public float stamina;

    public Image staminaBar;
    float staminaPercent;
    void Update()
    {
        
    }

    public void setStamina(float actualStamina)
    {
        staminaPercent = actualStamina / 15;
        stamina = staminaPercent;
        staminaBar.fillAmount = stamina;

    }

}
