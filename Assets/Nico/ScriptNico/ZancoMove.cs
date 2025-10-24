using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ZancoMove : MonoBehaviour
{
    animatorControllerCharacter acc;
    Transform posicion;
    public StaminaManager sm;

    bool puedeMover = true;
    bool llegoMeta = false;
    // Exposed max jumps and current consecutive jumps so other classes can read them
    public int saltosMax = 15;

    // saltosSeguidos is increased when the player jumps; make it public so StaminaManager can read it
    public float saltosSeguidos = 0f;

    bool isHot = false;

    float lastValidClickTime = -Mathf.Infinity;

  
    [SerializeField] private float resetDelay = 1.5f;


    [SerializeField] private float quickRecoverDelay = 0.2f;
    [SerializeField] private float quickRecoverRate = 60f;

    [SerializeField] private Image statusImage;
    [SerializeField] private Sprite caidoImagen;
    [SerializeField] private Sprite ganaImagen;
    [SerializeField] private Sprite baseImagen;

    public float numPer;


    bool inCaida = false;

    [SerializeField] AudioManagerSacos ams;

    void Start()
    {
        posicion = gameObject.GetComponent<Transform>();
        acc = GetComponent<animatorControllerCharacter>();
    }

    void Update()
    {
     
        if (sm != null)
        {
            // Make sure the StaminaManager knows which ZancoMove to read from
            if (sm.zn == null) sm.zn = this;
            sm.UpdateFromZanco();
        }

        float sinceLastClick = Time.time - lastValidClickTime;

     
        if (!inCaida && sinceLastClick >= quickRecoverDelay && saltosSeguidos > 0f)
        {
            saltosSeguidos -= quickRecoverRate * Time.deltaTime;
            if (saltosSeguidos < 0f) saltosSeguidos = 0f;
        }

 
        if (sinceLastClick >= resetDelay)
        {
            if (saltosSeguidos != 0f || inCaida)
            {
                saltosSeguidos = 0f;

                if (inCaida)
                {
                    if (acc != null) acc.recoverTrigger();
                    inCaida = false;
                }
            }
        }

        if(inCaida)
        {
            statusImage.sprite = caidoImagen;
        
        } else if (llegoMeta)
        {
            statusImage.sprite = ganaImagen; 
        
        }
        else
        {
            statusImage.sprite = baseImagen;
        
        }

  
    }


    public void LlegarMeta()
    {
        llegoMeta = true;
    }

    public void buttonGetsClicked()
    {
        if (llegoMeta) return;
        if (!puedeMover) return;

      
        if (saltosSeguidos >= saltosMax)
        {
            saltosSeguidos = 0f;
            caida();
            lastValidClickTime = Time.time;
            return;
        }


        MoveZanco();
        saltosSeguidos += 1f;
        lastValidClickTime = Time.time;
        if (acc != null) acc.jumpTrigger();
        ams.PlaySFX(ams.salto);
    }

    public void MoveZanco()
    {
        if (llegoMeta) return;
        if (!puedeMover) return;

        Vector3 nuevaPos = transform.position;
        nuevaPos.x -= 0.2f;
        transform.position = nuevaPos;
    }

    void caida()
    {
        if (acc != null) acc.fallTrigger();
        inCaida = true;
        lastValidClickTime = Time.time;
        StartCoroutine(BloquearMovimientoPorTiempo(2f));
        ams.PlaySFX(ams.Caida);


    }

    IEnumerator BloquearMovimientoPorTiempo(float segundos)
    {
        puedeMover = false;
        yield return new WaitForSeconds(segundos);
        puedeMover = true;
    }
}
