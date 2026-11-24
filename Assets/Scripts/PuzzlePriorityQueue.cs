using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 1: fila de prioridade (escrivaninha com post-its).
///
/// Cada tarefa tem um prazo e pode depender de outra. A regra é a de uma fila de prioridade
/// com dependências: sempre retirar da fila a tarefa LIBERADA (tudo de que ela depende já foi feito)
/// com o prazo mais próximo. O que vence depois de hoje fica na fila: hoje não precisa caber tudo.
///
/// A armadilha: "Ensaiar" vence antes de "Terminar os slides", mas depende dos slides.
/// A interface (post-its) é montada pelo próprio script dentro do painel, no estilo da UI pixel (TemaPixel).
/// </summary>
public class PuzzlePriorityQueue : Interactable
{
    [System.Serializable]
    public class Tarefa
    {
        public string nome;
        [Tooltip("Como o prazo aparece no post-it.")]
        public string prazo;
        [Tooltip("Prazo em minutos a partir de 0h de hoje (para comparar).")]
        public int minutos;
        [Tooltip("Índices das tarefas que precisam estar feitas antes.")]
        public int[] dependeDe = new int[0];
        public bool paraHoje = true;
    }

    public GameObject painel;
    public Text textoFeedback;
    public StoryState storyState;

    public Tarefa[] tarefas =
    {
        new Tarefa { nome = "Comer algo", prazo = "agora (17h)", minutos = 17 * 60 },
        new Tarefa { nome = "Corrigir o bug do projeto", prazo = "hoje, 20h", minutos = 20 * 60, dependeDe = new[] { 0 } },
        new Tarefa { nome = "Terminar os slides", prazo = "hoje, 23h", minutos = 23 * 60, dependeDe = new[] { 1 } },
        new Tarefa { nome = "Ensaiar a apresentação", prazo = "hoje, 22h", minutos = 22 * 60, dependeDe = new[] { 2 } },
        new Tarefa { nome = "Dormir", prazo = "meia-noite", minutos = 24 * 60 },
        new Tarefa { nome = "Estudar para a prova de Redes", prazo = "sexta-feira", minutos = 4 * 24 * 60, paraHoje = false },
    };
    [Tooltip("Em que ordem os post-its aparecem na tela (índices de 'tarefas').")]
    public int[] ordemNaTela = { 5, 3, 0, 4, 1, 2 };

    public static readonly Color[] CORES =
    {
        new Color(0.98f, 0.86f, 0.38f), new Color(0.96f, 0.62f, 0.72f), new Color(0.56f, 0.80f, 0.96f),
        new Color(0.62f, 0.88f, 0.60f), new Color(1f, 0.70f, 0.44f), new Color(0.80f, 0.70f, 0.98f)
    };

    // ----- legado (cena antiga e ferramentas antigas) -----
    [System.Serializable] public class PostIt { public Button botao; public int ordemCorreta; }
    [HideInInspector] public PostIt[] postIts = new PostIt[0];

    private bool concluido;
    private bool[] feito;
    private Button[] cartoes;
    private Text[] linhasDep;
    private Text[] carimbos;

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

    // ---------------------------------------------------------------- interface

