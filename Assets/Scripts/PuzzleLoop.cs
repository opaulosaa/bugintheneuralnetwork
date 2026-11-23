using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 2: o loop de pensamento (quadro/monitor).
/// Quatro botões (A, B, C, D); marque "correta" apenas na opção C do documento
/// ("parar quando houver uma evidência contrária").
/// </summary>
public class PuzzleLoop : Interactable
{
    [System.Serializable]
    public class Opcao
    {
        public Button botao;
        public string textoFeedback;
        public bool correta;
    }

    public GameObject painel;
    public Opcao[] opcoes;
    public Text textoFeedback;
    public StoryState storyState;

    private bool concluido = false;

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        foreach (var o in opcoes)
        {
            var capturada = o;
            capturada.botao.onClick.AddListener(() => Escolher(capturada));
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

    void Escolher(Opcao opcao)
    {
        if (concluido) return;

        if (textoFeedback != null) textoFeedback.text = opcao.textoFeedback;

        if (opcao.correta)
        {
            concluido = true;
            interagivel = false;
            GameManager.Instance?.ProgressoNoPuzzle();
            storyState?.ConcluirPuzzle2();
            Invoke(nameof(Fechar), 1.6f);
        }
        else
        {
            GameManager.Instance?.ErroNoPuzzle();
            opcao.botao.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);   // UI pixel: chacoalha
            if (textoFeedback != null) textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
        }
    }
}
