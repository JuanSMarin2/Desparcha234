
using UnityEngine;


public class animatorControllerCharacter : MonoBehaviour
{
    Animator ac;
    void Start()
    {
        ac = GetComponent<Animator>();
    }
    public void jumpTrigger()
    {
        ac.SetTrigger("salto");
    }

    public void fallTrigger()
    {
        ac.SetTrigger("fall");
    }
    public void recoverTrigger()
    {
        ac.SetTrigger("standUp");
    }
}
