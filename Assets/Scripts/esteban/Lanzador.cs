using UnityEngine;

public class Lanzador : MonoBehaviour
{
    [Header("Prefabs de cada jugador (esferas o fichas)")]
    public Rigidbody[] jugadorPrefabs; // tama�o 4 en el inspector

    [Header("Punto de lanzamiento")]
    public Transform puntoLanzamiento;

    [HideInInspector] public float fuerza;
    [HideInInspector] public float anguloHorizontal;
    [HideInInspector] public float anguloVertical;

    public void Lanzar()
    {
        // Obtener el �ndice de turno actual (0-3)
        int turnoActual = TurnManager.instance.CurrentTurn() - 1;
        

        // Elegir el prefab correcto
        Rigidbody prefabJugador = jugadorPrefabs[turnoActual];

        // Instanciarlo
        Rigidbody esfera = Instantiate(prefabJugador, puntoLanzamiento.position, Quaternion.identity);

        // Calcular direcci�n seg�n �ngulos
        Vector3 direccion = CalcularDireccion();

        // Aplicar fuerza
        esfera.AddForce(direccion * fuerza, ForceMode.Impulse);
    }

    private Vector3 CalcularDireccion()
    {
        // �ngulo vertical controla la inclinaci�n (X), horizontal controla la direcci�n (Y)
        Quaternion rotacion = Quaternion.Euler(-anguloVertical, anguloHorizontal, 0);
        return rotacion * Vector3.forward;
    }
}
