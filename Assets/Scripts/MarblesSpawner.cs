using UnityEngine;

public class MarblesSpawner : MonoBehaviour
{

    [SerializeField] private GameObject marble;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (marble != null)
        marble.transform.position = transform.position;

        this.gameObject.SetActive(false);

    }

}
