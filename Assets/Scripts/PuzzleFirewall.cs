using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 3: o firewall do celular.
///
/// Cris criou regras para filtrar as mensagens. Como num firewall de verdade, as regras são lidas
/// de cima para baixo e a PRIMEIRA que combina com a mensagem decide se ela chega ou é barrada;
/// se nenhuma combinar, vale a regra padrão (barrar). Ao lado, uma prévia mostra em tempo real o
/// destino de cada mensagem e qual regra decidiu.
///
/// Objetivo: deixar chegar quem quer ajudar (Bia, mãe, turma) e barrar só o ruído
/// (corrente, promoção, golpe). Para isso é preciso trocar a ação de regras E mudar a ordem:
/// a regra das correntes precisa ficar acima da regra dos contatos.
/// A interface é montada pelo próprio script (TemaPixel).
/// </summary>
public class PuzzleFirewall : Interactable
{
    [System.Serializable]
    public class Regra
    {
        public string alvo;
        public string nota;
        public bool permitir;
    }

    [System.Serializable]
    public class Mensagem
    {
        public string de;
        public string texto;
        [Tooltip("Rótulos que as regras usam para reconhecer a mensagem.")]
        public string[] marcas;
        public bool deveChegar;
    }

    public GameObject painel;
    public Text textoFeedback;
    public StoryState storyState;

    public Regra[] regrasIniciais =
    {
        new Regra { alvo = "contatos", nota = "\"pra não incomodar ninguém\"", permitir = false },
        new Regra { alvo = "Bia", nota = "\"só se ela insistir muito\"", permitir = true },
        new Regra { alvo = "desconhecidos", nota = "\"com estranho é mais fácil\"", permitir = true },
        new Regra { alvo = "promoções", nota = "", permitir = false },
        new Regra { alvo = "correntes", nota = "", permitir = false },
    };

    public Mensagem[] mensagens =
    {
        new Mensagem { de = "Bia", texto = "Tô aqui. Pode falar.", marcas = new[] { "contatos", "Bia" }, deveChegar = true },
        new Mensagem { de = "Mãe", texto = "Comeu alguma coisa hoje?", marcas = new[] { "contatos" }, deveChegar = true },
        new Mensagem { de = "Grupo da turma", texto = "Alguém quer revisar os slides junto?", marcas = new[] { "contatos" }, deveChegar = true },
        new Mensagem { de = "Grupo da família", texto = "BOM DIA!!! Repasse para 10 pessoas", marcas = new[] { "contatos", "correntes" }, deveChegar = false },
        new Mensagem { de = "Loja Mega", texto = "50% OFF só hoje!!!", marcas = new[] { "promoções" }, deveChegar = false },
        new Mensagem { de = "+55 11 9xxxx", texto = "Você ganhou um prêmio! Clique no link", marcas = new[] { "desconhecidos" }, deveChegar = false },
    };

    // ----- legado (cena antiga e ferramentas antigas) -----
    [HideInInspector] public Toggle[] regras = new Toggle[0];
    [HideInInspector] public bool[] estadoCorreto = new bool[0];
    [HideInInspector] public Button botaoAplicar;

    private readonly List<Regra> atuais = new List<Regra>();
    private readonly List<RectTransform> linhas = new List<RectTransform>();
    private readonly Dictionary<Regra, Text> textosAcao = new Dictionary<Regra, Text>();
    private readonly Dictionary<Regra, Image> fundosAcao = new Dictionary<Regra, Image>();
    private readonly Dictionary<Regra, Text> numeros = new Dictionary<Regra, Text>();
    private Text[] status;
    private Button aplicar;
    private bool concluido;

    const float Y_PRIMEIRA = 150f, PASSO = 62f, X_REGRAS = -300f;

    void Awake()
    {
        foreach (var r in regrasIniciais) atuais.Add(new Regra { alvo = r.alvo, nota = r.nota, permitir = r.permitir });
    }

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
    public override void FecharPainel() { Fechar(); }

    // ---------------------------------------------------------------- regras

