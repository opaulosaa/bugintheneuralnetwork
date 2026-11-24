using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 4: deadlock (notebook).
///
/// Quatro "processos" de Cris esperam uns pelos outros (grafo de espera): cada seta "A espera B" quer
/// dizer que A só roda depois de B. Há ciclos, então nada roda: um deadlock, como a paralisia de
/// quem não consegue começar. Para destravar, o jogador corta esperas, mas só as que existem apenas na
/// cabeça de Cris (esperas reais não podem ser cortadas). Ao executar, se ainda houver ciclo, o puzzle
/// mostra qual é; se não houver, os processos rodam em ordem (ordenação topológica).
///
/// A espera certa a cortar é "só abro os slides quando me sentir capaz": ela está nos dois ciclos.
/// A interface é montada pelo próprio script (TemaPixel).
/// </summary>
public class PuzzleDeadlock : Interactable
{
    [System.Serializable]
    public class Processo
    {
        public string nome;
        public Vector2 posicao;
    }

    [System.Serializable]
    public class Espera
    {
        public int de;
        public int para;
        public string motivo;
        public bool real;
        public string porQueReal;
        public Vector2 posicaoRotulo;
    }

    public GameObject painel;
    public Text textoFeedback;
    public StoryState storyState;
    public int numeroPuzzle = 4;

    public Processo[] processos =
    {
        new Processo { nome = "Abrir os slides", posicao = new Vector2(-420f, 10f) },
        new Processo { nome = "Sentir-se capaz", posicao = new Vector2(-60f, 150f) },
        new Processo { nome = "Ensaiar", posicao = new Vector2(30f, -120f) },
        new Processo { nome = "Descansar", posicao = new Vector2(400f, 90f) },
    };

    public Espera[] esperas =
    {
        new Espera { de = 0, para = 1, motivo = "só abro quando me sentir capaz", real = false, posicaoRotulo = new Vector2(-330f, 100f) },
        new Espera { de = 1, para = 2, motivo = "me sinto capaz depois de ensaiar", real = true,
                     porQueReal = "Ensaiar dá segurança de verdade.", posicaoRotulo = new Vector2(60f, 22f) },
        new Espera { de = 2, para = 0, motivo = "ensaiar precisa dos slides abertos", real = true,
                     porQueReal = "Não dá para ensaiar sem os slides.", posicaoRotulo = new Vector2(-270f, -92f) },
        new Espera { de = 1, para = 3, motivo = "me sinto capaz depois de descansar", real = true,
                     porQueReal = "Cansaço demais derruba qualquer um.", posicaoRotulo = new Vector2(200f, 160f) },
        new Espera { de = 3, para = 2, motivo = "só descanso depois de ensaiar tudo de novo", real = false, posicaoRotulo = new Vector2(330f, -36f) },
    };

    private bool[] cortada;
    private Image[] linhas;
    private Button[] rotulos;
    private Button[] nos;
    private Button botaoExecutar;
    private bool executando, concluido;
    static readonly Vector2 MEIO_NO = new Vector2(115f, 30f);

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        cortada = new bool[esperas.Length];
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
        if (tema == null || painel == null) { Debug.LogWarning("PuzzleDeadlock: sem TemaPixel no Canvas, a interface não foi montada."); return; }
        ((RectTransform)painel.transform).sizeDelta = new Vector2(1260f, 900f);
        var c = TemaPixel.NovoConteudo(painel.transform);
        var grafo = TemaPixel.Area(c, "Grafo", Vector2.zero, new Vector2(1200f, 480f));

        linhas = new Image[esperas.Length];
        for (int i = 0; i < esperas.Length; i++)
        {
            var e = esperas[i];
            linhas[i] = tema.Seta(grafo, "Seta_" + i, processos[e.de].posicao, processos[e.para].posicao, MEIO_NO, MEIO_NO, tema.corApagada);
        }

        nos = new Button[processos.Length];
        for (int i = 0; i < processos.Length; i++)
        {
            var b = tema.Botao(grafo, "Processo_" + i, processos[i].nome, processos[i].posicao, MEIO_NO * 2f, 28, false);
            // Os processos não fazem nada ao clicar: o que se corta são as esperas.
            nos[i] = b;
        }

        rotulos = new Button[esperas.Length];
        for (int i = 0; i < esperas.Length; i++)
        {
            var b = tema.Botao(grafo, "Espera_" + i, "espera: " + esperas[i].motivo, esperas[i].posicaoRotulo, new Vector2(280f, 52f), 22, false);
            int indice = i;
            b.onClick.AddListener(() => Cortar(indice));
            rotulos[i] = b;
        }

        tema.Texto(grafo, "Legenda", "A espera B = A só roda depois de B.  Clique numa espera para cortá-la.", 22, tema.corApagada,
            TextAnchor.MiddleCenter, new Vector2(0f, 205f), new Vector2(1100f, 28f));

        botaoExecutar = tema.Botao(c, "BotaoExecutar", "Executar processos  >", new Vector2(0f, -212f), new Vector2(420f, 56f), 30);
        botaoExecutar.onClick.AddListener(Executar);

