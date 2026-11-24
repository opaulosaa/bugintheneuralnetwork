using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// ======================= PUZZLES 4 E 5 E CENA FINAL =======================
//  - Puzzle 4 (Deadlock): uma mesinha com um notebook num canto livre do quarto, longe dos outros puzzles.
//  - Puzzle 5 (Coletor de lixo): nos sacos de lixo mais afastados dos outros puzzles.
//  - Painéis 4 e 5 copiados do painel 3 (mesmo visual pixel); a interface é montada ao dar Play.
//  - Progresso passa a ter 5 puzzles (StoryState, HUD, etiquetas "PUZZLE n DE 5", cortina, "Como jogar").
//  - A arrumação do quarto passa a ter 5 partes.
//  - Cena final pixelada depois da mensagem final (CenaFinalPixel), com botão "Continuar".
// Rodar de novo não duplica nada.
public static partial class SceneSetup
{
    const string ENUNCIADO_P4 =
        "Os processos de Cris travaram esperando uns pelos outros: um deadlock. Corte a espera que só existe " +
        "na cabeça de Cris para quebrar o ciclo e execute. Esperas reais não podem ser cortadas.";
    const string ENUNCIADO_P5 =
        "A memória de Cris está cheia. Faça como o coletor de lixo: a partir de Cris (hoje), marque tudo o que " +
        "ainda é alcançável seguindo as setas. Depois varra: o que não foi marcado é liberado.";
    const string PASTA_FINAL = "Assets/UI/Pixel/Final/";

