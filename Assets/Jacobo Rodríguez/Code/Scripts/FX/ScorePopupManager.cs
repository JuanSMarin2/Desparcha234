using UnityEngine;
using System.Collections.Generic;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager instance;

    [SerializeField] private ScorePopup popupPrefab;
    [SerializeField] private int prewarm = 10;
    [SerializeField] private Transform worldSpaceParent; // opcional: contenedor

    private readonly Queue<ScorePopup> _pool = new Queue<ScorePopup>();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (worldSpaceParent == null) worldSpaceParent = transform;
        Prewarm();
    }

    private void Prewarm()
    {
        if (popupPrefab == null || prewarm <= 0) return;
        for (int i = 0; i < prewarm; i++)
        {
            var p = Instantiate(popupPrefab, worldSpaceParent);
            p.Init(this);
            _pool.Enqueue(p);
        }
    }

    public void Recycle(ScorePopup p)
    {
        if (p == null) return;
        p.gameObject.SetActive(false);
        p.transform.SetParent(worldSpaceParent, true);
        _pool.Enqueue(p);
    }

    public void Spawn(Vector3 worldPos, int points, Color? colorOverride = null)
    {
        if (popupPrefab == null) return;
        ScorePopup p = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(popupPrefab, worldSpaceParent);
        p.Init(this);
        p.Show(worldPos, points, colorOverride);
    }
}
