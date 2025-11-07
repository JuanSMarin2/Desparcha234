using UnityEngine;

public class Activator : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    void Start()
    {
        targetObject.SetActive(true);
    }
}

