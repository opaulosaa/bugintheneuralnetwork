using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lista fixa no canto da tela com os três puzzles numerados e o estado de cada um
/// (pendente ou feito). Lê o progresso do StoryState.
/// </summary>
public class PuzzleProgressHUD : MonoBehaviour
{
    public Text texto;
    public string[] nomes = { "Fila de prioridade", "Loop de pensamento", "Firewall social" };

    private int ultimoEstado = -1;

    void Update()
    {
        if (texto == null) return;

        var story = StoryState.Instance;
        bool[] feitos =
        {
            story != null && story.puzzle1Concluido,
            story != null && story.puzzle2Concluido,
            story != null && story.puzzle3Concluido
        };

        int estado = 0;
        for (int i = 0; i < feitos.Length; i++)
        {
            if (feitos[i]) estado |= 1 << i;
        }
        if (estado == ultimoEstado) return;
        ultimoEstado = estado;

        string linhas = "";
        for (int i = 0; i < nomes.Length; i++)
        {
            string cor = feitos[i] ? "#7ED493" : "#B8C0D4";
            string situacao = feitos[i] ? "feito" : "pendente";
            linhas += "<color=" + cor + "><b>Puzzle " + (i + 1) + "</b>  " + nomes[i] + "  (" + situacao + ")</color>";
            if (i < nomes.Length - 1) linhas += "\n";
        }
        texto.text = linhas;
    }
}
