using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle 5: coletor de lixo (sacos de lixo).
///
/// A memória de Cris é um grafo de objetos ligados por referências (setas). Como num coletor de lixo
/// "marcar e varrer" (mark and sweep):
///  1. Marcar: a partir da raiz, "Cris (hoje)", o jogador marca tudo o que é alcançável seguindo as
///     setas. Só dá para marcar um objeto se algum objeto já marcado aponta para ele.
///  2. Varrer: tudo o que não foi marcado é liberado.
/// Armadilhas: o "comentário de 2019" aponta para algo vivo, mas ninguém aponta para ele (é lixo);
/// "travou na aula" e "sou um fracasso" apontam um para o outro, um ciclo de lixo que contagem de
/// referências não liberaria, mas o coletor que parte da raiz libera.
/// A interface é montada pelo próprio script (TemaPixel).
/// </summary>
public class PuzzleColetorLixo : Interactable
{
    [System.Serializable]
    public class Objeto
    {
        public string nome;
        public Vector2 posicao;
        [Tooltip("Índices dos objetos para os quais este aponta.")]
        public int[] aponta = new int[0];
    }

    public GameObject painel;
    public Text textoFeedback;
    public StoryState storyState;
    public int numeroPuzzle = 5;

    public Objeto[] objetos =
    {
        new Objeto { nome = "Cris (hoje)", posicao = new Vector2(-470f, 20f), aponta = new[] { 1, 3, 4 } },
        new Objeto { nome = "apresentação de amanhã", posicao = new Vector2(-160f, 150f), aponta = new[] { 2 } },
        new Objeto { nome = "slides", posicao = new Vector2(150f, 175f) },
        new Objeto { nome = "Bia", posicao = new Vector2(-160f, 25f) },
        new Objeto { nome = "vontade de melhorar", posicao = new Vector2(-160f, -105f), aponta = new[] { 5 } },
        new Objeto { nome = "nota 10 em Banco de Dados", posicao = new Vector2(150f, -120f) },
        new Objeto { nome = "comentário de um colega em 2019", posicao = new Vector2(430f, 110f), aponta = new[] { 1 } },
        new Objeto { nome = "aquela vez que travou na aula", posicao = new Vector2(330f, 20f), aponta = new[] { 8 } },
        new Objeto { nome = "\"eu sou um fracasso\"", posicao = new Vector2(440f, -110f), aponta = new[] { 7 } },
    };

    private bool[] marcado;
    private Button[] botoes;
    private Button botaoVarrer;
    private bool concluido, varrendo;
    static readonly Vector2 MEIO = new Vector2(120f, 30f);

    void Start()
    {
        if (painel != null) painel.SetActive(false);
        marcado = new bool[objetos.Length];
        marcado[0] = true;   // a raiz já começa marcada
        Montar();
    }

    public override void Interagir()
    {
        if (concluido) return;
        if (painel != null) painel.SetActive(true);
    }

    public void Fechar() { if (painel != null) painel.SetActive(false); }
    public override bool PainelAberto => painel != null && painel.activeInHierarchy;
    public override void FecharPainel() { if (!varrendo) Fechar(); }

    // ---------------------------------------------------------------- interface