        AtualizarVisual();
        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) pp.itens = new[] { grafo, (RectTransform)botaoExecutar.transform };
    }

    void AtualizarVisual()
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || linhas == null) return;
        for (int i = 0; i < esperas.Length; i++)
        {
            Color cor = cortada[i] ? new Color(tema.corApagada.r, tema.corApagada.g, tema.corApagada.b, 0.25f) : tema.corTexto;
            linhas[i].color = cor;
            var ponta = linhas[i].transform.Find("Ponta");
            if (ponta != null) ponta.GetComponent<Image>().color = cor;
            var bp = rotulos[i].GetComponent<BotaoPixel>();
            bp.texto.text = cortada[i] ? "cortada: " + esperas[i].motivo : "espera: " + esperas[i].motivo;
            bp.corTexto = cortada[i] ? tema.corVerde : tema.corTexto;
            bp.selecionado = cortada[i];
        }
    }

    // ---------------------------------------------------------------- regras

    /// <summary>Corta (ou devolve) uma espera. Público para o teste automático.</summary>
    public void Cortar(int i)
    {
        if (executando || concluido || i < 0 || i >= esperas.Length) return;
        if (esperas[i].real)
        {
            Erro("Essa espera é real: " + esperas[i].porQueReal + " Corte uma espera que só existe na cabeça de Cris.", rotulos != null ? rotulos[i] : null);
            return;
        }
        cortada[i] = !cortada[i];
        AtualizarVisual();
        Feedback(cortada[i] ? "Espera cortada. Execute para ver se ainda há ciclo." : "Espera devolvida.");
    }

    /// <summary>Procura um ciclo nas esperas que sobraram; devolve os processos do ciclo, ou null.</summary>
    public List<int> AcharCiclo()
    {
        int n = processos.Length;
        var estado = new int[n];   // 0 = não visitado, 1 = na pilha, 2 = pronto
        var pilha = new List<int>();
        for (int i = 0; i < n; i++)
        {
            var ciclo = Visitar(i, estado, pilha);
            if (ciclo != null) return ciclo;
        }
        return null;
    }

    List<int> Visitar(int v, int[] estado, List<int> pilha)
    {
        if (estado[v] == 2) return null;
        if (estado[v] == 1) return pilha.GetRange(pilha.IndexOf(v), pilha.Count - pilha.IndexOf(v));
        estado[v] = 1; pilha.Add(v);
        for (int k = 0; k < esperas.Length; k++)
        {
            if (cortada[k] || esperas[k].de != v) continue;
            var c = Visitar(esperas[k].para, estado, pilha);
            if (c != null) return c;
        }
        pilha.RemoveAt(pilha.Count - 1); estado[v] = 2;
        return null;
    }

    /// <summary>Ordem em que os processos rodam (quem é esperado roda antes).</summary>
    List<int> OrdemDeExecucao()
    {
        int n = processos.Length;
        var faltam = new int[n];
        for (int k = 0; k < esperas.Length; k++) if (!cortada[k]) faltam[esperas[k].de]++;
        var ordem = new List<int>();
        var feito = new bool[n];
        while (ordem.Count < n)
        {
            int prox = -1;
            for (int i = 0; i < n; i++) if (!feito[i] && faltam[i] == 0) { prox = i; break; }
            if (prox < 0) break;
            feito[prox] = true; ordem.Add(prox);
            for (int k = 0; k < esperas.Length; k++) if (!cortada[k] && esperas[k].para == prox) faltam[esperas[k].de]--;
        }
        return ordem;
    }

    /// <summary>Executa os processos (público para o teste automático).</summary>
    public void Executar()
    {
        if (executando || concluido) return;
        StartCoroutine(Rodar());
    }

    IEnumerator Rodar()
    {
        executando = true;
        var tema = TemaPixel.Instancia;
        var ciclo = AcharCiclo();
        if (ciclo != null)
        {
            var nomes = new List<string>();
            foreach (int v in ciclo) nomes.Add("\"" + processos[v].nome + "\"");
            nomes.Add("\"" + processos[ciclo[0]].nome + "\"");
            for (int t = 0; t < 3; t++)   // os processos do ciclo piscam: travados
            {
                foreach (int v in ciclo) Pintar(v, t % 2 == 0 ? tema.corVermelho : tema.corTexto);
                yield return new WaitForSeconds(0.18f);
            }
            foreach (int v in ciclo) Pintar(v, tema.corVermelho);
            Erro("Deadlock: " + string.Join(" espera ", nomes) + ". Ninguém anda.", botaoExecutar);
            yield return new WaitForSeconds(0.6f);
            foreach (int v in ciclo) Pintar(v, tema.corTexto);
            executando = false;
            yield break;
        }

        var ordem = OrdemDeExecucao();
        var passos = new List<string>();
        foreach (int v in ordem)
        {
            Pintar(v, tema.corVerde);
            passos.Add(processos[v].nome);
            Feedback("rodando: " + string.Join("  >  ", passos));
            yield return new WaitForSeconds(0.45f);
        }
        concluido = true;
        interagivel = false;
        Feedback(string.Join("  >  ", passos) + "\nSentir-se capaz vem depois de começar, não antes.");
        GameManager.Instance?.ProgressoNoPuzzle();
        storyState?.ConcluirPuzzle(numeroPuzzle);
        executando = false;
        Invoke(nameof(Fechar), 3f);
    }

    void Pintar(int v, Color cor)
    {
        if (nos == null || nos[v] == null) return;
        var bp = nos[v].GetComponent<BotaoPixel>();
        bp.corTexto = cor; bp.corTextoHover = cor;
        if (bp.texto != null) bp.texto.color = cor;
    }

    void Erro(string msg, Button tremer)
    {
        GameManager.Instance?.ErroNoPuzzle();
        Feedback(msg);
        if (textoFeedback != null) textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
        if (tremer != null) tremer.SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);
    }

    void Feedback(string msg) { if (textoFeedback != null) textoFeedback.text = msg; }
}
