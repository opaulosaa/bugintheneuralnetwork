using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= MENU INICIAL ESTILIZADO =======================
// Refaz o menu com a cara do jogo: fundo azul-noite translúcido (o quarto aparece por trás),
// uma rede neural animada com neurônios "com bug", título com glitch, relógio 03:12 piscando,
// botões âmbar/ciano com animação e entrada em sequência. Cores tiradas do quarto:
// ciano da cortina, âmbar da luminária/amanhecer, vermelho do despertador, lilás do edredom.
public static partial class SceneSetup
{
    const string PASTA_UI = "Assets/UI/Menu";

    static readonly Color MENU_CIANO = new Color(0.30f, 0.85f, 0.95f);
    static readonly Color MENU_AMBAR = new Color(1f, 0.70f, 0.35f);
    static readonly Color MENU_VERMELHO = new Color(1f, 0.28f, 0.25f);
    static readonly Color MENU_TEXTO = new Color(0.80f, 0.83f, 0.92f);

    [MenuItem("Bug In The Neural Network/Estilizar Menu Inicial")]
    public static void EstilizarMenuInicial()
    {
        // Base (botões já ligados ao TelaInicial) e, por cima, o visual novo.
        AdicionarTelaInicial();
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null) return;
        Transform painel = canvasObj.transform.Find("Painel_Inicial");
        if (painel == null) { Debug.LogError("Painel_Inicial não encontrado."); return; }