    void Montar()
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || painel == null) { Debug.LogWarning("PuzzleColetorLixo: sem TemaPixel no Canvas, a interface não foi montada."); return; }
        ((RectTransform)painel.transform).sizeDelta = new Vector2(1260f, 900f);
        var c = TemaPixel.NovoConteudo(painel.transform);
        var grafo = TemaPixel.Area(c, "Memoria", Vector2.zero, new Vector2(1200f, 480f));

        for (int i = 0; i < objetos.Length; i++)
            foreach (int j in objetos[i].aponta)
            {
                bool idaEVolta = System.Array.IndexOf(objetos[j].aponta, i) >= 0;
                tema.Seta(grafo, "Ref_" + i + "_" + j, objetos[i].posicao, objetos[j].posicao, MEIO, MEIO, tema.corApagada, idaEVolta ? 10f : 0f);
            }

        botoes = new Button[objetos.Length];
        for (int i = 0; i < objetos.Length; i++)
        {
            var b = tema.Botao(grafo, "Objeto_" + i, objetos[i].nome, objetos[i].posicao, MEIO * 2f, 24, false);
            int indice = i;
            b.onClick.AddListener(() => Marcar(indice));
            botoes[i] = b;
        }

        tema.Texto(grafo, "Legenda", "A seta = \"aponta para\".  Marque a partir de Cris (hoje) e depois varra.", 22, tema.corApagada,
            TextAnchor.MiddleCenter, new Vector2(0f, 205f), new Vector2(1100f, 28f));

        botaoVarrer = tema.Botao(c, "BotaoVarrer", "Varrer (liberar o que não foi marcado)", new Vector2(0f, -212f), new Vector2(560f, 56f), 28);
        botaoVarrer.onClick.AddListener(Varrer);

        AtualizarVisual();
        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) pp.itens = new[] { grafo, (RectTransform)botaoVarrer.transform };
    }

    void AtualizarVisual()
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || botoes == null) return;
        for (int i = 0; i < objetos.Length; i++)
        {
            var bp = botoes[i].GetComponent<BotaoPixel>();
            bp.selecionado = marcado[i];
            bp.corTexto = i == 0 ? tema.corAmbar : (marcado[i] ? tema.corVerde : tema.corTexto);
            bp.corTextoHover = bp.corTexto;
        }
    }

    // ---------------------------------------------------------------- regras

    bool AlguemMarcadoAponta(int alvo)
    {
        for (int i = 0; i < objetos.Length; i++)
            if (marcado[i] && System.Array.IndexOf(objetos[i].aponta, alvo) >= 0) return true;
        return false;
    }

    /// <summary>O que é alcançável a partir da raiz (busca em largura).</summary>
    public bool[] Alcancaveis()
    {
        var vis = new bool[objetos.Length];
        var fila = new Queue<int>();
        vis[0] = true; fila.Enqueue(0);
        while (fila.Count > 0)
        {
            int v = fila.Dequeue();
            foreach (int j in objetos[v].aponta) if (!vis[j]) { vis[j] = true; fila.Enqueue(j); }
        }
        return vis;
    }

    int QuemAponta(int alvo)
    {
        for (int i = 0; i < objetos.Length; i++) if (System.Array.IndexOf(objetos[i].aponta, alvo) >= 0) return i;
        return -1;
    }

    /// <summary>Marca um objeto (público para o teste automático).</summary>
    public void Marcar(int i)
    {
        if (concluido || varrendo || i <= 0 || i >= objetos.Length || marcado[i]) return;
        if (AlguemMarcadoAponta(i))
        {
            marcado[i] = true;
            AtualizarVisual();
            Feedback("Marcado: " + objetos[i].nome + ".");
            return;
        }
        var alc = Alcancaveis();
        string msg;
        if (alc[i])
            msg = "Ainda não: marque antes quem aponta para \"" + objetos[i].nome + "\" (" + objetos[QuemAponta(i)].nome + ").";
        else if (objetos[i].aponta.Length > 0 && QuemAponta(i) >= 0)
            msg = "Nada marcado aponta para \"" + objetos[i].nome + "\". Ele e \"" + objetos[QuemAponta(i)].nome +
                  "\" apontam um para o outro, mas ninguém alcançável aponta para eles: é um ciclo de lixo.";
        else if (objetos[i].aponta.Length > 0)
            msg = "\"" + objetos[i].nome + "\" aponta para algo vivo, mas ninguém aponta para ele. Isso não o mantém na memória.";
        else
            msg = "Nada marcado aponta para \"" + objetos[i].nome + "\".";
        Erro(msg, botoes != null ? botoes[i] : null);
    }

    /// <summary>Varre: libera o que não foi marcado (público para o teste automático).</summary>
    public void Varrer()
    {
        if (concluido || varrendo) return;
        var alc = Alcancaveis();
        for (int i = 0; i < objetos.Length; i++)
            if (alc[i] && !marcado[i])
            {
                Erro("Se varrer agora, \"" + objetos[i].nome + "\" seria apagado, e ele ainda é alcançável a partir de Cris. Marque tudo o que importa antes.",
                     botaoVarrer);
                return;
            }
        StartCoroutine(Coletar());
    }

    IEnumerator Coletar()
    {
        varrendo = true;
        int liberados = 0;
        for (int i = 0; i < objetos.Length; i++)
        {
            if (marcado[i]) continue;
            liberados++;
            if (botoes != null) StartCoroutine(Sumir((RectTransform)botoes[i].transform));
            yield return new WaitForSeconds(0.25f);
        }
        // As setas que saíam ou chegavam nos objetos liberados somem junto.
        if (botoes != null)
        {
            Transform grafo = botoes[0].transform.parent;
            foreach (Transform t in grafo)
            {
                if (!t.name.StartsWith("Ref_")) continue;
                var partes = t.name.Split('_');
                int de = int.Parse(partes[1]), para = int.Parse(partes[2]);
                if (!marcado[de] || !marcado[para]) t.gameObject.SetActive(false);
            }
        }
        concluido = true;
        interagivel = false;
        Feedback("Coletor rodou: " + liberados + " objetos liberados. O que é alcançável a partir de quem Cris é hoje continua na memória.");
        GameManager.Instance?.ProgressoNoPuzzle();
        storyState?.ConcluirPuzzle(numeroPuzzle);
        varrendo = false;
        Invoke(nameof(Fechar), 3f);
    }

    // O objeto liberado encolhe em degraus de pixel e some.
    IEnumerator Sumir(RectTransform rt)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        for (int k = 5; k >= 0; k--)
        {
            float s = k / 5f;
            rt.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, s), s, 1f);
            cg.alpha = s;
            yield return new WaitForSeconds(0.05f);
        }
        rt.gameObject.SetActive(false);
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