    [MenuItem("Bug In The Neural Network/Versão Cris/8. Puzzles 4 e 5 e cena final", false, 18)]
    public static void MontarPuzzles45ECenaFinal()
    {
        if (!CarregarUIPixel()) return;
        MontarPuzzlesV2();   // tema + painéis 1 a 3

        GameObject canvasObj = GameObject.Find("Canvas_UI");
        var story = Object.FindFirstObjectByType<StoryState>();
        var p3 = Object.FindFirstObjectByType<PuzzleFirewall>();
        if (canvasObj == null || story == null || p3 == null || p3.painel == null)
        {
            Debug.LogError("Faltam Canvas_UI, StoryState ou o Puzzle 3 na cena.");
            return;
        }
        Physics.SyncTransforms();

        // ---------- Puzzle 4: mesinha com notebook ----------
        var p4 = Object.FindFirstObjectByType<PuzzleDeadlock>();
        if (p4 == null)
        {
            Vector3 lugar = LugarLivre(PosicoesAEvitar(), 0f);
            var raiz = new GameObject("Notebook_Deadlock");
            raiz.transform.position = lugar;
            Vector3 centro = new Vector3(0f, 0f, 0f) - lugar; centro.y = 0f;
            float rotY = Quaternion.LookRotation(centro.sqrMagnitude > 0.01f ? centro.normalized : Vector3.forward).eulerAngles.y;
            var mesa = KenneyEm(raiz.transform, "sideTable", lugar, rotY, 0f, 0.55f, "y", 0f);
            float topo = 0.55f;
            Bounds bm;
            if (mesa != null && TentarBounds(mesa, out bm)) topo = bm.max.y;
            KenneyEm(raiz.transform, "laptop", lugar, rotY + 180f, 0f, 0.36f, "x", topo);
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(0.9f, 1.2f, 0.9f);
            p4 = raiz.AddComponent<PuzzleDeadlock>();
            Debug.Log("Puzzle 4: notebook em " + lugar);
        }
        p4.textoPrompt = "Puzzle 4 · Aperte E: Notebook";
        p4.storyState = story;
        p4.numeroPuzzle = 4;

        // ---------- Puzzle 5: sacos de lixo ----------
        var p5 = Object.FindFirstObjectByType<PuzzleColetorLixo>();
        if (p5 == null)
        {
            var evitar = PosicoesAEvitar();
            Transform melhor = null; float melhorDist = -1f;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("SacoLixo")) continue;
                float d = MenorDistancia(t.position, evitar);
                if (d > melhorDist) { melhorDist = d; melhor = t; }
            }
            Vector3 lugar;
            var raiz = new GameObject("Lixo_Coletor");
            if (melhor != null && melhorDist >= 1.6f) lugar = new Vector3(melhor.position.x, 0f, melhor.position.z);
            else
            {
                lugar = LugarLivre(evitar, 0f);
                KenneyEm(raiz.transform, "trashcan", lugar, 0f, 0f, 0.6f, "y", 0f);
            }
            raiz.transform.position = lugar;
            var col = raiz.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1f, 1f, 1f);
            p5 = raiz.AddComponent<PuzzleColetorLixo>();
            Debug.Log("Puzzle 5: sacos de lixo em " + lugar);
        }
        p5.textoPrompt = "Puzzle 5 · Aperte E: Sacos de lixo";
        p5.storyState = story;
        p5.numeroPuzzle = 5;

        // ---------- Painéis 4 e 5 (cópias do painel 3) ----------
        p4.painel = PainelCopiado(canvasObj.transform, p3.painel, "Painel_Puzzle4", ENUNCIADO_P4, "PUZZLE 4 DE 5 · DEADLOCK");
        p5.painel = PainelCopiado(canvasObj.transform, p3.painel, "Painel_Puzzle5", ENUNCIADO_P5, "PUZZLE 5 DE 5 · COLETOR DE LIXO");
        p4.textoFeedback = p4.painel.transform.Find("Feedback").GetComponent<Text>();
        p5.textoFeedback = p5.painel.transform.Find("Feedback").GetComponent<Text>();
        EditorUtility.SetDirty(p4); EditorUtility.SetDirty(p5);

        // ---------- Progresso com 5 puzzles ----------
        story.totalPuzzles = 5;
        EditorUtility.SetDirty(story);
        var hud = Object.FindFirstObjectByType<PuzzleProgressHUD>();
        if (hud != null)
        {
            hud.nomes = new[] { "Fila de prioridade", "Loop de pensamento", "Firewall social", "Deadlock", "Coletor de lixo" };
            EditorUtility.SetDirty(hud);
        }
        for (int i = 1; i <= 3; i++)
        {
            Transform badge = canvasObj.transform.Find("Painel_Puzzle" + i + "/Badge_Puzzle/Texto");
            if (badge == null) continue;
            var t = badge.GetComponent<Text>();
            t.text = t.text.Replace(" DE 3 ", " DE 5 ");
            EditorUtility.SetDirty(t);
        }
        Transform bloq = canvasObj.transform.Find("Painel_Bloqueado/Titulo");
        if (bloq != null) { var t = bloq.GetComponent<Text>(); t.text = "Ainda não é hora.\nConclua os 5 puzzles primeiro."; EditorUtility.SetDirty(t); }
        TextosNeutrosCris();
        MontarArrumacaoEBatimentos();
        LayoutCaixas(canvasObj.transform);

        // ---------- Cena final ----------
        Transform cf = canvasObj.transform.Find("CenaFinal");
        GameObject cenaObj = cf != null ? cf.gameObject : new GameObject("CenaFinal", typeof(RectTransform));
        cenaObj.transform.SetParent(canvasObj.transform, false);
        var cena = cenaObj.GetComponent<CenaFinalPixel>();
        if (cena == null) cena = cenaObj.AddComponent<CenaFinalPixel>();
        cena.pessoaParado = Quadros("pessoa_parado_", 4);
        cena.pessoaAndar = Quadros("pessoa_andar_", 4);
        cena.pessoaCansado = Quadros("pessoa_cansado_", 2);
        cena.pessoaComemorar = AssetDatabase.LoadAssetAtPath<Sprite>(PASTA_FINAL + "pessoa_comemorar.png");
        cena.coracao = AssetDatabase.LoadAssetAtPath<Sprite>(PASTA_FINAL + "coracao.png");
        cena.neuronio = sprNeuronio;
        cena.vinheta = sprVinhetaPx;
        cena.scanline = texScanline;
        EditorUtility.SetDirty(cena);
        var final = Object.FindFirstObjectByType<PuzzleFinal>();
        if (final != null) { final.cenaFinal = cena; EditorUtility.SetDirty(final); }

        MarcarCena();
        Debug.Log("Puzzles 4 e 5 e cena final prontos. Salve a cena (Ctrl+S) e teste no Play.");
    }

    static Sprite[] Quadros(string prefixo, int n)
    {
        var lista = new List<Sprite>();
        for (int i = 0; i < n; i++)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(PASTA_FINAL + prefixo + i + ".png");
            if (s != null) lista.Add(s);
        }
        return lista.ToArray();
    }

    static GameObject PainelCopiado(Transform canvas, GameObject modelo, string nome, string enunciado, string etiqueta)
    {
        Transform velho = canvas.Find(nome);
        GameObject p = velho != null ? velho.gameObject : (GameObject)Object.Instantiate(modelo, canvas);
        p.name = nome;
        p.SetActive(false);
        LimparPainel(p.transform, enunciado);
        Transform badge = p.transform.Find("Badge_Puzzle/Texto");
        if (badge != null) { var t = badge.GetComponent<Text>(); t.text = etiqueta; EditorUtility.SetDirty(t); }
        // Fica junto dos outros painéis, abaixo do menu e da cena final.
        Transform menu = canvas.Find("Painel_Inicial");
        if (menu != null) p.transform.SetSiblingIndex(menu.GetSiblingIndex());
        return p;
    }

    static List<Vector3> PosicoesAEvitar()
    {
        var lista = new List<Vector3>();
        foreach (var it in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None)) lista.Add(it.transform.position);
        GameObject jogador = GameObject.Find("Faisca");
        if (jogador != null) lista.Add(jogador.transform.position);
        return lista;
    }

    static float MenorDistancia(Vector3 p, List<Vector3> pontos)
    {
        float menor = float.MaxValue;
        foreach (var q in pontos) menor = Mathf.Min(menor, new Vector2(p.x - q.x, p.z - q.z).magnitude);
        return menor;
    }

    // Ponto encostado numa parede, sem móvel em cima nem na frente, o mais longe possível dos pontos a evitar.
    static Vector3 LugarLivre(List<Vector3> evitar, float _)
    {
        float xi = REF_X - 0.55f, zi = REF_Z - 0.55f;
        Vector3 melhor = new Vector3(0f, 0f, -zi);
        float melhorDist = -1f;
        for (float x = -xi; x <= xi + 0.001f; x += 0.25f)
            for (float z = -zi; z <= zi + 0.001f; z += 0.25f)
            {
                bool naParede = Mathf.Abs(x) > xi - 0.3f || Mathf.Abs(z) > zi - 0.3f;
                if (!naParede) continue;
                Vector3 p = new Vector3(x, 0.6f, z);
                if (Physics.CheckBox(p, new Vector3(0.4f, 0.5f, 0.4f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vector3 dentro = new Vector3(-x, 0f, -z).normalized;
                if (Physics.CheckBox(p + dentro * 0.9f, new Vector3(0.3f, 0.5f, 0.3f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                float d = MenorDistancia(p, evitar);
                if (d > melhorDist) { melhorDist = d; melhor = new Vector3(x, 0f, z); }
            }
        return melhor;
    }
}
