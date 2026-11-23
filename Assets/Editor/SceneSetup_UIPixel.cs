using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= UI PIXELADA (menu e caixas dos puzzles) =======================
// O jogo continua em 3D; só o menu e as caixas (puzzles, cortina bloqueada, final) ganham o visual pixel:
// fontes pixel (VT323 e Press Start 2P), bordas pixeladas em 9-slice ampliadas 4x sem suavizar,
// post-its de verdade no puzzle 1, chaves liga/desliga no puzzle 3, scanlines de CRT e animações
// (caixa "ligando", título digitado, itens caindo em degraus, botão com cursor piscando, erro tremendo).
// Os sprites ficam em Assets/UI/Pixel (gerados por Tools/gerar_ui_pixel.py). Rodar de novo não duplica nada.
public static partial class SceneSetup
{
    const string UIPX = "Assets/UI/Pixel/";
    const float UIPX_MULT = 0.25f;   // 1 px do sprite = 4 unidades do Canvas (1920x1080)

    static Font fonteTexto, fonteTitulo;
    static Sprite sprPainel, sprBotao, sprBotaoHover, sprEtiqueta, sprPostit, sprChaveOn, sprChaveOff, sprCursor, sprNeuronio, sprVinhetaPx;
    static Texture2D texScanline;

    [MenuItem("Bug In The Neural Network/Versão Cris/5. UI pixelada (menu e puzzles)", false, 15)]
    public static void EstilizarUIPixel()
    {
        if (!CarregarUIPixel()) return;
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null) { Debug.LogError("Canvas_UI não encontrado."); return; }
        Transform c = canvasObj.transform;

        int n = 0;
        foreach (string nome in new[] { "Painel_Puzzle1", "Painel_Puzzle2", "Painel_Puzzle3", "Painel_Bloqueado", "Painel_Final" })
        {
            Transform p = c.Find(nome);
            if (p == null) continue;
            EstilizarCaixa(p, nome.StartsWith("Painel_Puzzle"));
            n++;
        }

        Transform menu = c.Find("Painel_Inicial");
        if (menu != null) EstilizarMenuPixel(menu);

        LayoutCaixas(c);

