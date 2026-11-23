using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 3: firewall social (celular/computador).
/// Quatro regras (Toggle), na mesma ordem do documento de história. O jogador
/// deve deixar desligadas as três primeiras e ligada a última ("Silenciar spam e promoções"),
/// depois clicar em "Aplicar".
/// </summary>
public class PuzzleFirewall : Interactable
{
    public GameObject painel;
    public Toggle[] regras;
    public bool[] estadoCorreto = new bool[] { false, false, false, true };
    public Button botaoAplicar;
    public Text textoFeedback;
    public StoryState storyState;

    private bool concluido = false;

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        if (botaoAplicar != null) botaoAplicar.onClick.AddListener(Aplicar);
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

    void Aplicar()
    {
        if (concluido) return;

        bool tudoCerto = true;
        for (int i = 0; i < regras.Length; i++)
        {
            if (regras[i].isOn != estadoCorreto[i]) tudoCerto = false;
        }

        if (tudoCerto)
        {
            concluido = true;
            interagivel = false;
            GameManager.Instance?.ProgressoNoPuzzle();
            if (textoFeedback != null) textoFeedback.text = "As mensagens da Bia chegaram. A primeira: \"Tô aqui. Pode falar.\"";
            storyState?.ConcluirPuzzle3();
            Invoke(nameof(Fechar), 1.6f);
        }
        else
        {
            GameManager.Instance?.ErroNoPuzzle();
            if (botaoAplicar != null) botaoAplicar.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);   // UI pixel
            if (textoFeedback != null) {
                textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
                bool spamCerto = regras.Length > 3 && regras[3].isOn == estadoCorreto[3];
                textoFeedback.text = spamCerto
                    ? "A Bia continua do lado de fora. Alguma regra ainda decide por Cris."
                    : "Sem esse filtro, o celular vira só ruído. Nem toda barreira é ruim.";
            }
        }
    }
}
