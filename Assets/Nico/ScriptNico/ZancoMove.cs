using UnityEngine;
using System.Collections;

public class ZancoMove : MonoBehaviour
{
    animatorControllerCharacter acc;
    Transform posicion;
    bool puedeMover = true;
    bool llegoMeta = false; // Nueva variable de estado
    int saltosMax = 15;
    int saltosSeguidos = 0;
    CollorController cc;
    bool isHot = false;

    // tiempo del último click válido y delay para reset
    float lastValidClickTime = -Mathf.Infinity;
    const float resetDelay = 1.5f;
    public float numPer;

    // indicador de que hubo una caída y estamos esperando el reseteo asociado
    bool inCaida = false;

    void Start()
    {
        posicion = gameObject.GetComponent<Transform>();
        cc = GetComponent<CollorController>();
        acc = GetComponent<animatorControllerCharacter>();
    }

    void Update()
    {
        // Primero comprobar si ya pasó el resetDelay desde el último click válido.
        // Si venimos de una caída (inCaida == true) llamaremos recoverTrigger() cuando se resetee.
        if (Time.time - lastValidClickTime >= resetDelay)
        {
            // Sólo ejecutar acciones de reseteo si algo cambia o si venimos de una caída pendiente.
            if (saltosSeguidos != 0 || inCaida)
            {
                saltosSeguidos = 0;
                if (cc != null) cc.frio();

                if (inCaida)
                {
                    if (acc != null) acc.recoverTrigger(); // <-- llamamos recoverTrigger() tras la caída
                    inCaida = false;
                }

                // opcional: Debug.Log("Reset saltosSeguidos por inactividad o tras caida");
            }
        }

        // Si no hay saltos acumulados, asegurar color frio
        if (saltosSeguidos == 0)
        {
            if (cc != null) cc.frio();
        }

        Debug.Log(saltosSeguidos);
    }

    // Llama este método cuando llegue a la meta
    public void LlegarMeta()
    {
        llegoMeta = true;
    }

    public void buttonGetsClicked()
    {
        // Ignorar clicks si está bloqueado por caída o ya llegó a la meta
        if (llegoMeta) return;
        if (!puedeMover) return;

        // Si alcanza el máximo, iniciar caída y no incrementar saltos
        if (saltosSeguidos >= saltosMax)
        {
            saltosSeguidos = 0;
            caida();
            if (cc != null) cc.frio();
            lastValidClickTime = Time.time; // marca el inicio del periodo tras la caída
            return;
        }

        // Click válido: mover, calentar, incrementar y registrar tiempo válido
        MoveZanco();
        if (cc != null) cc.calentando();
        saltosSeguidos += 1;
        lastValidClickTime = Time.time;
        if (acc != null) acc.jumpTrigger();
    }

    public void MoveZanco()
    {
        if (llegoMeta) return; // Si llegó a la meta, no se mueve
        if (!puedeMover) return;

        Vector3 nuevaPos = transform.position;
        nuevaPos.x -= 0.2f;
        transform.position = nuevaPos;
    }

    void caida()
    {
        Debug.Log("Caida");
        if (acc != null) acc.fallTrigger();
        // marcar que hubo una caída para invocar recoverTrigger() cuando se haga el reset
        inCaida = true;
        // asegurar que el conteo de inactividad arranca desde ahora
        lastValidClickTime = Time.time;
        StartCoroutine(BloquearMovimientoPorTiempo(2f));
    }

    IEnumerator BloquearMovimientoPorTiempo(float segundos)
    {
        puedeMover = false;
        yield return new WaitForSeconds(segundos);
        puedeMover = true;
    }
}
