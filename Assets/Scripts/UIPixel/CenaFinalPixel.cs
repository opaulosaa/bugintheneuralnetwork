using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Cena final em pixel, no estilo do menu (rede neural, título com glitch, scanlines e vinheta).
/// Aparece depois da mensagem final, quando o jogador clica em "Continuar".
///
/// Roteiro: Cris aparece curvado; o jogador aperta ESPAÇO (ou clica) para Cris dar o primeiro passo
/// sozinho; depois as outras pessoas chegam andando dos dois lados e ficam junto; todos pulam juntos,
/// corações sobem e surge "TODOS NÓS PRECISAMOS DE AJUDA"; por fim, uma "conquista desbloqueada",
/// os créditos e os botões.
/// Tudo é montado por este script em tempo de execução, dentro do Canvas.
/// </summary>
public class CenaFinalPixel : MonoBehaviour
{
    [Header("Sprites (Assets/UI/Pixel/Final)")]
    public Sprite[] pessoaParado;
    public Sprite[] pessoaAndar;
    public Sprite[] pessoaCansado;
    public Sprite pessoaComemorar;
    public Sprite coracao;
    public Sprite neuronio;
    public Sprite vinheta;
    public Texture2D scanline;

    [Header("Textos")]
    [TextArea] public string textoAntes = "Cris achava que precisava dar conta de tudo sem ninguém.";
    public string chamadaPasso = "[ESPAÇO] dar o primeiro passo";
    [TextArea] public string textoPasso = "O primeiro passo foi de Cris: abrir a cortina e responder a Bia.";
    [TextArea] public string textoOutros = "E aí os outros vieram também. A Bia, a família, a turma.";
    public string titulo = "TODOS NÓS PRECISAMOS DE AJUDA";
    [TextArea] public string textoFinal = "Pedir ajuda não é vergonha. É um passo seu, e um passo de quem caminha com você.";
    public string conquista = "CONQUISTA DESBLOQUEADA: O PRIMEIRO PASSO";
    [TextArea] public string creditos = "Bug In The Neural Network  ·  Maria Clara Andrade, Enzo Lima, Natan Shinmon e Paulo Alencar  ·  CGRV, Unifor 2026";

    public Color[] coresPessoas =
    {
        new Color(0.70f, 0.85f, 1f), new Color(1f, 0.78f, 0.84f), new Color(0.78f, 1f, 0.80f),
        new Color(1f, 0.92f, 0.65f), new Color(0.86f, 0.80f, 1f), new Color(1f, 0.80f, 0.65f)
    };

    const float ESCALA = 6f;   // 1 px do sprite = 6 unidades do Canvas
    RectTransform raiz;
    Text textoFrase;
    bool mostrando;
    readonly List<Image> pessoas = new List<Image>();

