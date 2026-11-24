using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// ======================= PUZZLES MAIS COMPLEXOS =======================
// Os três puzzles passam a montar a própria interface em tempo de execução (no estilo da UI pixel),
// a partir do TemaPixel que fica no Canvas_UI. Esta ferramenta:
//  - cria/atualiza o TemaPixel com as fontes e sprites de Assets/UI/Pixel;
//  - limpa dos painéis os botões e chaves antigos (fica só a etiqueta, o título, o feedback e as scanlines);
//  - escreve o novo enunciado de cada puzzle.
// Puzzle 1: fila de prioridade com prazos e dependências.
// Puzzle 2: montar o laço (condição de saída + o que muda dentro dele) e executar.
// Puzzle 3: firewall com regras em ordem (a primeira que combina decide) e prévia das mensagens.
public static partial class SceneSetup
{
    const string ENUNCIADO_P1 =
        "A apresentação é amanhã às 9h. Monte a fila de hoje: pegue sempre a tarefa LIBERADA com o prazo mais próximo. " +
        "Uma tarefa só fica liberada depois que aquilo de que ela depende foi feito.";
    const string ENUNCIADO_P2 =
        "Um pensamento está rodando em laço na cabeça de Cris. Monte o laço: escolha a condição de saída e o que " +
        "acontece dentro dele. Depois, execute para ver se ele termina.";
    const string ENUNCIADO_P3 =
        "O celular de Cris filtra as mensagens com regras. Elas são lidas de cima para baixo, e a primeira que combina " +
        "decide. Deixe chegar quem quer ajudar e barre só o ruído.";

    [MenuItem("Bug In The Neural Network/Versão Cris/7. Puzzles mais complexos", false, 17)]
    public static void MontarPuzzlesV2()
    {
        if (!CarregarUIPixel()) return;
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null) { Debug.LogError("Canvas_UI não encontrado."); return; }

        var tema = canvasObj.GetComponent<TemaPixel>();
        if (tema == null) tema = canvasObj.AddComponent<TemaPixel>();
        tema.fonteTexto = fonteTexto;
        tema.fonteTitulo = fonteTitulo;
        tema.painel = sprPainel;
        tema.botao = sprBotao;
        tema.botaoHover = sprBotaoHover;
        tema.cursor = sprCursor;
        tema.papel = Spr("ui_papel") != null ? Spr("ui_papel") : sprBotao;
        tema.alfinete = Spr("ui_alfinete");
        tema.caixaCodigo = Spr("ui_codigo");
        EditorUtility.SetDirty(tema);

        var p1 = Object.FindFirstObjectByType<PuzzlePriorityQueue>();
        var p2 = Object.FindFirstObjectByType<PuzzleLoop>();
        var p3 = Object.FindFirstObjectByType<PuzzleFirewall>();
        int n = 0;
        if (p1 != null && p1.painel != null)
        {
            LimparPainel(p1.painel.transform, ENUNCIADO_P1);
            p1.postIts = new PuzzlePriorityQueue.PostIt[0];
            EditorUtility.SetDirty(p1); n++;
        }
        if (p2 != null && p2.painel != null)
        {
            LimparPainel(p2.painel.transform, ENUNCIADO_P2);
            p2.opcoes = new PuzzleLoop.Opcao[0];
            EditorUtility.SetDirty(p2); n++;
        }
        if (p3 != null && p3.painel != null)
        {
            LimparPainel(p3.painel.transform, ENUNCIADO_P3);
            p3.regras = new Toggle[0];
            p3.estadoCorreto = new bool[0];
            p3.botaoAplicar = null;
            EditorUtility.SetDirty(p3); n++;
        }

        MarcarCena();
        Debug.Log("Puzzles mais complexos: " + n + " painéis preparados. A interface de cada puzzle é montada ao dar Play. Salve a cena (Ctrl+S).");
    }

    static void LimparPainel(Transform painel, string enunciado)
    {
        var ficam = new HashSet<string> { "Badge_Puzzle", "Titulo", "Feedback", "Scanlines", "Dica_Esc" };
        for (int i = painel.childCount - 1; i >= 0; i--)
        {
            Transform f = painel.GetChild(i);
            if (!ficam.Contains(f.name)) Object.DestroyImmediate(f.gameObject);
        }
        Transform t = painel.Find("Titulo");
        if (t != null)
        {
            var txt = t.GetComponent<Text>();
            txt.text = enunciado;
            txt.supportRichText = true;
            EditorUtility.SetDirty(txt);
        }
        Transform fb = painel.Find("Feedback");
        if (fb != null) { var txt = fb.GetComponent<Text>(); txt.text = ""; EditorUtility.SetDirty(txt); }
        var pp = painel.GetComponent<PainelPixel>();
        if (pp != null) { pp.itens = new RectTransform[0]; EditorUtility.SetDirty(pp); }
    }
}
