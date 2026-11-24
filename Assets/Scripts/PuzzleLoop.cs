using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 2: o pensamento em laço (mural de cortiça).
///
/// O jogador monta o laço:
///     enquanto (CONDIÇÃO) {
///         pensar("Eu não dou conta.");
///         CORPO
///     }
/// e clica em Executar. Para o laço terminar, as duas escolhas precisam estar certas:
///  - a condição tem que poder ficar falsa por algo que Cris controla (!temEvidencia);
///  - e algo dentro do laço tem que mudar essa variável (temEvidencia = lembrarConquistas()).
/// Condição certa com corpo errado continua sendo um laço infinito: é a lição do puzzle.
/// A interface é montada pelo próprio script (TemaPixel).
/// </summary>
public class PuzzleLoop : Interactable
{
    public GameObject painel;
    public Text textoFeedback;
    public StoryState storyState;

    public string[] condicoes = { "verdadeiro", "!tudoPerfeito", "!alguemConcorda", "!temEvidencia" };
    public string[] explicacaoCondicao =
    {
        "\"verdadeiro\" nunca vira falso: esse laço não tem porta de saída.",
        "Para sair, tudo teria que ficar perfeito. Isso nunca acontece, então o laço nunca termina.",
        "A saída ficou na mão dos outros, e ninguém sabe que esse laço está rodando.",
        ""
    };
    public string[] rotulosCorpo = { "pensar de novo", "esperar passar", "pedir desculpas", "lembrar conquistas" };
    public string[] codigoCorpo =
    {
        "pensar(\"Eu não dou conta.\");",
        "esperar(cansaco.passar);",
        "pedirDesculpas(todos);",
        "temEvidencia = lembrarConquistas();"
    };
    public int condicaoCerta = 3;
    public int corpoCerto = 3;

    // ----- legado (cena antiga e ferramentas antigas) -----
    [System.Serializable] public class Opcao { public Button botao; public string textoFeedback; public bool correta; }
    [HideInInspector] public Opcao[] opcoes = new Opcao[0];

