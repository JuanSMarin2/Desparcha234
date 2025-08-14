using UnityEngine;
using System.Collections;

public class ZancoMove : MonoBehaviour
{
    Transform posicion;
    bool puedeMover = true;
    bool llegoMeta = false; // Nueva variable de estado

    void Start()
    {
        posicion = gameObject.GetComponent<Transform>();
    }

    // Llama este método cuando llegue a la meta
    public void LlegarMeta()
    {
        llegoMeta = true;
    }

    public void MoveZanco()
    {
        if (llegoMeta) return; // Si llegó a la meta, no se mueve
        if (!puedeMover) return;

        Vector3 nuevaPos = transform.position;
        nuevaPos.x -= 0.2f;
        transform.position = nuevaPos;

        caida();
    }

    void caida()
    {
        int num = Random.Range(0, 100);
        if (num >= 0 && num < 5)
        {
            Debug.Log("Caida");
            StartCoroutine(BloquearMovimientoPorTiempo(2f));
        }
    }

    IEnumerator BloquearMovimientoPorTiempo(float segundos)
    {
        puedeMover = false;
        yield return new WaitForSeconds(segundos);
        puedeMover = true;
    }
}
