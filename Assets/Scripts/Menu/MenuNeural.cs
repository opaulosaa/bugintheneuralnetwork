using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima o menu inicial (tudo em tempo real, porque o jogo fica pausado com timeScale = 0):
///  - uma rede neural ao fundo: neurônios à deriva, sinapses entre os próximos, pulsos de sinal
///    correndo pelas sinapses e alguns neurônios "com bug" piscando em vermelho;
///  - entrada em sequência dos elementos (fade + leve subida);
///  - glitch ocasional no título;
///  - relógio (fim de tarde, 16:40) com os dois-pontos piscando e um brilho que respira atrás do título.
/// Montado por Bug In The Neural Network > Estilizar Menu Inicial.
/// </summary>
public class MenuNeural : MonoBehaviour
{
    [Tooltip("Hora mostrada no relógio do menu (a história começa no fim da tarde).")]
    public string horaMenu = "16:40";

    [Header("Rede neural")]
    public RectTransform areaRede;
    public Sprite spriteNeuronio;
    public int quantidade = 46;
    public float distanciaSinapse = 230f;
    public Color corNeuronio = new Color(0.30f, 0.85f, 0.95f, 0.85f);
    public Color corSinapse = new Color(0.30f, 0.85f, 0.95f, 0.22f);
    public Color corPulso = new Color(1f, 0.72f, 0.35f, 1f);
    public Color corBug = new Color(1f, 0.25f, 0.30f, 1f);
    [Range(0f, 1f)] public float fracaoBugs = 0.1f;

    [Header("Entrada em sequência")]
    public CanvasGroup[] entrada;
    public float atrasoInicial = 0.25f;
    public float intervaloEntrada = 0.18f;
    public float duracaoEntrada = 0.7f;

    [Header("Título")]
    public Text titulo;
    public Text glitchVermelho;
    public Text glitchCiano;
    public RectTransform brilhoTitulo;

    [Header("Relógio")]
    public Text relogio;

    class Neuronio { public RectTransform rt; public Image img; public Vector2 pos, vel; public float semente, tamanho; public bool bug; }
    class Pulso { public RectTransform rt; public int a, b; public float t, vel; }

    readonly List<Neuronio> neuronios = new List<Neuronio>();
    readonly List<Image> linhas = new List<Image>();
    readonly List<Pulso> pulsos = new List<Pulso>();
    RectTransform camadaLinhas, camadaPulsos;
    Vector2[] posBase;
    float tempo, proximoGlitch, fimGlitch, proximoPulso;
    Vector2 tituloBase;

    void OnEnable()
    {
        tempo = 0f;
        if (entrada != null)
        {
            posBase = new Vector2[entrada.Length];
            for (int i = 0; i < entrada.Length; i++)
            {
                if (entrada[i] == null) continue;
                entrada[i].alpha = 0f;
                posBase[i] = ((RectTransform)entrada[i].transform).anchoredPosition;
            }
        }
        if (titulo != null) tituloBase = titulo.rectTransform.anchoredPosition;
        proximoGlitch = 2.5f;
    }

    void CriarRede()
    {
        camadaLinhas = NovaCamada("Sinapses");
        camadaPulsos = NovaCamada("Pulsos");
        Rect r = areaRede.rect;
        var rnd = new System.Random(3);
        for (int i = 0; i < quantidade; i++)
        {
            var n = new Neuronio();
            n.bug = rnd.NextDouble() < fracaoBugs;
            n.tamanho = n.bug ? 16f : 8f + (float)rnd.NextDouble() * 10f;
            n.pos = new Vector2(Mathf.Lerp(r.xMin, r.xMax, (float)rnd.NextDouble()), Mathf.Lerp(r.yMin, r.yMax, (float)rnd.NextDouble()));
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            n.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (8f + (float)rnd.NextDouble() * 14f);
            n.semente = (float)rnd.NextDouble() * 100f;
            var g = new GameObject(n.bug ? "Neuronio_Bug" : "Neuronio", typeof(RectTransform), typeof(Image));
            n.rt = (RectTransform)g.transform;
            n.rt.SetParent(areaRede, false);
            n.rt.sizeDelta = Vector2.one * n.tamanho;
            n.img = g.GetComponent<Image>();
            n.img.sprite = spriteNeuronio;
            n.img.raycastTarget = false;
            n.img.color = n.bug ? corBug : corNeuronio;
            neuronios.Add(n);
        }
    }

    RectTransform NovaCamada(string nome)
    {
        var g = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)g.transform;
        rt.SetParent(areaRede, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        tempo += dt;
        // A rede nasce no primeiro quadro em que o Canvas já tem tamanho (no OnEnable ainda pode ser 0).
        if (neuronios.Count == 0 && areaRede != null && areaRede.rect.width > 10f) CriarRede();
        AnimarEntrada();
        AnimarRede(dt);
        AnimarTitulo();
        if (relogio != null)
            relogio.text = (Mathf.Repeat(tempo, 1f) < 0.5f) ? horaMenu : horaMenu.Replace(":", " ");
        if (brilhoTitulo != null)
        {
            float k = 1f + Mathf.Sin(tempo * 1.3f) * 0.06f;
            brilhoTitulo.localScale = new Vector3(k, k, 1f);
        }
    }

    void AnimarEntrada()
    {
        if (entrada == null) return;
        for (int i = 0; i < entrada.Length; i++)
        {
            if (entrada[i] == null) continue;
            float t = Mathf.Clamp01((tempo - atrasoInicial - i * intervaloEntrada) / duracaoEntrada);
            float e = 1f - Mathf.Pow(1f - t, 3f);   // desacelera no fim
            entrada[i].alpha = e;
            ((RectTransform)entrada[i].transform).anchoredPosition = posBase[i] + Vector2.down * (1f - e) * 24f;
        }
    }

