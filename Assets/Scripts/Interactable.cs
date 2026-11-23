using UnityEngine;

/// <summary>
/// Classe base para qualquer objeto interativo da sala (mesa, quadro, celular, cortina).
/// Coloque em um GameObject com um Collider marcado como "Is Trigger".
/// </summary>
public class Interactable : MonoBehaviour
{
    [Tooltip("Texto mostrado no prompt, ex: 'Aperte E: Mesa de tarefas'")]
    public string textoPrompt = "Aperte E para interagir";

    [Tooltip("Se falso, o jogador vê o objeto mas não consegue interagir ainda")]
    public bool interagivel = true;

    public virtual void Interagir()
    {
        // Sobrescrito pelos scripts dos puzzles (PuzzlePriorityQueue, PuzzleLoop, etc.)
    }

    /// <summary>Verdadeiro enquanto o painel (UI) deste puzzle está na tela.</summary>
    public virtual bool PainelAberto => false;

    /// <summary>Fecha o painel deste puzzle, se houver (tecla Esc).</summary>
    public virtual void FecharPainel() { }
}
