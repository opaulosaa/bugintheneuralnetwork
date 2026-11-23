using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 1: fila de prioridade (mesa com post-its).
/// Arraste os 5 botões de post-it para "postIts", cada um com a ordem correta
/// (0 = Comer algo, 1 = Corrigir o bug, 2 = Ensaiar a apresentação, 3 = Estudar para a prova de Redes, 4 = Dormir).
/// </summary>
public class PuzzlePriorityQueue : Interactable
{
    [System.Serializable]
    public class PostIt
    {
        public Button botao;
        public int ordemCorreta;
    }

    public GameObject painel;
    public PostIt[] postIts;
    public Text textoFeedback;
    public StoryState storyState;

    private int proximoEsperado = 0;
    private bool concluido = false;

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        foreach (var p in postIts)
        {
            var capturado = p;
            capturado.botao.onClick.AddListener(() => Clicar(capturado));
        }
    }

    public override void Interagir()
    {
        if (concluido) return;
        if (painel != null) painel.SetActive(true);
    }

    public void Fechar()
    {
        if (painel != null) painel.SetActive(false);
    }

    public override bool PainelAberto => painel != null && painel.activeInHierarchy;
    public override void FecharPainel() { Fechar(); }

    void Clicar(PostIt post)
    {
        if (concluido) return;

        if (post.ordemCorreta == proximoEsperado)
        {
            post.botao.interactable = false;
            proximoEsperado++;
            GameManager.Instance?.ProgressoNoPuzzle();
            if (textoFeedback != null) textoFeedback.text = "Certo. Continue.";

            if (proximoEsperado >= postIts.Length)
            {
                concluido = true;
                if (textoFeedback != null) textoFeedback.text = "Fila organizada.";
                interagivel = false;
                storyState?.ConcluirPuzzle1();
                Invoke(nameof(Fechar), 1.2f);
            }
        }
        else
        {
            GameManager.Instance?.ErroNoPuzzle();
            if (textoFeedback != null)
            {
                textoFeedback.text = "Essa não é a prioridade agora.";
                textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);   // UI pixel: digita de novo
            }
            post.botao.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);              // UI pixel: chacoalha
        }
    }
}