        MarcarCena();
        Debug.Log("UI pixelada aplicada em " + n + " caixas" + (menu != null ? " e no menu" : "") + ". Salve a cena (Ctrl+S).");
    }

    static bool CarregarUIPixel()
    {
        fonteTexto = AssetDatabase.LoadAssetAtPath<Font>(UIPX + "Fontes/VT323-Regular.ttf");
        fonteTitulo = AssetDatabase.LoadAssetAtPath<Font>(UIPX + "Fontes/PressStart2P-Regular.ttf");
        sprPainel = Spr("ui_painel"); sprBotao = Spr("ui_botao"); sprBotaoHover = Spr("ui_botao_hover"); sprEtiqueta = Spr("ui_etiqueta");
        sprPostit = Spr("ui_postit"); sprChaveOn = Spr("ui_chave_on"); sprChaveOff = Spr("ui_chave_off"); sprCursor = Spr("ui_cursor");
        sprNeuronio = Spr("ui_neuronio"); sprVinhetaPx = Spr("ui_vinheta");
        texScanline = AssetDatabase.LoadAssetAtPath<Texture2D>(UIPX + "ui_scanline.png");
        if (fonteTexto == null || fonteTitulo == null || sprPainel == null || texScanline == null)
        {
            Debug.LogError("Sprites/fontes da UI pixel não encontrados em " + UIPX + ". Rode Tools/gerar_ui_pixel.py e volte ao Unity.");
            return false;
        }
        return true;
    }

    static Sprite Spr(string nome) { return AssetDatabase.LoadAssetAtPath<Sprite>(UIPX + nome + ".png"); }

    // ---------------------------------------------------------------- caixas
    static void EstilizarCaixa(Transform painel, bool ehPuzzle)
    {
        Image fundo = painel.GetComponent<Image>();
        if (fundo != null) Fatiar(fundo, sprPainel, Color.white);

        foreach (var t in painel.GetComponentsInChildren<Text>(true)) FontePixel(t, painel);

        Transform badge = painel.Find("Badge_Puzzle");
        if (badge != null)
        {
            Fatiar(badge.GetComponent<Image>(), sprEtiqueta, Color.white);
            ((RectTransform)badge).anchoredPosition = new Vector2(0f, -8f);   // encaixada na borda de cima
        }

        var itens = new List<RectTransform>();
        int indicePost = 0;
        foreach (Transform filho in painel)
        {
            var botao = filho.GetComponent<Button>();
            var toggle = filho.GetComponent<Toggle>();
            if (botao != null)
            {
                if (filho.name.StartsWith("Post_")) PostIt(botao, indicePost++);
                else BotaoPx(botao);
                itens.Add((RectTransform)filho);
            }
            else if (toggle != null) { ChavePx(toggle); itens.Add((RectTransform)filho); }
            else if (filho.name == "Feedback")
            {
                var txt = filho.GetComponent<Text>();
                txt.color = new Color(1f, 0.80f, 0.42f);
                if (filho.GetComponent<TextoDigitado>() == null) filho.gameObject.AddComponent<TextoDigitado>();
            }
        }

        Text titulo = painel.Find("Titulo") != null ? painel.Find("Titulo").GetComponent<Text>() : null;
        if (titulo != null) titulo.color = new Color(0.86f, 0.95f, 1f);

        if (ehPuzzle)
        {
            Transform dica = painel.Find("Dica_Esc");
            Text tDica = dica != null ? dica.GetComponent<Text>() : CriarTexto(painel, "Dica_Esc", "[ESC] fechar", 24,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-110f, 28f), new Vector2(200, 30));
            tDica.font = fonteTexto; tDica.fontSize = 28; tDica.alignment = TextAnchor.MiddleRight;
            tDica.color = new Color(0.45f, 0.75f, 0.85f, 0.7f);
        }

        Scanlines(painel, 12f);

        var cg = painel.GetComponent<CanvasGroup>();
        if (cg == null) cg = painel.gameObject.AddComponent<CanvasGroup>();
        var pp = painel.GetComponent<PainelPixel>();
        if (pp == null) pp = painel.gameObject.AddComponent<PainelPixel>();
        pp.titulo = titulo;
        pp.itens = itens.ToArray();
        EditorUtility.SetDirty(pp);
    }

    static void Fatiar(Image img, Sprite spr, Color cor)
    {
        if (img == null) return;
        img.sprite = spr;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = UIPX_MULT;
        img.color = cor;
        EditorUtility.SetDirty(img);
    }

    // Títulos curtos em caixa alta (rótulos, título do jogo, relógio) usam Press Start 2P; o resto, VT323.
    static void FontePixel(Text t, Transform raiz)
    {
        if (t.font == fonteTexto || t.font == fonteTitulo) return;   // já convertido
        string s = t.text ?? "";
        bool rotulo = t.name.StartsWith("Titulo_Glitch") || t.name == "Chamada" || t.name == "Hora" ||
                      (t.transform.parent != null && t.transform.parent.name == "Badge_Puzzle") ||
                      (t.name == "Titulo" && (raiz.name == "Painel_Inicial" || raiz.name == "Painel_ComoJogar") && t.transform.parent == raiz) ||
                      (t.name == "Titulo" && t.transform.parent != null && t.transform.parent.name == "Bloco_Titulo") ||
                      (t.name == "Titulo" && t.transform.parent != null && t.transform.parent.name == "Painel_ComoJogar");
        if (rotulo)
        {
            t.font = fonteTitulo;
            t.fontSize = Mathf.Max(12, Mathf.RoundToInt(t.fontSize * 0.6f / 4f) * 4);
            t.fontStyle = FontStyle.Normal;
            if (t.name == "Titulo" || t.name.StartsWith("Titulo_Glitch")) t.text = s.ToUpperInvariant();
        }
        else
        {
            t.font = fonteTexto;
            t.fontSize = Mathf.RoundToInt(t.fontSize * 1.45f / 2f) * 2;
            t.fontStyle = FontStyle.Normal;
            t.lineSpacing = 0.95f;
        }
        if (t.resizeTextForBestFit) { t.resizeTextMaxSize = t.fontSize; t.resizeTextMinSize = Mathf.Min(t.resizeTextMinSize, t.fontSize); }
        EditorUtility.SetDirty(t);
    }

    static void BotaoPx(Button b)
    {
        var bm = b.GetComponent<BotaoMenu>();
        if (bm != null)
        {
            if (bm.destaque != null) bm.destaque.gameObject.SetActive(false);
            Object.DestroyImmediate(bm);
        }
        b.transition = Selectable.Transition.None;
        b.transform.localScale = Vector3.one;
        Image img = b.GetComponent<Image>();
        Fatiar(img, sprBotao, Color.white);
        Text txt = b.GetComponentInChildren<Text>(true);

        Transform cur = b.transform.Find("Cursor_Pixel");
        Image cursor = cur != null ? cur.GetComponent<Image>() : null;
        if (cursor == null)
        {
            var g = new GameObject("Cursor_Pixel", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(b.transform, false);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(20f, 28f);
            rt.anchoredPosition = new Vector2(28f, 0f);
            cursor = g.GetComponent<Image>();
            cursor.sprite = sprCursor;
            cursor.raycastTarget = false;
            cursor.color = new Color(1f, 0.75f, 0.35f);
        }
        cursor.enabled = false;

        var bp = b.GetComponent<BotaoPixel>();
        if (bp == null) bp = b.gameObject.AddComponent<BotaoPixel>();
        bp.fundo = img; bp.texto = txt; bp.cursor = cursor;
        bp.spriteNormal = sprBotao; bp.spriteHover = sprBotaoHover;
        EditorUtility.SetDirty(bp);
    }

    static readonly Color[] CORES_POSTIT =
    {
        new Color(0.98f, 0.86f, 0.38f), new Color(0.96f, 0.62f, 0.72f), new Color(0.56f, 0.80f, 0.96f),
        new Color(0.62f, 0.88f, 0.60f), new Color(1f, 0.70f, 0.44f)
    };

    // Puzzle 1: os botões viram post-its de verdade, lado a lado e meio tortos.
    static void PostIt(Button b, int i)
    {
        b.transition = Selectable.Transition.None;
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(152f, 136f);   // 19x17 px de sprite, ampliados 8x
        rt.anchoredPosition = new Vector2((i - 2) * 168f, -20f + (i % 2 == 0 ? 8f : -8f));
        float[] giros = { -3f, 2f, -1.5f, 3f, -2.5f };
        rt.localRotation = Quaternion.Euler(0f, 0f, giros[i % giros.Length]);

        Image img = b.GetComponent<Image>();
        img.sprite = sprPostit; img.type = Image.Type.Simple; img.color = CORES_POSTIT[i % CORES_POSTIT.Length];
        Text txt = b.GetComponentInChildren<Text>(true);
        if (txt != null)
        {
            txt.font = fonteTexto; txt.fontSize = 26; txt.lineSpacing = 0.85f;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.offsetMin = new Vector2(6f, 14f); txt.rectTransform.offsetMax = new Vector2(-6f, -14f);
            EditorUtility.SetDirty(txt);
        }

        var bp = b.GetComponent<BotaoPixel>();
        if (bp == null) bp = b.gameObject.AddComponent<BotaoPixel>();
        bp.fundo = img; bp.texto = txt; bp.cursor = null; bp.spriteNormal = null; bp.spriteHover = null;
        bp.corTexto = new Color(0.14f, 0.12f, 0.18f); bp.corTextoHover = new Color(0.05f, 0.04f, 0.08f);
        var fl = b.GetComponent<FlutuarUI>();
        if (fl == null) fl = b.gameObject.AddComponent<FlutuarUI>();
        fl.amplitudePixels = 0f; fl.giroGraus = 1.5f; fl.frequencia = 0.35f + 0.07f * i;
        EditorUtility.SetDirty(bp); EditorUtility.SetDirty(fl);
    }

    // Puzzle 3: cada regra vira uma chave liga/desliga pixelada.
    static void ChavePx(Toggle tg)
    {
        Transform fundoT = tg.transform.Find("Fundo");
        Image fundo = fundoT != null ? fundoT.GetComponent<Image>() : tg.targetGraphic as Image;
        Image marca = tg.graphic as Image;
        if (fundo != null)
        {
            fundo.sprite = sprChaveOff; fundo.type = Image.Type.Simple; fundo.color = Color.white;
            var rt = fundo.rectTransform; rt.sizeDelta = new Vector2(88f, 44f); rt.anchoredPosition = new Vector2(48f, 0f);
            EditorUtility.SetDirty(fundo);
        }
        if (marca != null)
        {
            marca.sprite = sprChaveOn; marca.type = Image.Type.Simple; marca.color = Color.white;
            marca.rectTransform.offsetMin = Vector2.zero; marca.rectTransform.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(marca);
        }
        tg.transition = Selectable.Transition.None;
        Transform lab = tg.transform.Find("Label");
        Text rotulo = lab != null ? lab.GetComponent<Text>() : null;
        if (rotulo != null) { rotulo.rectTransform.offsetMin = new Vector2(112f, -22f); rotulo.fontSize = Mathf.Max(rotulo.fontSize, 28); }
        var ch = tg.GetComponent<ChaveAnimada>();
        if (ch == null) ch = tg.gameObject.AddComponent<ChaveAnimada>();
        ch.chave = fundo != null ? fundo.rectTransform : null;
        ch.rotulo = rotulo;
        EditorUtility.SetDirty(ch); EditorUtility.SetDirty(tg);
    }

    static void Scanlines(Transform pai, float margem)
    {
        Transform velho = pai.Find("Scanlines");
        RawImage ri = velho != null ? velho.GetComponent<RawImage>() : null;
        if (ri == null)
        {
            var g = new GameObject("Scanlines", typeof(RectTransform), typeof(RawImage));
            g.transform.SetParent(pai, false);
            ri = g.GetComponent<RawImage>();
        }
        var rt = ri.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, margem); rt.offsetMax = new Vector2(-margem, -margem);
        ri.texture = texScanline;
        ri.color = new Color(1f, 1f, 1f, 0.22f);
        ri.raycastTarget = false;
        ri.transform.SetAsLastSibling();
        if (ri.GetComponent<ScanlinesUI>() == null) ri.gameObject.AddComponent<ScanlinesUI>();
    }

    // ---------------------------------------------------------------- menu
    static void EstilizarMenuPixel(Transform menu)
    {
        foreach (var t in menu.GetComponentsInChildren<Text>(true)) FontePixel(t, menu);

        foreach (string nome in new[] { "BotaoComecar", "BotaoComoJogar", "BotaoSair" })
        {
            Transform b = menu.Find(nome);
            if (b != null && b.GetComponent<Button>() != null) BotaoPx(b.GetComponent<Button>());
        }
        Transform cj = menu.Find("Painel_ComoJogar");
        if (cj != null)
        {
            Transform voltar = cj.Find("BotaoVoltar");
            if (voltar != null && voltar.GetComponent<Button>() != null) BotaoPx(voltar.GetComponent<Button>());
            Scanlines(cj, 0f);
        }

        var mn = menu.GetComponent<MenuNeural>();
        if (mn != null) { mn.spriteNeuronio = sprNeuronio; EditorUtility.SetDirty(mn); }

        // Vinheta pixelada e scanlines por cima de tudo do menu.
        Transform v = menu.Find("Vinheta_Pixel");
        Image vin = v != null ? v.GetComponent<Image>() : null;
        if (vin == null)
        {
            var g = new GameObject("Vinheta_Pixel", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(menu, false);
            vin = g.GetComponent<Image>();
        }
        vin.sprite = sprVinhetaPx; vin.color = new Color(1f, 1f, 1f, 0.9f); vin.raycastTarget = false;
        var vrt = vin.rectTransform; vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = vrt.offsetMax = Vector2.zero;
        Scanlines(menu, 0f);
        vin.transform.SetSiblingIndex(menu.Find("Scanlines").GetSiblingIndex());
        if (cj != null) cj.SetAsLastSibling();   // "Como jogar" abre por cima; ele tem as próprias scanlines
        menu.Find("Scanlines").SetAsLastSibling();
    }

    // ---------------------------------------------------------------- layout dos textos
    // Cada texto ganha uma área fixa dentro da caixa e "Best Fit": a fonte diminui sozinha até caber,
    // então nada sai da caixa nem fica cortado, por maior que seja a mensagem.
    [MenuItem("Bug In The Neural Network/Versão Cris/6. Corrigir textos das caixas", false, 16)]
    public static void CorrigirTextosCaixas()
    {
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null) { Debug.LogError("Canvas_UI não encontrado."); return; }
        LayoutCaixas(canvasObj.transform);
        MarcarCena();
        Debug.Log("Textos das caixas reposicionados. Salve a cena (Ctrl+S).");
    }

    static void LayoutCaixas(Transform c)
    {
        for (int i = 1; i <= 3; i++)
        {
            Transform p = c.Find("Painel_Puzzle" + i);
            if (p == null) continue;
            Tamanho(p, 1100f, 780f);
            Text titulo = TextoFilho(p, "Titulo");
            if (titulo != null) { Faixa(titulo.rectTransform, true, 70f, 150f, 70f); Caber(titulo, 22, 36, TextAnchor.UpperCenter); }
            Text fb = TextoFilho(p, "Feedback");
            if (fb != null) { Faixa(fb.rectTransform, false, 56f, 120f, 70f); Caber(fb, 20, 32, TextAnchor.MiddleCenter); }

            if (i == 1)
                for (int k = 0; k < 5; k++)
                {
                    Transform b = p.Find("Post_" + k);
                    if (b == null) continue;
                    Item(b, (k - 2) * 182f, 10f + (k % 2 == 0 ? 8f : -8f), 164f, 148f);
                    Text t = b.GetComponentInChildren<Text>(true);
                    if (t != null)
                    {
                        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                        t.rectTransform.offsetMin = new Vector2(10f, 16f); t.rectTransform.offsetMax = new Vector2(-10f, -18f);
                        Caber(t, 16, 26, TextAnchor.MiddleCenter);
                    }
                }
            else if (i == 2)
                for (int k = 0; k < 4; k++)
                {
                    Transform b = p.Find("Opcao_" + k);
                    if (b == null) continue;
                    Item(b, 0f, 120f - k * 80f, 940f, 66f);
                    TextoDoBotao(b, TextAnchor.MiddleLeft);
                }
            else
            {
                for (int k = 0; k < 4; k++)
                {
                    Transform r = p.Find("Regra_" + k);
                    if (r == null) continue;
                    Item(r, 0f, 136f - k * 66f, 940f, 56f);
                    Transform lab = r.Find("Label");
                    Text t = lab != null ? lab.GetComponent<Text>() : null;
                    if (t != null)
                    {
                        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                        t.rectTransform.offsetMin = new Vector2(112f, 0f); t.rectTransform.offsetMax = Vector2.zero;
                        Caber(t, 18, 30, TextAnchor.MiddleLeft);
                    }
                }
                Transform ap = p.Find("BotaoAplicar");
                if (ap != null) { Item(ap, 0f, -150f, 360f, 64f); TextoDoBotao(ap, TextAnchor.MiddleCenter); }
            }
        }

        Transform bloq = c.Find("Painel_Bloqueado");
        if (bloq != null)
        {
            Tamanho(bloq, 820f, 260f);
            Text t = TextoFilho(bloq, "Titulo");
            if (t != null) { Preencher(t.rectTransform, 50f, 40f); Caber(t, 22, 36, TextAnchor.MiddleCenter); }
        }

        Transform fin = c.Find("Painel_Final");
        if (fin != null)
        {
            Tamanho(fin, 1100f, 620f);
            Text t = TextoFilho(fin, "Titulo");
            if (t != null) { Preencher(t.rectTransform, 80f, 70f); Caber(t, 24, 40, TextAnchor.MiddleCenter); t.lineSpacing = 1f; }
        }
    }

    static Text TextoFilho(Transform p, string nome)
    {
        Transform t = p.Find(nome);
        return t != null ? t.GetComponent<Text>() : null;
    }

    static void Tamanho(Transform p, float w, float h)
    {
        var rt = (RectTransform)p;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }

    // Faixa horizontal colada no topo (ou na base) da caixa, com margens laterais.
    static void Faixa(RectTransform rt, bool topo, float distancia, float altura, float margem)
    {
        rt.anchorMin = new Vector2(0f, topo ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, topo ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, topo ? 1f : 0f);
        rt.offsetMin = new Vector2(margem, 0f); rt.offsetMax = new Vector2(-margem, 0f);
        rt.sizeDelta = new Vector2(-2f * margem, altura);
        rt.anchoredPosition = new Vector2(0f, topo ? -distancia : distancia);
        EditorUtility.SetDirty(rt);
    }

    static void Preencher(RectTransform rt, float margemX, float margemY)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margemX, margemY); rt.offsetMax = new Vector2(-margemX, -margemY);
        EditorUtility.SetDirty(rt);
    }

    static void Item(Transform t, float x, float y, float w, float h)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        EditorUtility.SetDirty(rt);
    }

    static void TextoDoBotao(Transform b, TextAnchor alinhamento)
    {
        Text t = null;
        foreach (var x in b.GetComponentsInChildren<Text>(true)) { t = x; break; }
        if (t == null) return;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        bool esquerda = alinhamento == TextAnchor.MiddleLeft;
        rt.offsetMin = new Vector2(esquerda ? 60f : 24f, 6f); rt.offsetMax = new Vector2(-24f, -6f);
        Caber(t, 18, 30, alinhamento);
    }

    static void Caber(Text t, int minimo, int maximo, TextAnchor alinhamento)
    {
        t.alignment = alinhamento;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = minimo;
        t.resizeTextMaxSize = maximo;
        t.fontSize = maximo;
        t.supportRichText = true;   // a digitação usa <color> transparente
        EditorUtility.SetDirty(t);
    }
}

// Importação dos sprites e fontes da UI pixel: sem suavização, sem compressão, bordas 9-slice.
public class ImportadorUIPixel : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.StartsWith("Assets/UI/Pixel/")) return;
        var ti = (TextureImporter)assetImporter;
        string nome = Path.GetFileNameWithoutExtension(p);
        ti.textureType = nome == "ui_scanline" ? TextureImporterType.Default : TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.spritePixelsPerUnit = 100f;
        ti.wrapMode = nome == "ui_scanline" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (nome.StartsWith("ui_painel") || nome.StartsWith("ui_botao") || nome == "ui_etiqueta")
            ti.spriteBorder = new Vector4(5f, 5f, 5f, 5f);
    }

    void OnPreprocessAsset()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.StartsWith("Assets/UI/Pixel/Fontes/")) return;
        var fi = assetImporter as TrueTypeFontImporter;
        if (fi != null) fi.fontRenderingMode = FontRenderingMode.HintedRaster;
    }
}