    void AnimarRede(float dt)
    {
        if (areaRede == null) return;
        Rect r = areaRede.rect;
        foreach (var n in neuronios)
        {
            // Deriva lenta com um leve desvio de ruído, dando a volta nas bordas.
            float desvio = (Mathf.PerlinNoise(n.semente, tempo * 0.2f) - 0.5f) * 40f;
            n.pos += (n.vel + new Vector2(desvio, -desvio)) * dt;
            if (n.pos.x < r.xMin - 40f) n.pos.x = r.xMax + 40f;
            if (n.pos.x > r.xMax + 40f) n.pos.x = r.xMin - 40f;
            if (n.pos.y < r.yMin - 40f) n.pos.y = r.yMax + 40f;
            if (n.pos.y > r.yMax + 40f) n.pos.y = r.yMin - 40f;
            n.rt.anchoredPosition = n.pos;

            Color c = n.bug ? corBug : corNeuronio;
            float brilho = n.bug
                ? (Mathf.PerlinNoise(n.semente, tempo * 6f) > 0.55f ? 1f : 0.25f)   // pisca irregular
                : 0.65f + 0.35f * Mathf.Sin(tempo * 2f + n.semente);
            c.a *= brilho;
            n.img.color = c;
        }

        // Sinapses entre neurônios próximos (reaproveita as linhas de um quadro para o outro).
        int usadas = 0;
        float d2max = distanciaSinapse * distanciaSinapse;
        for (int i = 0; i < neuronios.Count; i++)
            for (int j = i + 1; j < neuronios.Count; j++)
            {
                Vector2 d = neuronios[j].pos - neuronios[i].pos;
                float d2 = d.sqrMagnitude;
                if (d2 > d2max) continue;
                Image l = Linha(usadas++);
                RectTransform lr = l.rectTransform;
                float dist = Mathf.Sqrt(d2);
                lr.anchoredPosition = neuronios[i].pos + d * 0.5f;
                lr.sizeDelta = new Vector2(dist, 2f);
                lr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                bool comBug = neuronios[i].bug || neuronios[j].bug;
                Color c = comBug ? new Color(corBug.r, corBug.g, corBug.b, corSinapse.a) : corSinapse;
                c.a *= 1f - dist / distanciaSinapse;
                l.color = c;
                l.enabled = true;

                // De vez em quando um sinal sai correndo por uma sinapse.
                if (tempo > proximoPulso && !comBug && pulsos.Count < 14)
                {
                    proximoPulso = tempo + Random.Range(0.08f, 0.3f);
                    NovoPulso(i, j);
                }
            }
        for (int k = usadas; k < linhas.Count; k++) linhas[k].enabled = false;

        for (int k = pulsos.Count - 1; k >= 0; k--)
        {
            Pulso p = pulsos[k];
            p.t += dt * p.vel;
            if (p.t >= 1f) { Destroy(p.rt.gameObject); pulsos.RemoveAt(k); continue; }
            p.rt.anchoredPosition = Vector2.Lerp(neuronios[p.a].pos, neuronios[p.b].pos, p.t);
            float a = Mathf.Sin(p.t * Mathf.PI);
            p.rt.GetComponent<Image>().color = new Color(corPulso.r, corPulso.g, corPulso.b, a);
        }
    }

    Image Linha(int i)
    {
        while (linhas.Count <= i)
        {
            var g = new GameObject("Sinapse", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(camadaLinhas, false);
            var img = g.GetComponent<Image>();
            img.raycastTarget = false;
            linhas.Add(img);
        }
        return linhas[i];
    }

    void NovoPulso(int a, int b)
    {
        if (Random.value < 0.5f) { int t = a; a = b; b = t; }
        var g = new GameObject("Pulso", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)g.transform;
        rt.SetParent(camadaPulsos, false);
        rt.sizeDelta = Vector2.one * 9f;
        var img = g.GetComponent<Image>();
        img.sprite = spriteNeuronio;
        img.raycastTarget = false;
        pulsos.Add(new Pulso { rt = rt, a = a, b = b, t = 0f, vel = Random.Range(0.6f, 1.2f) });
    }

    void AnimarTitulo()
    {
        if (titulo == null) return;
        if (tempo > proximoGlitch)
        {
            fimGlitch = tempo + Random.Range(0.12f, 0.28f);
            proximoGlitch = tempo + Random.Range(2.5f, 5.5f);
        }
        bool emGlitch = tempo < fimGlitch;
        Vector2 tremor = emGlitch ? new Vector2(Random.Range(-6f, 6f), Random.Range(-2f, 2f)) : Vector2.zero;
        titulo.rectTransform.anchoredPosition = tituloBase + tremor * 0.4f;
        if (glitchVermelho != null)
        {
            glitchVermelho.rectTransform.anchoredPosition = tituloBase + (emGlitch ? new Vector2(-7f, 2f) + tremor : Vector2.zero);
            glitchVermelho.color = new Color(1f, 0.2f, 0.3f, emGlitch ? 0.75f : 0f);
        }
        if (glitchCiano != null)
        {
            glitchCiano.rectTransform.anchoredPosition = tituloBase + (emGlitch ? new Vector2(7f, -2f) - tremor : Vector2.zero);
            glitchCiano.color = new Color(0.2f, 0.9f, 1f, emGlitch ? 0.75f : 0f);
        }
    }
}