    void Montar()
    {
        var tema = TemaPixel.Instancia;
        feito = new bool[tarefas.Length];
        if (tema == null || painel == null) { Debug.LogWarning("PuzzlePriorityQueue: sem TemaPixel no Canvas, a interface não foi montada."); return; }

        var conteudo = TemaPixel.NovoConteudo(painel.transform);
        cartoes = new Button[tarefas.Length];
        linhasDep = new Text[tarefas.Length];
        carimbos = new Text[tarefas.Length];
        var itens = new List<RectTransform>();
        Vector2 tam = new Vector2(320f, 150f);
        float[] giros = { -2f, 1.5f, -1f, 2f, -1.5f, 1f };

        for (int k = 0; k < ordemNaTela.Length && k < tarefas.Length; k++)
        {
            int i = ordemNaTela[k];
            Vector2 pos = new Vector2(((k % 3) - 1) * 345f, k < 3 ? 78f : -86f);
            var b = tema.Papel(conteudo, "Tarefa_" + i, CORES[k % CORES.Length], pos, tam);
            b.transform.localRotation = Quaternion.Euler(0f, 0f, giros[k % giros.Length]);
            var nome = tema.Texto(b.transform, "Nome", tarefas[i].nome, 30, tema.corTinta, TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(290f, 62f));
            b.GetComponent<BotaoPixel>().texto = nome;
            tema.Texto(b.transform, "Prazo", "prazo: " + tarefas[i].prazo, 24, new Color(0.25f, 0.20f, 0.28f), TextAnchor.MiddleCenter, new Vector2(0f, -16f), new Vector2(290f, 28f));
            linhasDep[i] = tema.Texto(b.transform, "Depende", "", 22, tema.corTinta, TextAnchor.MiddleCenter, new Vector2(0f, -46f), new Vector2(290f, 28f));
            carimbos[i] = tema.Texto(b.transform, "Carimbo", "FEITO", 20, new Color(0.85f, 0.15f, 0.2f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(260f, 40f), true);
            carimbos[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            carimbos[i].enabled = false;
            b.GetComponent<BotaoPixel>().alphaDesativado = 0.55f;
            var fl = b.gameObject.AddComponent<FlutuarUI>();
            fl.amplitudePixels = 0f; fl.giroGraus = 1f; fl.frequencia = 0.3f + 0.06f * k;

            int indice = i;
            b.onClick.AddListener(() => Clicar(indice));
            cartoes[i] = b;
            itens.Add((RectTransform)b.transform);
        }
        AtualizarDependencias();

        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) pp.itens = itens.ToArray();
    }

    void AtualizarDependencias()
    {
        if (linhasDep == null) return;
        var tema = TemaPixel.Instancia;
        for (int i = 0; i < tarefas.Length; i++)
        {
            if (linhasDep[i] == null) continue;
            var dep = tarefas[i].dependeDe;
            if (!tarefas[i].paraHoje) { linhasDep[i].text = "não vence hoje"; linhasDep[i].color = new Color(0.30f, 0.28f, 0.34f); continue; }
            if (dep == null || dep.Length == 0) { linhasDep[i].text = "não depende de nada"; linhasDep[i].color = new Color(0.30f, 0.28f, 0.34f); continue; }
            var nomes = new List<string>();
            bool pronto = true;
            foreach (int d in dep) { nomes.Add(tarefas[d].nome); if (!feito[d]) pronto = false; }
            linhasDep[i].text = (pronto ? "liberada (" : "depende de: ") + string.Join(", ", nomes) + (pronto ? " feito)" : "");
            linhasDep[i].color = pronto ? new Color(0.10f, 0.45f, 0.20f) : new Color(0.65f, 0.10f, 0.15f);
        }
    }

    // ---------------------------------------------------------------- regras

    /// <summary>Primeira dependência ainda não feita (ou -1 se a tarefa está liberada).</summary>
    int DependenciaPendente(int i)
    {
        if (tarefas[i].dependeDe == null) return -1;
        foreach (int d in tarefas[i].dependeDe) if (!feito[d]) return d;
        return -1;
    }

    /// <summary>Topo da fila: a tarefa de hoje, liberada e não feita, com o prazo mais próximo.</summary>
    public int TopoDaFila()
    {
        int melhor = -1;
        for (int i = 0; i < tarefas.Length; i++)
        {
            if (feito[i] || !tarefas[i].paraHoje || DependenciaPendente(i) >= 0) continue;
            if (melhor < 0 || tarefas[i].minutos < tarefas[melhor].minutos) melhor = i;
        }
        return melhor;
    }

    /// <summary>Clique num post-it (público para o teste automático).</summary>
    public void Clicar(int i)
    {
        if (concluido || feito == null || feito[i]) return;
        var t = tarefas[i];

        if (!t.paraHoje)
        {
            Erro(i, "\"" + t.nome + "\" vence " + t.prazo + ". Fica na fila para outro dia: hoje não precisa caber tudo.");
            return;
        }
        int pendente = DependenciaPendente(i);
        if (pendente >= 0)
        {
            Erro(i, "\"" + t.nome + "\" está bloqueada: depende de \"" + tarefas[pendente].nome + "\", que ainda não foi feita.");
            return;
        }
        int topo = TopoDaFila();
        if (topo != i)
        {
            Erro(i, "Tem tarefa liberada que vence antes: \"" + tarefas[topo].nome + "\" (" + tarefas[topo].prazo + ").");
            return;
        }

        feito[i] = true;
        GameManager.Instance?.ProgressoNoPuzzle();
        if (cartoes != null && cartoes[i] != null) cartoes[i].interactable = false;
        if (carimbos != null && carimbos[i] != null) carimbos[i].enabled = true;
        AtualizarDependencias();

        if (TopoDaFila() < 0)
        {
            concluido = true;
            interagivel = false;
            Feedback("Fila de hoje vazia. A prova de Redes continua na fila, para amanhã. E tudo bem.");
            storyState?.ConcluirPuzzle1();
            Invoke(nameof(Fechar), 2.4f);
        }
        else Feedback("Feito: " + t.nome + ". Qual é a próxima da fila?");
    }

    void Erro(int i, string msg)
    {
        GameManager.Instance?.ErroNoPuzzle();
        Feedback(msg);
        if (textoFeedback != null) textoFeedback.SendMessage("Repetir", SendMessageOptions.DontRequireReceiver);
        if (cartoes != null && cartoes[i] != null) cartoes[i].SendMessage("Tremer", SendMessageOptions.DontRequireReceiver);
    }

    void Feedback(string msg) { if (textoFeedback != null) textoFeedback.text = msg; }

    /// <summary>Ordem certa de cliques (usada pelo teste automático).</summary>
    public List<int> OrdemCerta()
    {
        var salvo = (bool[])feito.Clone();
        var ordem = new List<int>();
        int topo;
        while ((topo = TopoDaFila()) >= 0) { ordem.Add(topo); feito[topo] = true; }
        feito = salvo;
        return ordem;
    }
}
