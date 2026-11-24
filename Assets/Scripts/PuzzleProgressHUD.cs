using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lista fixa no canto da tela com os puzzles numerados e o estado de cada um
/// (pendente ou feito). Lê o progresso do StoryState. A quantidade de puzzles é o tamanho de "nomes".
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
        int estado = 0;
        for (int i = 0; i < nomes.Length; i++)
            if (story != null && story.Concluido(i + 1)) estado |= 1 << i;
        if (estado == ultimoEstado) return;
        ultimoEstado = estado;

        string linhas = "";
        for (int i = 0; i < nomes.Length; i++)
        {
            bool feito = (estado & (1 << i)) != 0;
            string cor = feito ? "#7ED493" : "#B8C0D4";
            string situacao = feito ? "feito" : "pendente";
            linhas += "<color=" + cor + "><b>Puzzle " + (i + 1) + "</b>  " + nomes[i] + "  (" + situacao + ")</color>";
            if (i < nomes.Length - 1) linhas += "\n";
        }
        texto.text = linhas;
    }
}
