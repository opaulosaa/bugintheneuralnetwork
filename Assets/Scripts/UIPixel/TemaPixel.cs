using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tema da UI pixelada (fontes e sprites) e uma "fábrica" de elementos para os puzzles montarem
/// a própria interface em tempo de execução, sempre no mesmo estilo. Fica no Canvas_UI.
/// Preenchido por Bug In The Neural Network > Versão Cris > 7. Puzzles mais complexos.
/// </summary>
public class TemaPixel : MonoBehaviour
{
    public Font fonteTexto;      // VT323
    public Font fonteTitulo;     // Press Start 2P
    public Sprite painel, botao, botaoHover, papel, alfinete, cursor, caixaCodigo;

    public Color corTexto = new Color(0.86f, 0.95f, 1f);
    public Color corAmbar = new Color(1f, 0.80f, 0.42f);
    public Color corVerde = new Color(0.55f, 0.95f, 0.70f);
    public Color corVermelho = new Color(1f, 0.45f, 0.52f);
    public Color corApagada = new Color(0.55f, 0.62f, 0.75f);
    public Color corTinta = new Color(0.14f, 0.12f, 0.18f);

    public const float MULT = 0.25f;   // 1 px do sprite = 4 unidades do Canvas

    static TemaPixel instancia;
    public static TemaPixel Instancia
    {
        get
        {
            if (instancia == null) instancia = FindFirstObjectByType<TemaPixel>(FindObjectsInactive.Include);
            return instancia;
        }
    }

    // ---------------------------------------------------------------- fábrica

    public static RectTransform Area(Transform pai, string nome, Vector2 pos, Vector2 tam)
    {
        var g = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)g.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    public Text Texto(Transform pai, string nome, string conteudo, int tamanho, Color cor, TextAnchor alinhamento, Vector2 pos, Vector2 tam, bool titulo = false)
    {
        var rt = Area(pai, nome, pos, tam);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = titulo ? fonteTitulo : fonteTexto;
        t.text = conteudo;
        t.color = cor;
        t.alignment = alinhamento;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMaxSize = tamanho;
        t.resizeTextMinSize = Mathf.Min(tamanho, titulo ? 8 : 14);
        t.fontSize = tamanho;
        t.lineSpacing = 0.95f;
        t.raycastTarget = false;
        return t;
    }

    public Image Imagem(Transform pai, string nome, Sprite spr, Color cor, Vector2 pos, Vector2 tam, bool fatiada)
    {
        var rt = Area(pai, nome, pos, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = spr;
        img.color = cor;
        img.type = fatiada ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = MULT;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Botão pixel com borda, cursor ">" e animações (BotaoPixel).</summary>
    public Button Botao(Transform pai, string nome, string rotulo, Vector2 pos, Vector2 tam, int fonte = 28, bool cursorVisivel = true)
    {
        var img = Imagem(pai, nome, botao, Color.white, pos, tam, true);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = img;

        float esq = cursorVisivel ? 48f : 16f;
        var t = Texto(img.transform, "Texto", rotulo, fonte, corTexto, TextAnchor.MiddleCenter, Vector2.zero, tam);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = new Vector2(esq, 4f); t.rectTransform.offsetMax = new Vector2(-16f, -4f);

        Image cur = null;
        if (cursorVisivel)
        {
            cur = Imagem(img.transform, "Cursor_Pixel", cursor, corAmbar, Vector2.zero, new Vector2(16f, 24f), false);
            cur.rectTransform.anchorMin = cur.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            cur.rectTransform.anchoredPosition = new Vector2(26f, 0f);
            cur.enabled = false;
        }

        var bp = img.gameObject.AddComponent<BotaoPixel>();
        bp.fundo = img; bp.texto = t; bp.cursor = cur;
        bp.spriteNormal = botao; bp.spriteHover = botaoHover;
        return b;
    }

    /// <summary>Folha de papel (post-it, cartão) clicável, com alfinete.</summary>
    public Button Papel(Transform pai, string nome, Color cor, Vector2 pos, Vector2 tam)
    {
        var img = Imagem(pai, nome, papel, cor, pos, tam, true);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = img;
        if (alfinete != null)
        {
            var pin = Imagem(img.transform, "Alfinete", alfinete, Color.white, Vector2.zero, new Vector2(20f, 20f), false);
            pin.rectTransform.anchorMin = pin.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            pin.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        }
        var bp = img.gameObject.AddComponent<BotaoPixel>();
        bp.fundo = img; bp.spriteNormal = null; bp.spriteHover = null;
        bp.corTexto = corTinta; bp.corTextoHover = corTinta;
        return b;
    }

    /// <summary>
    /// Seta de "de" até "para" (centros de dois retângulos de meio-tamanho "meioDe"/"meioPara"),
    /// recuada para começar e terminar na borda dos retângulos. "desvio" afasta a seta para o lado
    /// (para duas setas em sentidos opostos não ficarem uma em cima da outra).
    /// </summary>
    public Image Seta(Transform pai, string nome, Vector2 de, Vector2 para, Vector2 meioDe, Vector2 meioPara, Color cor, float desvio = 0f)
    {
        Vector2 d = para - de;
        float len = d.magnitude;
        if (len < 1f) len = 1f;
        Vector2 u = d / len;
        Vector2 n = new Vector2(-u.y, u.x) * desvio;
        Vector2 a = de + n + u * (Recuo(u, meioDe) + 4f);
        Vector2 b = para + n - u * (Recuo(u, meioPara) + 6f);
        float comp = Mathf.Max(8f, (b - a).magnitude);
        var linha = Imagem(pai, nome, null, cor, (a + b) * 0.5f, new Vector2(comp, 4f), false);
        linha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(u.y, u.x) * Mathf.Rad2Deg);
        var ponta = Imagem(linha.transform, "Ponta", cursor, cor, new Vector2(comp * 0.5f - 4f, 0f), new Vector2(12f, 18f), false);
        ponta.raycastTarget = false;
        return linha;
    }

    // Distância do centro até a borda do retângulo, na direção u.
    static float Recuo(Vector2 u, Vector2 meio)
    {
        float tx = Mathf.Abs(u.x) > 0.0001f ? meio.x / Mathf.Abs(u.x) : float.MaxValue;
        float ty = Mathf.Abs(u.y) > 0.0001f ? meio.y / Mathf.Abs(u.y) : float.MaxValue;
        return Mathf.Min(tx, ty);
    }

    /// <summary>Remove o conteúdo montado antes (para montar de novo sem duplicar).</summary>
    public static RectTransform NovoConteudo(Transform painel)
    {
        Transform velho = painel.Find("Conteudo");
        if (velho != null) Destroy(velho.gameObject);
        var rt = Area(painel, "Conteudo", Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // Fica abaixo das scanlines.
        Transform sc = painel.Find("Scanlines");
        if (sc != null) rt.SetSiblingIndex(sc.GetSiblingIndex());
        return rt;
    }
}
