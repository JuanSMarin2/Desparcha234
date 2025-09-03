using UnityEngine;

public class RandomActivator : MonoBehaviour
{
    [SerializeField] private GameObject[] objects; // Arrastra aqu� los 4 GameObjects

    void Start()
    {
        if (objects == null || objects.Length == 0) return;

        // Elegir �ndice aleatorio
        int randomIndex = Random.Range(0, objects.Length);

        // Activar solo el elegido
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == randomIndex);
        }
    }
}