    /// <summary>Põe o botão "Continuar" na caixa da mensagem final.</summary>
    public void Preparar(GameObject painelFinal)
    {
        var tema = TemaPixel.Instancia;
        if (tema == null || painelFinal == null || painelFinal.transform.Find("BotaoContinuar") != null) return;
        var t = painelFinal.transform.Find("Titulo");
        if (t != null) ((RectTransform)t).offsetMin = new Vector2(((RectTransform)t).offsetMin.x, 130f);
        var b = tema.Botao(painelFinal.transform, "BotaoContinuar", "Continuar  >", Vector2.zero, new Vector2(340f, 56f), 30);
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 72f);
        b.onClick.AddListener(Mostrar);
        var sc = painelFinal.transform.Find("Scanlines");
        if (sc != null) sc.SetAsLastSibling();
    }

    public void Mostrar()
    {
        if (mostrando) return;
        mostrando = true;
        foreach (var m in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None)) m.podeMover = false;
        StartCoroutine(Roteiro());
    }

    // ---------------------------------------------------------------- montagem

    void Montar()
    {
        var tema = TemaPixel.Instancia;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        var g = new GameObject("CenaFinal_Pixel", typeof(RectTransform), typeof(CanvasGroup));
        g.SetActive(false);
        raiz = (RectTransform)g.transform;
        raiz.SetParent(canvas.transform, false);
        raiz.anchorMin = Vector2.zero; raiz.anchorMax = Vector2.one; raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        raiz.SetAsLastSibling();

        var fundo = g.AddComponent<Image>();
        fundo.color = new Color(0.03f, 0.04f, 0.08f, 1f);

        var rede = TemaPixel.Area(raiz, "Rede_Neural", Vector2.zero, Vector2.zero);
        rede.anchorMin = Vector2.zero; rede.anchorMax = Vector2.one; rede.offsetMin = rede.offsetMax = Vector2.zero;

        var vin = tema.Imagem(raiz, "Vinheta", vinheta, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, Vector2.zero, false);
        vin.rectTransform.anchorMin = Vector2.zero; vin.rectTransform.anchorMax = Vector2.one;
        vin.rectTransform.offsetMin = vin.rectTransform.offsetMax = Vector2.zero;

        // Título com glitch (as cópias vermelha e ciano são animadas pelo MenuNeural, como no menu).
        var tGlitchR = tema.Texto(raiz, "Titulo_Glitch_Vermelho", "", 40, new Color(1f, 0.2f, 0.3f, 0f), TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(1700f, 70f), true);
        var tGlitchC = tema.Texto(raiz, "Titulo_Glitch_Ciano", "", 40, new Color(0.2f, 0.9f, 1f, 0f), TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(1700f, 70f), true);
        var tTitulo = tema.Texto(raiz, "Titulo", "", 40, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(1700f, 70f), true);

        textoFrase = tema.Texto(raiz, "Frase", "", 44, tema.corTexto, TextAnchor.MiddleCenter, new Vector2(0f, 175f), new Vector2(1400f, 170f));

        var mn = g.AddComponent<MenuNeural>();
        mn.areaRede = rede;
        mn.spriteNeuronio = neuronio;
        mn.titulo = tTitulo;
        mn.glitchVermelho = tGlitchR;
        mn.glitchCiano = tGlitchC;
        mn.distanciaSinapse = 230f;

        var sc = TemaPixel.Area(raiz, "Scanlines", Vector2.zero, Vector2.zero);
        sc.anchorMin = Vector2.zero; sc.anchorMax = Vector2.one; sc.offsetMin = sc.offsetMax = Vector2.zero;
        var ri = sc.gameObject.AddComponent<RawImage>();
        ri.texture = scanline; ri.color = new Color(1f, 1f, 1f, 0.22f); ri.raycastTarget = false;
        sc.gameObject.AddComponent<ScanlinesUI>();

        g.SetActive(true);
    }

    Image NovaPessoa(string nome, Color cor, float x)
    {
        var tema = TemaPixel.Instancia;
        var img = tema.Imagem(raiz, nome, pessoaParado[0], cor, new Vector2(x, -200f), new Vector2(16f, 28f) * ESCALA, false);
        img.rectTransform.pivot = new Vector2(0.5f, 0f);
        img.rectTransform.anchoredPosition = new Vector2(x, -270f);
        raiz.Find("Scanlines").SetAsLastSibling();
        pessoas.Add(img);
        return img;
    }

    // ---------------------------------------------------------------- roteiro

    IEnumerator Roteiro()
    {
        Montar();
        var grupo = raiz.GetComponent<CanvasGroup>();

        // Liga como um monitor antigo.
        for (int k = 0; k <= 6; k++)
        {
            float s = k / 6f;
            grupo.alpha = s;
            raiz.localScale = new Vector3(1f, Mathf.Lerp(0.02f, 1f, s), 1f);
            yield return new WaitForSecondsRealtime(0.05f);
        }
        raiz.localScale = Vector3.one;

        // Cris, curvado, parado no meio.
        var cris = NovaPessoa("Cris", Color.white, 0f);
        var animCris = StartCoroutine(Quadros(cris, pessoaCansado, 2.5f));
        yield return Digitar(textoAntes);
        yield return new WaitForSecondsRealtime(0.8f);

        // O primeiro passo é do jogador (e de Cris): espera ESPAÇO ou um clique no botão.
        var tema = TemaPixel.Instancia;
        bool deuPasso = false;
        var bPasso = tema.Botao(raiz, "BotaoPrimeiroPasso", chamadaPasso, new Vector2(0f, -345f), new Vector2(620f, 60f), 30);
        bPasso.onClick.AddListener(() => deuPasso = true);
        raiz.Find("Scanlines").SetAsLastSibling();
        var piscar = StartCoroutine(Piscar(bPasso.GetComponent<BotaoPixel>().texto));
        while (!deuPasso)
        {
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) deuPasso = true;
            yield return null;
        }
        StopCoroutine(piscar);
        Destroy(bPasso.gameObject);

        // Cris se endireita e anda alguns passos para a frente.
        StopCoroutine(animCris);
        yield return Andar(cris, 0f, 0.9f, -36f);   // um passo à frente do grupo
        animCris = StartCoroutine(Quadros(cris, pessoaParado, 3f));
        yield return Digitar(textoPasso);
        yield return new WaitForSecondsRealtime(1.6f);

        // Os outros chegam, alternando os lados.
        yield return Digitar(textoOutros);
        float[] destinos = { -150f, 150f, -300f, 300f, -450f, 450f };
        var chegaram = new List<Image>();
        for (int i = 0; i < destinos.Length; i++)
        {
            var p = NovaPessoa("Pessoa_" + i, coresPessoas[i % coresPessoas.Length], destinos[i] < 0 ? -1100f : 1100f);
            StartCoroutine(Andar(p, destinos[i], 1.6f + i * 0.12f, 0f));
            chegaram.Add(p);
            yield return new WaitForSecondsRealtime(0.35f);
        }
        cris.transform.SetAsLastSibling();          // Cris está um passo à frente: desenha por cima do grupo
        raiz.Find("Scanlines").SetAsLastSibling();
        yield return new WaitForSecondsRealtime(1.8f);
        StartCoroutine(Coracoes());

        // Todo mundo pula junto, em onda; o título aparece com glitch.
        StopCoroutine(animCris);
        StartCoroutine(Pulinho(cris, 0f));
        for (int i = 0; i < chegaram.Count; i++) StartCoroutine(Pulinho(chegaram[i], 0.08f * (i + 1)));
        var tTitulo = raiz.Find("Titulo").GetComponent<Text>();
        float n = 0f;
        while (n < titulo.Length)
        {
            n += Time.unscaledDeltaTime * 30f;
            tTitulo.text = PainelPixel.Revelar(titulo, Mathf.Min(titulo.Length, Mathf.FloorToInt(n)));
            yield return null;
        }
        tTitulo.text = titulo;
        raiz.Find("Titulo_Glitch_Vermelho").GetComponent<Text>().text = titulo;   // o glitch só começa com o título inteiro
        raiz.Find("Titulo_Glitch_Ciano").GetComponent<Text>().text = titulo;
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Digitar(textoFinal);
        yield return new WaitForSecondsRealtime(1.2f);

        // Conquista desbloqueada, créditos e botões.
        yield return Conquista();
        var tCred = tema.Texto(raiz, "Creditos", creditos, 26, tema.corApagada, TextAnchor.MiddleCenter, new Vector2(0f, -405f), new Vector2(1700f, 34f));
        var bDeNovo = tema.Botao(raiz, "BotaoJogarDeNovo", "Jogar de novo", new Vector2(-200f, -478f), new Vector2(340f, 56f), 30);
        var bSair = tema.Botao(raiz, "BotaoSair", "Sair", new Vector2(200f, -478f), new Vector2(340f, 56f), 30);
        bDeNovo.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        bSair.onClick.AddListener(Sair);
        raiz.Find("Scanlines").SetAsLastSibling();
        StartCoroutine(Aparecer(tCred));
    }

    // Faixa de "conquista desbloqueada", como nos videogames: entra de baixo em degraus e fica brilhando.
    IEnumerator Conquista()
    {
        var tema = TemaPixel.Instancia;
        var faixa = tema.Imagem(raiz, "Conquista", tema.painel, Color.white, new Vector2(0f, -20f), new Vector2(1000f, 76f), true);
        var t = tema.Texto(faixa.transform, "Texto", conquista, 16, tema.corAmbar, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(940f, 60f), true);
        raiz.Find("Scanlines").SetAsLastSibling();
        float[] ys = { -20f, 0f, 16f, 28f, 36f, 40f };
        foreach (float y in ys)
        {
            faixa.rectTransform.anchoredPosition = new Vector2(0f, y);
            yield return new WaitForSecondsRealtime(0.04f);
        }
        StartCoroutine(Piscar(t));
        yield return new WaitForSecondsRealtime(0.6f);
    }

    IEnumerator Piscar(Text t)
    {
        Color baseCor = t.color;
        while (t != null)
        {
            t.color = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.55f ? baseCor : new Color(1f, 1f, 1f, baseCor.a);
            yield return null;
        }
    }

    IEnumerator Digitar(string frase)
    {
        float n = 0f;
        while (n < frase.Length)
        {
            n += Time.unscaledDeltaTime * 45f;
            textoFrase.text = PainelPixel.Revelar(frase, Mathf.Min(frase.Length, Mathf.FloorToInt(n)));
            yield return null;
        }
        textoFrase.text = frase;
    }

    IEnumerator Quadros(Image img, Sprite[] quadros, float fps)
    {
        if (quadros == null || quadros.Length == 0) yield break;
        int i = 0;
        while (true)
        {
            img.sprite = quadros[i % quadros.Length];
            i++;
            yield return new WaitForSecondsRealtime(1f / fps);
        }
    }

    // Anda até "destinoX" com o ciclo de passos; ao chegar, respira parado.
    IEnumerator Andar(Image img, float destinoX, float duracao, float deslocY)
    {
        var rt = img.rectTransform;
        float inicioX = rt.anchoredPosition.x;
        float inicioY = rt.anchoredPosition.y, destinoY = inicioY + deslocY;
        rt.localScale = new Vector3(destinoX < inicioX ? -1f : 1f, 1f, 1f);
        float t = 0f, quadro = 0f;
        int i = 0;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            quadro += Time.unscaledDeltaTime;
            if (quadro > 0.12f) { quadro = 0f; i++; img.sprite = pessoaAndar[i % pessoaAndar.Length]; }
            float k = Mathf.Clamp01(t / duracao);
            float x = Mathf.Lerp(inicioX, destinoX, k), y = Mathf.Lerp(inicioY, destinoY, k);
            rt.anchoredPosition = new Vector2(Mathf.Round(x / ESCALA) * ESCALA, Mathf.Round(y / ESCALA) * ESCALA);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(destinoX, destinoY);
        rt.localScale = Vector3.one;
        if (img.name != "Cris") StartCoroutine(Quadros(img, pessoaParado, 2.5f + Random.Range(-0.3f, 0.3f)));
    }

    IEnumerator Pulinho(Image img, float atraso)
    {
        yield return new WaitForSecondsRealtime(atraso);
        var rt = img.rectTransform;
        float chao = rt.anchoredPosition.y;
        img.sprite = pessoaComemorar;
        float[] alturas = { 6f, 12f, 18f, 18f, 12f, 6f, 0f };
        for (int r = 0; r < 3; r++)
            foreach (float h in alturas)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, chao + h * 2f);
                yield return new WaitForSecondsRealtime(0.05f);
            }
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, chao);
        StartCoroutine(Quadros(img, pessoaParado, 3f));
    }

    // Corações pixelados sobem de perto das pessoas, sem parar.
    IEnumerator Coracoes()
    {
        var tema = TemaPixel.Instancia;
        while (raiz != null)
        {
            var p = pessoas[Random.Range(0, pessoas.Count)];
            Vector2 de = p.rectTransform.anchoredPosition + new Vector2(Random.Range(-30f, 30f), 190f);
            var h = tema.Imagem(raiz, "Coracao", coracao, Color.white, de, new Vector2(13f, 12f) * 4f, false);
            raiz.Find("Scanlines").SetAsLastSibling();
            StartCoroutine(Subir(h));
            yield return new WaitForSecondsRealtime(Random.Range(0.25f, 0.55f));
        }
    }

    IEnumerator Subir(Image h)
    {
        var rt = h.rectTransform;
        Vector2 de = rt.anchoredPosition;
        float t = 0f, dur = 2.2f, fase = Random.value * 6f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            float x = de.x + Mathf.Sin(fase + t * 3f) * 16f;
            float y = de.y + k * 220f;
            rt.anchoredPosition = new Vector2(Mathf.Round(x / 4f) * 4f, Mathf.Round(y / 4f) * 4f);
            var c = h.color; c.a = k < 0.7f ? 1f : Mathf.Round((1f - k) / 0.3f * 4f) / 4f; h.color = c;
            yield return null;
        }
        Destroy(h.gameObject);
    }

    IEnumerator Aparecer(Graphic g)
    {
        var c = g.color;
        for (int k = 0; k <= 4; k++)
        {
            c.a = k / 4f; g.color = c;
            yield return new WaitForSecondsRealtime(0.08f);
        }
    }

    void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
