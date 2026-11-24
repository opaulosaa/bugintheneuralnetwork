using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Entrada animada das caixas pixeladas (puzzles e telas do menu), em tempo real (funciona com o jogo pausado):
///  - a caixa "liga" como um monitor antigo: cresce em degraus de pixel na vertical e depois na horizontal;
///  - o título é digitado letra por letra (o resto fica transparente, então o texto não "anda" na tela);
///  - os itens da caixa (botões, post-its, chaves) entram um depois do outro, caindo alguns pixels.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PainelPixel : MonoBehaviour
{
    public Text titulo;
    public RectTransform[] itens;
    public float duracaoAbrir = 0.22f;
    public float letrasPorSegundo = 70f;
    public float intervaloItens = 0.06f;
    [Tooltip("Tamanho de um 'pixel' da UI, em unidades do Canvas (os sprites são ampliados 4x).")]
    public float pixel = 4f;

    CanvasGroup grupo;
    string tituloCompleto;
    string ultimoMostrado;
    Vector2[] posItens;
    CanvasGroup[] grupoItens;
    Coroutine rodando;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        if (titulo != null) tituloCompleto = titulo.text;
        FontesPixel.Nitidas();
    }

    RectTransform[] itensPreparados;

    // Guarda posição e CanvasGroup de cada item. Refeito se a lista mudar (os puzzles montam a UI no Start).
    void PrepararItens()
    {
        if (itens == itensPreparados && posItens != null) return;
        itensPreparados = itens;
        int n = itens != null ? itens.Length : 0;
        posItens = new Vector2[n];
        grupoItens = new CanvasGroup[n];
        for (int i = 0; i < n; i++)
        {
            if (itens[i] == null) continue;
            posItens[i] = itens[i].anchoredPosition;
            grupoItens[i] = itens[i].GetComponent<CanvasGroup>();
            if (grupoItens[i] == null) grupoItens[i] = itens[i].gameObject.AddComponent<CanvasGroup>();
        }
    }

    void OnEnable()
    {
        PrepararItens();
        if (rodando != null) StopCoroutine(rodando);
        rodando = StartCoroutine(Abrir());
    }

    void OnDisable()
    {
        transform.localScale = Vector3.one;
        if (grupo != null) grupo.alpha = 1f;
        if (titulo != null && tituloCompleto != null) titulo.text = tituloCompleto;
        if (itens != null && posItens != null && itens == itensPreparados)
            for (int i = 0; i < itens.Length; i++)
            {
                if (itens[i] == null) continue;
                itens[i].anchoredPosition = posItens[i];
                if (grupoItens[i] != null) grupoItens[i].alpha = 1f;
            }
    }

    /// <summary>Atualiza o texto do título (por exemplo, a mensagem final) e digita de novo.</summary>
    public void DefinirTitulo(string texto)
    {
        tituloCompleto = texto;
        if (isActiveAndEnabled && titulo != null) StartCoroutine(Digitar());
    }

    IEnumerator Abrir()
    {
        if (titulo != null && !string.IsNullOrEmpty(titulo.text) && titulo.text != tituloCompleto && titulo.text != ultimoMostrado)
            tituloCompleto = titulo.text;   // o texto pode ter sido trocado por script antes de abrir (ex.: mensagem final)
        if (titulo != null && tituloCompleto != null) Mostrar(0);
        if (itens != null)
            for (int i = 0; i < itens.Length; i++)
                if (grupoItens[i] != null) grupoItens[i].alpha = 0f;

        // Liga a "tela": primeiro uma linha fina, depois abre na vertical, em degraus.
        float t = 0f;
        while (t < duracaoAbrir)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoAbrir);
            float kx = Mathf.Clamp01(k * 2f), ky = Mathf.Clamp01(k * 2f - 1f);
            kx = Mathf.Round(kx * 6f) / 6f; ky = Mathf.Round(ky * 6f) / 6f;   // degraus
            transform.localScale = new Vector3(Mathf.Lerp(0.6f, 1f, kx), Mathf.Lerp(0.04f, 1f, ky), 1f);
            grupo.alpha = Mathf.Lerp(0.4f, 1f, k);
            yield return null;
        }
        transform.localScale = Vector3.one;
        grupo.alpha = 1f;

        if (itens != null)
            for (int i = 0; i < itens.Length; i++)
                if (itens[i] != null) StartCoroutine(EntrarItem(i, i * intervaloItens));

        yield return Digitar();
        rodando = null;
    }

    IEnumerator Digitar()
    {
        if (titulo == null || string.IsNullOrEmpty(tituloCompleto)) yield break;
        float n = 0f;
        while (n < tituloCompleto.Length)
        {
            n += Time.unscaledDeltaTime * letrasPorSegundo;
            Mostrar(Mathf.Min(tituloCompleto.Length, Mathf.FloorToInt(n)));
            yield return null;
        }
        titulo.text = tituloCompleto;
        ultimoMostrado = null;
    }

    // As letras ainda não digitadas continuam no texto, só que transparentes: a quebra de linha e o
    // tamanho (Best Fit) já são os finais desde a primeira letra, e nada se desloca enquanto digita.
    void Mostrar(int k)
    {
        ultimoMostrado = Revelar(tituloCompleto, k);
        titulo.text = ultimoMostrado;
    }

    public static string Revelar(string texto, int k)
    {
        if (string.IsNullOrEmpty(texto) || k >= texto.Length) return texto;
        return texto.Substring(0, k) + "<color=#00000000>" + texto.Substring(k) + "</color>";
    }

    IEnumerator EntrarItem(int i, float atraso)
    {
        float t = -atraso;
        const float dur = 0.18f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float queda = Mathf.Round((1f - k) * 3f) * pixel;   // cai 3 "pixels" em degraus
            itens[i].anchoredPosition = posItens[i] + Vector2.up * queda;
            grupoItens[i].alpha = k <= 0f ? 0f : Mathf.Round(k * 4f) / 4f;
            yield return null;
        }
        itens[i].anchoredPosition = posItens[i];
        grupoItens[i].alpha = 1f;
    }
}

/// <summary>Deixa as fontes pixel nítidas (sem suavização) quando o atlas da fonte é refeito.</summary>
public static class FontesPixel
{
    static bool registrado;

    public static void Nitidas()
    {
        Aplicar();
        if (registrado) return;
        registrado = true;
        Font.textureRebuilt += _ => Aplicar();
    }

    static void Aplicar()
    {
        foreach (var f in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (f == null || f.material == null || f.material.mainTexture == null) continue;
            if (f.name.Contains("VT323") || f.name.Contains("PressStart2P"))
                f.material.mainTexture.filterMode = FilterMode.Point;
        }
    }
}
