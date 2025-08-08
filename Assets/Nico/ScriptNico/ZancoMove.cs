using UnityEngine;
using System.Collections.Generic;

public class ZancoMove : MonoBehaviour
{
    Transform posicion;
    void Start()
    {
        posicion = gameObject.GetComponent<Transform>();
    }
    public void MoveZanco()
    {
        Vector3 nuevaPos = transform.position;
        nuevaPos.x -= 0.2f;
        transform.position = nuevaPos;
    }
}
