using UnityEngine;
using System.Collections;

public class ZancoMove : MonoBehaviour
{
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

    void Start()
    {
        posicion = gameObject.GetComponent<Transform>();
        cc = GetComponent<CollorController>();
    }

    void Update()
    {
        // Si no hay saltos acumulados, asegurar color frio
        if (saltosSeguidos == 0)
        {
            if (cc != null) cc.frio();
            return;
        }

        // Si han pasado >= resetDelay desde el último click válido, resetear saltosSeguidos
        if (Time.time - lastValidClickTime >= resetDelay)
        {
            saltosSeguidos = 0;
            if (cc != null) cc.frio();
            // opcional: Debug.Log("Reset saltosSeguidos por inactividad");
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
        StartCoroutine(BloquearMovimientoPorTiempo(2f));
    }

    IEnumerator BloquearMovimientoPorTiempo(float segundos)
    {
        puedeMover = false;
        yield return new WaitForSeconds(segundos);
        puedeMover = true;
    }
}
