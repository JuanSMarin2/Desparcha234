using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador simple para encadenar varios mini-juegos que exponen
/// un método público para iniciar y un UnityEvent llamado onGameFinished.
/// Asigna los componentes en el inspector en el orden que deben ejecutarse.
/// </summary>
public class GameSequenceController : MonoBehaviour
{
    [Tooltip("Lista de componentes de juego (por ejemplo, CircleGameManagerUI, AccelerometerGame, BotonReducible, JOrden) en el orden de ejecución.")]
    public MonoBehaviour[] gamesInOrder;

    private int currentIndex = 0;

    public void PlaySequence()
    {
        StopAllCoroutines();
        currentIndex = 0;
        if (gamesInOrder == null || gamesInOrder.Length == 0)
        {
            Debug.LogWarning("GameSequenceController: no hay juegos asignados.");
            return;
        }

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        while (currentIndex < gamesInOrder.Length)
        {
            var comp = gamesInOrder[currentIndex];
            if (comp == null)
            {
                currentIndex++;
                continue;
            }

            bool started = TryStartGame(comp);
            if (!started)
            {
                Debug.LogWarning($"GameSequenceController: componente {comp.name} no tiene un método Play()/StartGame()/StartSequence(). Skipping.");
                currentIndex++;
                continue;
            }

            // Wait until the component invokes its UnityEvent (onGameFinished) by polling a common convention: look for a public field named "onGameFinished"
            bool finished = false;
            System.Action finishCallback = () => finished = true;

            var eventField = comp.GetType().GetField("onGameFinished");
            if (eventField != null)
            {
                var ue = eventField.GetValue(comp) as UnityEngine.Events.UnityEvent;
                if (ue != null)
                {
                    ue.AddListener(() => finishCallback());
                }
            }

            // Wait until finished flag is set
            while (!finished)
                yield return null;

            currentIndex++;
        }

        Debug.Log("GameSequenceController: Secuencia de juegos completada.");
    }

    private bool TryStartGame(MonoBehaviour comp)
    {
        var t = comp.GetType();

        // Try common start method names
        var methods = new string[] { "Play", "StartGame", "StartSequence" };
        foreach (var name in methods)
        {
            var m = t.GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (m != null)
            {
                m.Invoke(comp, null);
                return true;
            }
        }

        return false;
    }
}
