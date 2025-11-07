using UnityEngine;

public class ShieldSpawnerManager : MonoBehaviour
{
    [Header("Objetos a mover")]
    [Tooltip("Objeto 'adelanto' que marca la posición donde estará el Shield el próximo turno.")]
    [SerializeField] private Transform beforeShield;

    [Tooltip("Objeto real del Shield que se moverá a la última posición del BeforeShield al cambiar de turno.")]
    [SerializeField] private Transform shieldObject;

    [Header("Posibles ubicaciones")] 
    [Tooltip("Lista de objetos cuyas posiciones se usarán como ubicaciones posibles.")]
    [SerializeField] private Transform[] candidatePositions;

    [Header("Opciones de posicionamiento")]
    [Tooltip("Si es true, se aplica también la rotación del candidato.")]
    [SerializeField] private bool matchRotation = false;

    [Tooltip("Si es true, preserva la componente Z actual del objeto movido.")]
    [SerializeField] private bool preserveZ = true;

    [Tooltip("Offset local a aplicar tras teletransportar (en el espacio del mundo).")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    private int lastObservedTurn = -1;
    private int lastBeforeIndex = -1;
    private Vector3 lastBeforePosition;
    private Quaternion lastBeforeRotation = Quaternion.identity;
    private bool hasLastBefore = false;

    private void Start()
    {
        if (TurnManager.instance != null)
        {
            lastObservedTurn = TurnManager.instance.CurrentTurn();
        }

        // En el arranque, si queremos ya posicionar un BeforeShield inicial
        if (!hasLastBefore)
        {
            MoveBeforeShieldToRandom(differentFromIndex: -1);
        }
    }

    private void Update()
    {
        if (TurnManager.instance == null) return;

        int current = TurnManager.instance.CurrentTurn();
        if (lastObservedTurn != -1 && current != lastObservedTurn)
        {
            OnTurnChanged();
        }
        lastObservedTurn = current;
    }

    private void OnTurnChanged()
    {
        // 1) Mover el ShieldObject a la última posición del BeforeShield (si existe)
        if (shieldObject != null && hasLastBefore)
        {
            Vector3 pos = lastBeforePosition + worldOffset;
            if (preserveZ) pos.z = shieldObject.position.z;
            shieldObject.position = pos;
            if (matchRotation)
            {
                shieldObject.rotation = lastBeforeRotation;
            }
        }

        // 2) Mover el BeforeShield a una nueva posición aleatoria distinta
        MoveBeforeShieldToRandom(differentFromIndex: lastBeforeIndex);
    }

    private void MoveBeforeShieldToRandom(int differentFromIndex)
    {
        if (beforeShield == null || candidatePositions == null || candidatePositions.Length == 0)
            return;

        // Elegir índice aleatorio (distinto del anterior si hay más de uno)
        int newIndex = GetRandomIndexDifferentFrom(differentFromIndex);
        Transform target = candidatePositions[newIndex];
        if (target == null) return;

        // Guardar como última posición/rotación para el próximo turno
        lastBeforeIndex = newIndex;
        lastBeforePosition = target.position;
        lastBeforeRotation = target.rotation;
        hasLastBefore = true;

        // Colocar el BeforeShield ahora mismo
        Vector3 pos = target.position + worldOffset;
        if (preserveZ) pos.z = beforeShield.position.z;
        beforeShield.position = pos;
        if (matchRotation)
        {
            beforeShield.rotation = target.rotation;
        }
    }

    private int GetRandomIndexDifferentFrom(int exclude)
    {
        int n = candidatePositions.Length;
        if (n <= 1) return 0;

        int idx;
        // Evitar bucles infinitos si todos son null o lista pequeña
        int guard = 0;
        do
        {
            idx = Random.Range(0, n);
            guard++;
            if (guard > 50) break; // fallback
        } while (idx == exclude || candidatePositions[idx] == null);

        // Si el elegido es null, buscar el primero válido
        if (candidatePositions[idx] == null)
        {
            for (int i = 0; i < n; i++)
            {
                if (i == exclude) continue;
                if (candidatePositions[i] != null) return i;
            }
            // Si ninguno válido, devolvemos 0 y que falle silenciosamente
            return 0;
        }

        return idx;
    }
}