    private int condicao = -1, corpo = -1;
    private Button[] botoesCond, botoesCorpo;
    private Button botaoExecutar;
    private Text codigo;
    private bool executando, concluido;

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        Montar();
    }

    public override void Interagir()
    {
        if (concluido) return;
        if (painel != null) painel.SetActive(true);
    }

    public void Fechar() { if (painel != null) painel.SetActive(false); }
    public override bool PainelAberto => painel != null && painel.activeInHierarchy;
    public override void FecharPainel() { if (!executando) Fechar(); }

    // ---------------------------------------------------------------- interface

    void Montar()
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || painel == null) { Debug.LogWarning("PuzzleLoop: sem TemaPixel no Canvas, a interface não foi montada."); return; }
        var c = TemaPixel.NovoConteudo(painel.transform);
        var itens = new List<RectTransform>();

        itens.Add(tema.Texto(c, "Rotulo_Condicao", "1. CONDIÇÃO DE SAÍDA  enquanto ( ? )", 12, tema.corAmbar, TextAnchor.MiddleLeft, new Vector2(0f, 152f), new Vector2(980f, 26f), true).rectTransform);
        botoesCond = Fileira(tema, c, "Cond_", condicoes, 112f, i => Escolher(i, corpo), itens);
        itens.Add(tema.Texto(c, "Rotulo_Corpo", "2. DENTRO DO LAÇO, DEPOIS DE PENSAR", 12, tema.corAmbar, TextAnchor.MiddleLeft, new Vector2(0f, 64f), new Vector2(980f, 26f), true).rectTransform);
        botoesCorpo = Fileira(tema, c, "Corpo_", rotulosCorpo, 24f, i => Escolher(condicao, i), itens);

        var caixa = tema.Imagem(c, "Caixa_Codigo", tema.caixaCodigo != null ? tema.caixaCodigo : tema.botao, new Color(1f, 1f, 1f, 0.9f), new Vector2(0f, -78f), new Vector2(980f, 128f), true);
        codigo = tema.Texto(caixa.transform, "Codigo", "", 26, tema.corTexto, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(920f, 116f));
        itens.Add(caixa.rectTransform);

        botaoExecutar = tema.Botao(c, "BotaoExecutar", "Executar  >", new Vector2(0f, -172f), new Vector2(340f, 56f), 30);
        botaoExecutar.onClick.AddListener(Executar);
        itens.Add((RectTransform)botaoExecutar.transform);

        AtualizarCodigo();
        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) pp.itens = itens.ToArray();
    }

    Button[] Fileira(TemaPixel tema, Transform pai, string prefixo, string[] rotulos, float y, System.Action<int> aoEscolher, List<RectTransform> itens)
    {
        var botoes = new Button[rotulos.Length];
        float largura = 236f, passo = 246f;
        for (int i = 0; i < rotulos.Length; i++)
        {
            float x = (i - (rotulos.Length - 1) * 0.5f) * passo;
            var b = tema.Botao(pai, prefixo + i, rotulos[i], new Vector2(x, y), new Vector2(largura, 54f), 26, false);
            int indice = i;
            b.onClick.AddListener(() => aoEscolher(indice));
            botoes[i] = b;
            itens.Add((RectTransform)b.transform);
        }
        return botoes;
    }

    /// <summary>Escolhe condição e corpo (-1 = nenhum). Público para o teste automático.</summary>
    public void Escolher(int novaCondicao, int novoCorpo)
    {
        if (executando || concluido) return;
        condicao = novaCondicao; corpo = novoCorpo;
        Marcar(botoesCond, condicao);
        Marcar(botoesCorpo, corpo);
        AtualizarCodigo();
    }

    static void Marcar(Button[] botoes, int escolhido)
    {
        if (botoes == null) return;
        for (int i = 0; i < botoes.Length; i++)
        {
            var bp = botoes[i].GetComponent<BotaoPixel>();
            if (bp != null) bp.selecionado = i == escolhido;
        }
    }

    void AtualizarCodigo()
    {
        if (codigo == null) return;
        string ambar = "<color=#FFCC6B>";
        string cond = condicao >= 0 ? ambar + condicoes[condicao] + "</color>" : ambar + "???</color>";
        string linha = corpo >= 0 ? ambar + codigoCorpo[corpo] + "</color>" : ambar + "???</color>";
        codigo.text = "enquanto (" + cond + ") {\n    pensar(\"Eu não dou conta.\");\n    " + linha + "\n}";
    }

    // ---------------------------------------------------------------- execução

    /// <summary>Roda o laço montado (público para o teste automático).</summary>
    public void Executar()
    {
        if (executando || concluido) return;
        if (condicao < 0 || corpo < 0)
        {
            Feedback("Escolha a condição de saída e o que acontece dentro do laço antes de executar.");
            return;
        }
        StartCoroutine(Rodar());
    }

    IEnumerator Rodar()
    {
        executando = true;
        bool certo = condicao == condicaoCerta && corpo == corpoCerto;
        int voltas = certo ? 1 : 4;
        for (int v = 1; v <= voltas; v++)
        {
            Feedback("executando... volta " + (v < 4 ? v.ToString() : "9.999") + (certo ? "" : "  pensar(\"Eu não dou conta.\")"));
            yield return new WaitForSeconds(v < 3 ? 0.45f : 0.25f);
        }

        if (certo)
        {
            concluido = true;
            interagivel = false;
            Feedback("lembrarConquistas(): Banco de Dados entregue. Estruturas de Dados aprovada.\ntemEvidencia = verdadeiro. O laço terminou.");
            GameManager.Instance?.ProgressoNoPuzzle();
            storyState?.ConcluirPuzzle2();
            executando = false;
            Invoke(nameof(Fechar), 2.6f);
            yield break;
        }

        string porque = condicao != condicaoCerta
            ? explicacaoCondicao[condicao]
            : "A condição é boa, mas nada dentro do laço muda \"temEvidencia\". Sem isso, ele gira para sempre.";
        Feedback("Laço infinito. " + porque);
        if (textoFeedback != null) textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
        if (botaoExecutar != null) botaoExecutar.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);
        GameManager.Instance?.ErroNoPuzzle();
        executando = false;
    }

    void Feedback(string msg) { if (textoFeedback != null) textoFeedback.text = msg; }
}