        Sprite circulo = GerarSprite("Neuronio_Brilho", 64, 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
            float a = Mathf.Clamp01(1f - d);
            return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
        });
        Sprite gradiente = GerarSprite("Fundo_Gradiente", 4, 256, (x, y) =>
            Color.Lerp(new Color(0.09f, 0.05f, 0.15f), new Color(0.02f, 0.03f, 0.08f), y / 255f));
        Sprite vinheta = GerarSprite("Vinheta", 256, 256, (x, y) =>
        {
            Vector2 p = new Vector2(x / 255f - 0.5f, y / 255f - 0.5f) * 2f;
            float d = Mathf.Clamp01((p.magnitude - 0.55f) / 0.9f);
            return new Color(0f, 0f, 0f, d * d);
        });
        Sprite arredondado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---------- Fundo ----------
        Image fundo = painel.GetComponent<Image>();
        fundo.sprite = gradiente;
        fundo.color = new Color(1f, 1f, 1f, 0.9f);
        var grupoMenu = painel.gameObject.AddComponent<CanvasGroup>();
        grupoMenu.alpha = 1f;

        RectTransform rede = NovoRect(painel, "Rede_Neural", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rede.SetSiblingIndex(0);
        Image imgVinheta = NovaImagem(painel, "Vinheta", vinheta, new Color(0f, 0f, 0f, 0.85f));
        Esticar(imgVinheta.rectTransform);
        imgVinheta.transform.SetSiblingIndex(1);

        // ---------- Título ----------
        Image brilho = NovaImagem(painel, "Brilho_Titulo", circulo, new Color(MENU_AMBAR.r, MENU_AMBAR.g, MENU_AMBAR.b, 0.10f));
        Ancorar(brilho.rectTransform, new Vector2(0.5f, 0.75f), new Vector2(1500, 360));
        brilho.transform.SetSiblingIndex(2);

        var entrada = new List<CanvasGroup>();

        Text chamada = CriarTexto(painel, "Chamada", "D E N T R O   D A   C A B E Ç A   D E   C R I S", 17,
            new Vector2(0.5f, 0.845f), new Vector2(0.5f, 0.845f), Vector2.zero, new Vector2(1200, 30));
        chamada.color = new Color(MENU_CIANO.r, MENU_CIANO.g, MENU_CIANO.b, 0.85f);
        entrada.Add(Grupo(chamada.gameObject));

        Text titulo = painel.Find("Titulo").GetComponent<Text>();
        RectTransform blocoTitulo = NovoRect(painel, "Bloco_Titulo", new Vector2(0.5f, 0.765f), new Vector2(0.5f, 0.765f), Vector2.zero, new Vector2(1500, 110));
        titulo.transform.SetParent(blocoTitulo, false);
        Ancorar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 110));
        titulo.fontSize = 76;
        titulo.fontStyle = FontStyle.Bold;
        titulo.color = new Color(0.95f, 0.96f, 1f);
        var sombra = titulo.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(MENU_CIANO.r, MENU_CIANO.g, MENU_CIANO.b, 0.45f);
        sombra.effectDistance = new Vector2(0f, -4f);
        Text glitchR = CopiaGlitch(titulo, "Titulo_Glitch_Vermelho");
        Text glitchC = CopiaGlitch(titulo, "Titulo_Glitch_Ciano");
        titulo.transform.SetAsLastSibling();
        entrada.Add(Grupo(blocoTitulo.gameObject));

        Text subtitulo = painel.Find("Subtitulo").GetComponent<Text>();
        subtitulo.fontSize = 27;
        subtitulo.fontStyle = FontStyle.Italic;
        subtitulo.color = MENU_CIANO;
        Ancorar(subtitulo.rectTransform, new Vector2(0.5f, 0.685f), new Vector2(1200, 40));
        entrada.Add(Grupo(subtitulo.gameObject));

        Image divisor = NovaImagem(painel, "Divisor", null, new Color(MENU_AMBAR.r, MENU_AMBAR.g, MENU_AMBAR.b, 0.8f));
        Ancorar(divisor.rectTransform, new Vector2(0.5f, 0.648f), new Vector2(220, 3));
        entrada.Add(Grupo(divisor.gameObject));

        Text contexto = painel.Find("Contexto").GetComponent<Text>();
        contexto.fontSize = 23;
        contexto.lineSpacing = 1.15f;
        contexto.color = MENU_TEXTO;
        Ancorar(contexto.rectTransform, new Vector2(0.5f, 0.545f), new Vector2(980, 170));
        entrada.Add(Grupo(contexto.gameObject));

        // ---------- Botões ----------
        string[] nomes = { "BotaoComecar", "BotaoComoJogar", "BotaoSair" };
        float[] alturas = { 0.385f, 0.305f, 0.228f };
        for (int i = 0; i < nomes.Length; i++)
        {
            Transform b = painel.Find(nomes[i]);
            if (b == null) continue;
            Ancorar((RectTransform)b, new Vector2(0.5f, alturas[i]), new Vector2(440, 66));
            EstilizarBotao(b.gameObject, arredondado, i == 0);
            entrada.Add(Grupo(b.gameObject));
        }

        // ---------- Relógio (a madrugada antes da apresentação) ----------
        RectTransform blocoRelogio = NovoRect(painel, "Relogio", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -50f), new Vector2(320, 110));
        blocoRelogio.pivot = new Vector2(1f, 1f);
        Text hora = CriarTexto(blocoRelogio, "Hora", "16:40", 52, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -32f), new Vector2(320, 64));
        hora.fontStyle = FontStyle.Bold;
        hora.alignment = TextAnchor.MiddleRight;
        hora.color = MENU_VERMELHO;
        var sombraHora = hora.gameObject.AddComponent<Shadow>();
        sombraHora.effectColor = new Color(MENU_VERMELHO.r, MENU_VERMELHO.g, MENU_VERMELHO.b, 0.35f);
        sombraHora.effectDistance = new Vector2(0f, -3f);
        Text vespera = CriarTexto(blocoRelogio, "Legenda", "apresentação amanhã, 9h", 17, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -82f), new Vector2(320, 26));
        vespera.alignment = TextAnchor.MiddleRight;
        vespera.color = new Color(MENU_TEXTO.r, MENU_TEXTO.g, MENU_TEXTO.b, 0.6f);
        entrada.Add(Grupo(blocoRelogio.gameObject));

        // ---------- Aviso de conteúdo ----------
        Text aviso = CriarTexto(painel, "Aviso_Conteudo",
            "Este jogo fala sobre depressão e ansiedade. Se precisar conversar, o CVV atende de graça, 24h, pelo 188 ou cvv.org.br.",
            17, new Vector2(0.5f, 0.055f), new Vector2(0.5f, 0.055f), Vector2.zero, new Vector2(1300, 30));
        aviso.color = new Color(MENU_TEXTO.r, MENU_TEXTO.g, MENU_TEXTO.b, 0.55f);
        entrada.Add(Grupo(aviso.gameObject));

        // ---------- "Como jogar" no mesmo estilo ----------
        Transform cj = painel.Find("Painel_ComoJogar");
        if (cj != null)
        {
            cj.SetAsLastSibling();
            Image cjFundo = cj.GetComponent<Image>();
            cjFundo.sprite = gradiente;
            cjFundo.color = new Color(1f, 1f, 1f, 0.97f);
            cj.gameObject.AddComponent<CanvasGroup>();
            Text cjTitulo = cj.Find("Titulo").GetComponent<Text>();
            cjTitulo.color = MENU_AMBAR;
            cjTitulo.fontSize = 48;
            Text cjTexto = cj.Find("Texto").GetComponent<Text>();
            cjTexto.color = MENU_TEXTO;
            cjTexto.lineSpacing = 1.2f;
            Image cjDivisor = NovaImagem(cj, "Divisor", null, new Color(MENU_CIANO.r, MENU_CIANO.g, MENU_CIANO.b, 0.8f));
            Ancorar(cjDivisor.rectTransform, new Vector2(0.5f, 0.835f), new Vector2(220, 3));
            Transform voltar = cj.Find("BotaoVoltar");
            if (voltar != null)
            {
                Ancorar((RectTransform)voltar, new Vector2(0.5f, 0.1f), new Vector2(440, 66));
                EstilizarBotao(voltar.gameObject, arredondado, false);
            }
        }

        // ---------- Animação ----------
        var anim = painel.gameObject.AddComponent<MenuNeural>();
        anim.areaRede = rede;
        anim.spriteNeuronio = circulo;
        anim.entrada = entrada.ToArray();
        anim.titulo = titulo;
        anim.glitchVermelho = glitchR;
        anim.glitchCiano = glitchC;
        anim.brilhoTitulo = brilho.rectTransform;
        anim.relogio = hora;

        // O HUD dos puzzles fica escondido enquanto o menu está aberto.
        var tela = Object.FindFirstObjectByType<TelaInicial>();
        var hud = Object.FindFirstObjectByType<PuzzleProgressHUD>();
        if (tela != null && hud != null)
        {
            tela.esconderNoMenu = new[] { hud.gameObject };
            EditorUtility.SetDirty(tela);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Menu inicial estilizado. Salve a cena (Ctrl+S) e dê Play para ver as animações.");
    }

    // ----- Utilitários do menu -----

    static void EstilizarBotao(GameObject b, Sprite sprite, bool principal)
    {
        Image img = b.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        Button botao = b.GetComponent<Button>();
        botao.transition = Selectable.Transition.None;   // as cores ficam por conta do BotaoMenu

        Text txt = b.GetComponentInChildren<Text>();
        txt.fontSize = principal ? 27 : 23;
        txt.fontStyle = principal ? FontStyle.Bold : FontStyle.Normal;

        Image barra = NovaImagem(b.transform, "Destaque", null, principal ? new Color(1f, 1f, 1f, 0.55f) : MENU_CIANO);
        RectTransform br = barra.rectTransform;
        br.anchorMin = br.anchorMax = new Vector2(0f, 0.5f);
        br.pivot = new Vector2(0f, 0.5f);
        br.anchoredPosition = new Vector2(10f, 0f);
        br.sizeDelta = new Vector2(6f, 30f);

        var anim = b.AddComponent<BotaoMenu>();
        anim.fundo = img;
        anim.texto = txt;
        anim.destaque = br;
        if (principal)
        {
            anim.corFundo = new Color(MENU_AMBAR.r, MENU_AMBAR.g, MENU_AMBAR.b, 0.92f);
            anim.corFundoHover = new Color(1f, 0.82f, 0.50f, 1f);
            anim.corTexto = new Color(0.14f, 0.07f, 0.10f);
            anim.corTextoHover = new Color(0.05f, 0.02f, 0.04f);
        }
        else
        {
            anim.corFundo = new Color(MENU_CIANO.r, MENU_CIANO.g, MENU_CIANO.b, 0.10f);
            anim.corFundoHover = new Color(MENU_CIANO.r, MENU_CIANO.g, MENU_CIANO.b, 0.28f);
            anim.corTexto = new Color(0.75f, 0.95f, 1f);
            anim.corTextoHover = Color.white;
        }
    }

    static Text CopiaGlitch(Text original, string nome)
    {
        Text c = Object.Instantiate(original, original.transform.parent);
        c.name = nome;
        var s = c.GetComponent<Shadow>();
        if (s != null) Object.DestroyImmediate(s);
        c.color = new Color(1f, 1f, 1f, 0f);
        c.raycastTarget = false;
        return c;
    }

    static CanvasGroup Grupo(GameObject g)
    {
        var cg = g.GetComponent<CanvasGroup>();
        if (cg == null) cg = g.AddComponent<CanvasGroup>();
        return cg;
    }

    static RectTransform NovoRect(Transform pai, string nome, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 tam)
    {
        var g = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)g.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static Image NovaImagem(Transform pai, string nome, Sprite sprite, Color cor)
    {
        var g = new GameObject(nome, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(pai, false);
        var img = g.GetComponent<Image>();
        img.sprite = sprite;
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Ancorar(RectTransform rt, Vector2 ancora, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = ancora;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = tamanho;
    }

    // Gera uma textura simples por código e importa como Sprite (sem depender de arte externa).
    static Sprite GerarSprite(string nome, int w, int h, System.Func<int, int, Color> cor)
    {
        if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(PASTA_UI)) AssetDatabase.CreateFolder("Assets/UI", "Menu");
        string caminho = PASTA_UI + "/" + nome + ".png";
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, cor(x, y));
        tex.Apply();
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(caminho);
        var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
        imp.textureType = TextureImporterType.Sprite;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }
}
