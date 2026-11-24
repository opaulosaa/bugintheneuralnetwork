using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Acompanha o progresso da história: quais puzzles foram concluídos e avisa quando a cortina
/// (puzzle final) pode ser liberada. "totalPuzzles" diz quantos puzzles a cena tem (3 na versão
/// original, 5 depois de Versão Cris > 8. Puzzles 4 e 5 e cena final).
/// </summary>
public class StoryState : MonoBehaviour
{
    public static StoryState Instance { get; private set; }

    [Tooltip("Quantos puzzles precisam ser concluídos para liberar a cortina.")]
    public int totalPuzzles = 3;

    public UnityEvent OnTodosPuzzlesConcluidos;

    /// <summary>Avisa qual puzzle (1 a 5) acabou de ser concluído. Usado pela ArrumacaoQuarto.</summary>
    public event System.Action<int> OnPuzzleConcluido;

    private readonly bool[] concluidos = new bool[8];
    private bool avisouTodos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public bool Concluido(int numero) { return numero >= 1 && numero <= concluidos.Length && concluidos[numero - 1]; }

    public bool puzzle1Concluido => Concluido(1);
    public bool puzzle2Concluido => Concluido(2);
    public bool puzzle3Concluido => Concluido(3);

    public void ConcluirPuzzle(int numero)
    {
        if (numero < 1 || numero > concluidos.Length || concluidos[numero - 1]) return;
        concluidos[numero - 1] = true;
        OnPuzzleConcluido?.Invoke(numero);
        VerificarTodos();
    }

    public void ConcluirPuzzle1() { ConcluirPuzzle(1); }
    public void ConcluirPuzzle2() { ConcluirPuzzle(2); }
    public void ConcluirPuzzle3() { ConcluirPuzzle(3); }

    public bool TodosConcluidos
    {
        get
        {
            for (int i = 1; i <= totalPuzzles; i++) if (!Concluido(i)) return false;
            return true;
        }
    }

    void VerificarTodos()
    {
        if (TodosConcluidos && !avisouTodos)
        {
            avisouTodos = true;
            OnTodosPuzzlesConcluidos?.Invoke();
        }
    }
}