    static bool Combina(Regra r, Mensagem m)
    {
        foreach (var marca in m.marcas) if (string.Equals(marca, r.alvo, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Índice da regra que decide a mensagem (-1 = regra padrão) e se ela chega.</summary>
    public int Avaliar(Mensagem m, out bool chega)
    {
        for (int i = 0; i < atuais.Count; i++)
            if (Combina(atuais[i], m)) { chega = atuais[i].permitir; return i; }
        chega = false;
        return -1;
    }

    /// <summary>Troca PERMITIR/BARRAR da regra na posição indicada (público para o teste).</summary>
    public void AlternarAcao(int posicao)
    {
        if (concluido || posicao < 0 || posicao >= atuais.Count) return;
        atuais[posicao].permitir = !atuais[posicao].permitir;
        AtualizarTudo();
    }

    /// <summary>Sobe (-1) ou desce (+1) a regra (público para o teste).</summary>
    public void Mover(int posicao, int direcao)
    {
        int destino = posicao + direcao;
        if (concluido || posicao < 0 || posicao >= atuais.Count || destino < 0 || destino >= atuais.Count) return;
        var r = atuais[posicao]; atuais[posicao] = atuais[destino]; atuais[destino] = r;
        var l = linhas[posicao]; linhas[posicao] = linhas[destino]; linhas[destino] = l;
        AtualizarTudo();
    }

    public int PosicaoDe(string alvo) { return atuais.FindIndex(r => r.alvo == alvo); }

    /// <summary>Confere a configuração (público para o teste).</summary>
    public void Aplicar()
    {
        if (concluido) return;
        foreach (var m in mensagens)
        {
            bool chega;
            int regra = Avaliar(m, out chega);
            if (chega == m.deveChegar) continue;
            string quem = regra >= 0 ? "pela regra " + (regra + 1) + " (" + atuais[regra].alvo + ")" : "pela regra padrão";
            Erro(chega
                ? "\"" + m.de + "\" ainda passa " + quem + ". Isso é ruído: precisa ser barrado."
                : "\"" + m.de + "\" ainda é barrada " + quem + ". Lembre: a primeira regra que combina é a que decide.");
            return;
        }
        concluido = true;
        interagivel = false;
        GameManager.Instance?.ProgressoNoPuzzle();
        Feedback("Regras aplicadas. As mensagens chegaram. A primeira é da Bia: \"Tô aqui. Pode falar.\"");
        storyState?.ConcluirPuzzle3();
        Invoke(nameof(Fechar), 2.6f);
    }

    void Erro(string msg)
    {
        GameManager.Instance?.ErroNoPuzzle();
        Feedback(msg);
        if (textoFeedback != null) textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
        if (aplicar != null) aplicar.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);
    }

    void Feedback(string msg) { if (textoFeedback != null) textoFeedback.text = msg; }

    // ---------------------------------------------------------------- interface

    void Montar()
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || painel == null) { Debug.LogWarning("PuzzleFirewall: sem TemaPixel no Canvas, a interface não foi montada."); return; }

        // Este painel precisa de mais espaço que os outros.
        ((RectTransform)painel.transform).sizeDelta = new Vector2(1260f, 900f);
        var c = TemaPixel.NovoConteudo(painel.transform);
        var itens = new List<RectTransform>();

        // ----- coluna das regras -----
        var colunaRegras = TemaPixel.Area(c, "Regras", new Vector2(0f, 0f), new Vector2(1200f, 520f));
        tema.Texto(colunaRegras, "Titulo_Regras", "REGRAS (DE CIMA PARA BAIXO)", 12, tema.corAmbar, TextAnchor.MiddleLeft, new Vector2(X_REGRAS, 205f), new Vector2(600f, 26f), true);
        foreach (var r in atuais) linhas.Add(LinhaRegra(tema, colunaRegras, r));
        var padrao = tema.Texto(colunaRegras, "Regra_Padrao", "padrão: nenhuma combinou  >  BARRAR", 24, tema.corApagada, TextAnchor.MiddleLeft, new Vector2(X_REGRAS + 20f, Y_PRIMEIRA - PASSO * atuais.Count + 8f), new Vector2(560f, 30f));
        itens.Add(colunaRegras);

        // ----- coluna das mensagens (prévia) -----
        var colunaMsg = TemaPixel.Area(c, "Mensagens", new Vector2(0f, 0f), new Vector2(1200f, 520f));
        tema.Texto(colunaMsg, "Titulo_Msg", "CAIXA DE ENTRADA (PRÉVIA)", 12, tema.corAmbar, TextAnchor.MiddleLeft, new Vector2(320f, 205f), new Vector2(520f, 26f), true);
        status = new Text[mensagens.Length];
        for (int i = 0; i < mensagens.Length; i++)
        {
            float y = 160f - i * 56f;
            var m = mensagens[i];
            tema.Texto(colunaMsg, "De_" + i, m.de, 24, tema.corAmbar, TextAnchor.MiddleLeft, new Vector2(240f, y + 12f), new Vector2(360f, 26f));
            tema.Texto(colunaMsg, "Msg_" + i, "\"" + m.texto + "\"", 22, tema.corTexto, TextAnchor.MiddleLeft, new Vector2(240f, y - 12f), new Vector2(360f, 26f));
            status[i] = tema.Texto(colunaMsg, "Status_" + i, "", 22, tema.corVerde, TextAnchor.MiddleRight, new Vector2(510f, y), new Vector2(160f, 48f));
        }
        itens.Add(colunaMsg);

        aplicar = tema.Botao(c, "BotaoAplicar", "Aplicar regras", new Vector2(320f, -196f), new Vector2(380f, 56f), 30);
        aplicar.onClick.AddListener(Aplicar);
        itens.Add((RectTransform)aplicar.transform);

        AtualizarTudo(true);
        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) pp.itens = itens.ToArray();
    }

    RectTransform LinhaRegra(TemaPixel tema, Transform pai, Regra r)
    {
        var linha = TemaPixel.Area(pai, "Regra_" + r.alvo, new Vector2(X_REGRAS, 0f), new Vector2(600f, 56f));
        numeros[r] = tema.Texto(linha, "Numero", "1", 26, tema.corApagada, TextAnchor.MiddleCenter, new Vector2(-282f, 0f), new Vector2(30f, 40f));
        var sobe = tema.Botao(linha, "Subir", "^", new Vector2(-236f, 0f), new Vector2(48f, 48f), 28, false);
        var desce = tema.Botao(linha, "Descer", "v", new Vector2(-184f, 0f), new Vector2(48f, 48f), 26, false);
        var acao = tema.Botao(linha, "Acao", "BARRAR", new Vector2(-70f, 0f), new Vector2(160f, 48f), 24, false);
        tema.Texto(linha, "Alvo", r.alvo, 28, tema.corTexto, TextAnchor.MiddleLeft, new Vector2(140f, r.nota.Length > 0 ? 10f : 0f), new Vector2(240f, 30f));
        if (r.nota.Length > 0) tema.Texto(linha, "Nota", r.nota, 20, tema.corApagada, TextAnchor.MiddleLeft, new Vector2(140f, -14f), new Vector2(240f, 24f));
        textosAcao[r] = acao.GetComponent<BotaoPixel>().texto;
        fundosAcao[r] = acao.GetComponent<Image>();

        sobe.onClick.AddListener(() => Mover(atuais.IndexOf(r), -1));
        desce.onClick.AddListener(() => Mover(atuais.IndexOf(r), +1));
        acao.onClick.AddListener(() => AlternarAcao(atuais.IndexOf(r)));
        return linha;
    }

    void AtualizarTudo(bool instantaneo = false)
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || status == null) return;
        for (int i = 0; i < atuais.Count; i++)
        {
            var r = atuais[i];
            var bp = textosAcao[r].GetComponentInParent<BotaoPixel>();
            textosAcao[r].text = r.permitir ? "PERMITIR" : "BARRAR";
            if (bp != null) { bp.corTexto = r.permitir ? tema.corVerde : tema.corVermelho; bp.corTextoHover = bp.corTexto; }
            numeros[r].text = (i + 1).ToString();
            Vector2 alvo = new Vector2(X_REGRAS, Y_PRIMEIRA - i * PASSO);
            if (instantaneo || !isActiveAndEnabled || !painel.activeInHierarchy) linhas[i].anchoredPosition = alvo;
            else StartCoroutine(Deslizar(linhas[i], alvo));
        }
        for (int i = 0; i < mensagens.Length; i++)
        {
            bool chega;
            int regra = Avaliar(mensagens[i], out chega);
            status[i].text = (chega ? "chega" : "barrada") + "\n" + (regra >= 0 ? "regra " + (regra + 1) : "padrão");
            status[i].color = chega ? tema.corVerde : tema.corVermelho;
        }
    }

    // A linha desliza até a nova posição em degraus de pixel.
    IEnumerator Deslizar(RectTransform rt, Vector2 alvo)
    {
        Vector2 de = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.14f)
        {
            t += Time.unscaledDeltaTime;
            Vector2 p = Vector2.Lerp(de, alvo, Mathf.Clamp01(t / 0.14f));
            rt.anchoredPosition = new Vector2(Mathf.Round(p.x / 4f) * 4f, Mathf.Round(p.y / 4f) * 4f);
            yield return null;
        }
        rt.anchoredPosition = alvo;
    }
}
