using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Acompanha o progresso da história: quais dos três primeiros puzzles foram
/// concluídos e avisa quando a cortina (puzzle final) pode ser liberada.
/// </summary>
public class StoryState : MonoBehaviour
{
    public static StoryState Instance { get; private set; }

    public bool puzzle1Concluido { get; private set; }
    public bool puzzle2Concluido { get; private set; }
    public bool puzzle3Concluido { get; private set; }

    public UnityEvent OnTodosPuzzlesConcluidos;

    /// <summary>Avisa qual puzzle (1, 2 ou 3) acabou de ser concluído. Usado pela ArrumacaoQuarto.</summary>
    public event System.Action<int> OnPuzzleConcluido;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void ConcluirPuzzle1() { if (puzzle1Concluido) return; puzzle1Concluido = true; OnPuzzleConcluido?.Invoke(1); VerificarTodos(); }
    public void ConcluirPuzzle2() { if (puzzle2Concluido) return; puzzle2Concluido = true; OnPuzzleConcluido?.Invoke(2); VerificarTodos(); }
    public void ConcluirPuzzle3() { if (puzzle3Concluido) return; puzzle3Concluido = true; OnPuzzleConcluido?.Invoke(3); VerificarTodos(); }

    public bool TodosConcluidos => puzzle1Concluido && puzzle2Concluido && puzzle3Concluido;

    void VerificarTodos()
    {
        if (TodosConcluidos)
        {
            OnTodosPuzzlesConcluidos?.Invoke();
        }
    }
}
