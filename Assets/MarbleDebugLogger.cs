using UnityEngine;
using System.Linq;

public class MarbleDebugLogger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            var marbles = FindObjectsByType<MarblePower>(FindObjectsSortMode.None);

      
            marbles = marbles.OrderBy(m => m.name).ToArray();

            for (int i = 0; i < marbles.Length; i++)
            {
                var m = marbles[i];
       
                var type = "None";
                var turns = 0;

      

                Debug.Log($"Canica [{i}] ? {type} (turnos: {turns})");
            }
        }
    }
}