using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A cada puzzle resolvido, uma parte do quarto se arruma sozinha, mostrando que Cris está
/// no caminho certo:
///  - Puzzle 1 (fila de prioridade): a escrivaninha e o chão em volta dela.
///  - Puzzle 2 (loop de pensamento): o caminho no meio do quarto.
///  - Puzzle 3 (firewall social): a cama. O "ninho" do edredom se desfaz e a cama fica feita.
/// Roupas voam para o cesto e o resto para o saco de lixo mais próximo, um item de cada vez.
/// O quarto também recupera um pouco da cor (pós-processamento) a cada parte arrumada.
/// Os grupos são preenchidos por Bug In The Neural Network > Versão Cris > 4. Arrumação e batimentos,
/// mas podem ser ajustados à mão no Inspector.
/// </summary>
public class ArrumacaoQuarto : MonoBehaviour
{
    [System.Serializable]
    public class Grupo
    {
        public string nome;
        public List<Transform> itens = new List<Transform>();
    }

    [Header("O que cada puzzle arruma (índice 0 = puzzle 1)")]
    public Grupo[] grupos = { new Grupo { nome = "Escrivaninha" }, new Grupo { nome = "Meio do quarto" }, new Grupo { nome = "Cama" } };

    [Header("Para onde as coisas vão")]
    public Transform cesto;
    public List<Transform> sacosLixo = new List<Transform>();
    public string[] palavrasRoupa = { "Roupa", "Camiseta", "Calca", "Meia", "Toalha", "Moletom", "Monte", "Tenis", "Chinelo" };
    [Tooltip("Livros voltam para a estante.")]
    public Transform estante;
    public string[] palavrasEstante = { "book", "Book", "Livro", "livro" };

    [Header("Cama feita (puzzle 3)")]
    public List<Transform> pecasNinho = new List<Transform>();
    public Transform lencol;
    public Material materialEdredom;

    [Header("Ritmo")]
    [Tooltip("Espera o painel do puzzle fechar antes de começar.")]
    public float atrasoInicial = 1.9f;
    public float duracaoVoo = 0.75f;
    public float intervaloEntreItens = 0.06f;

    [Header("Cor do quarto voltando")]
    public Volume volume;
    [Tooltip("Saturação (Color Adjustments) depois dos 3 puzzles.")]
    public float saturacaoFinal = 0f;
    [Tooltip("Intensidade da vinheta depois dos 3 puzzles.")]
    public float vinhetaFinal = 0.12f;

    private AudioSource fonte;
    private AudioClip somArrumar;
    private ColorAdjustments cores;
    private Vignette vinheta;
    private float saturacaoInicial, vinhetaInicial;
    private int partesArrumadas;
    private readonly HashSet<Transform> pulando = new HashSet<Transform>();

    void Start()
    {
        if (StoryState.Instance != null) StoryState.Instance.OnPuzzleConcluido += Arrumar;

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        somArrumar = CriarSomArrumar();

        if (volume == null) volume = FindFirstObjectByType<Volume>();
        if (volume != null)
        {
            VolumeProfile perfil = volume.profile;   // cópia só desta partida (não mexe no asset)
            if (perfil.TryGet(out cores)) { cores.saturation.overrideState = true; saturacaoInicial = cores.saturation.value; }
            if (perfil.TryGet(out vinheta)) { vinheta.intensity.overrideState = true; vinhetaInicial = vinheta.intensity.value; }
        }
    }

    void OnDestroy()
    {
        if (StoryState.Instance != null) StoryState.Instance.OnPuzzleConcluido -= Arrumar;
    }

    /// <summary>Arruma a parte do quarto do puzzle (1, 2 ou 3).</summary>
    public void Arrumar(int puzzle)
    {
        int i = puzzle - 1;
        if (grupos == null || i < 0 || i >= grupos.Length) return;
        StartCoroutine(Sequencia(i));
    }

    IEnumerator Sequencia(int indice)
    {
        yield return new WaitForSeconds(atrasoInicial);

        var jogador = GameManager.Instance != null ? GameManager.Instance.faiscaPlayer : null;
        if (jogador != null)
        {
            var anim = jogador.GetComponent<CrisMiniatura>();
            if (anim != null) anim.Comemorar();
        }
        if (somArrumar != null) fonte.PlayOneShot(somArrumar, 0.6f);

        partesArrumadas++;
        StartCoroutine(VoltarCor(partesArrumadas / (float)Mathf.Max(1, grupos.Length)));

        // Do mais perto do jogador para o mais longe: parece uma onda saindo de Cris.
        var itens = new List<Transform>();
        foreach (var t in grupos[indice].itens) if (t != null && t.gameObject.activeInHierarchy) itens.Add(t);
        Vector3 origem = jogador != null ? jogador.transform.position : transform.position;
        itens.Sort((a, b) => Distancia(a.position, origem).CompareTo(Distancia(b.position, origem)));

        if (indice == 2) StartCoroutine(FazerCama());

        foreach (var t in itens)
        {
            StartCoroutine(Voar(t, Destino(t)));
            yield return new WaitForSeconds(intervaloEntreItens);
        }
    }

    static float Distancia(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

    Transform Destino(Transform item)
    {
        foreach (string p in palavrasRoupa)
            if (item.name.Contains(p)) return cesto;
        if (estante != null)
            foreach (string p in palavrasEstante)
                if (item.name.Contains(p)) return estante;
        Transform melhor = null;
        float menor = float.MaxValue;
        foreach (var s in sacosLixo)
        {
            if (s == null || !s.gameObject.activeInHierarchy) continue;
            float d = Distancia(s.position, item.position);
            if (d < menor) { menor = d; melhor = s; }
        }
        return melhor;
    }

    IEnumerator Voar(Transform item, Transform destino)
    {
        foreach (var c in item.GetComponentsInChildren<Collider>()) c.enabled = false;

        Vector3 inicio = item.position;
        Vector3 escala = item.localScale;
        Quaternion rot = item.rotation;
        Vector3 fim = destino != null ? destino.position + Vector3.up * 0.35f : inicio + Vector3.up * 0.6f;
        float altura = 0.5f + Distancia(inicio, fim) * 0.18f;
        Vector3 giro = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(200f, 420f);

        float t = 0f;
        while (t < duracaoVoo)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracaoVoo);
            float s = Mathf.SmoothStep(0f, 1f, k);
            item.position = Vector3.Lerp(inicio, fim, s) + Vector3.up * (altura * 4f * s * (1f - s));
            item.rotation = Quaternion.Euler(giro * k * duracaoVoo) * rot;
            item.localScale = escala * Mathf.Lerp(1f, 0.1f, k * k);
            yield return null;
        }
        item.gameObject.SetActive(false);
        if (destino != null && !pulando.Contains(destino)) StartCoroutine(Pular(destino));
    }

    // O cesto/saco "engole" o item com um pulinho de escala.
    IEnumerator Pular(Transform alvo)
    {
        pulando.Add(alvo);
        Vector3 e = alvo.localScale;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            alvo.localScale = e * (1f + 0.12f * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI));
            yield return null;
        }
        alvo.localScale = e;
        pulando.Remove(alvo);
    }

    // O casulo do edredom murcha e um edredom esticado aparece por cima do lençol.
    IEnumerator FazerCama()
    {
        var pecas = new List<Transform>();
        var escalas = new List<Vector3>();
        foreach (var p in pecasNinho)
            if (p != null && p.gameObject.activeInHierarchy) { pecas.Add(p); escalas.Add(p.localScale); }

        Transform edredom = null;
        Vector3 escalaEdredom = Vector3.one;
        if (lencol != null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Edredom_Arrumado";
            Destroy(g.GetComponent<Collider>());
            edredom = g.transform;
            edredom.SetParent(lencol.parent, false);
            edredom.localRotation = lencol.localRotation;
            Vector3 l = lencol.localScale;
            escalaEdredom = new Vector3(l.x * 1.02f, 0.07f, l.z * 0.82f);
            edredom.localPosition = lencol.localPosition + lencol.localRotation * new Vector3(0f, 0.04f, -l.z * 0.09f);
            edredom.localScale = new Vector3(escalaEdredom.x, 0.0001f, escalaEdredom.z);
            if (materialEdredom != null) g.GetComponent<Renderer>().sharedMaterial = materialEdredom;
        }

        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            for (int i = 0; i < pecas.Count; i++) pecas[i].localScale = escalas[i] * (1f - k);
            if (edredom != null) edredom.localScale = new Vector3(escalaEdredom.x, Mathf.Max(0.0001f, escalaEdredom.y * k), escalaEdredom.z);
            yield return null;
        }
        foreach (var p in pecas) p.gameObject.SetActive(false);
    }

    IEnumerator VoltarCor(float fracao)
    {
        if (cores == null && vinheta == null) yield break;
        float satDe = cores != null ? cores.saturation.value : 0f;
        float vinDe = vinheta != null ? vinheta.intensity.value : 0f;
        float satPara = Mathf.Lerp(saturacaoInicial, saturacaoFinal, fracao);
        float vinPara = Mathf.Lerp(vinhetaInicial, vinhetaFinal, fracao);
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.5f));
            if (cores != null) cores.saturation.value = Mathf.Lerp(satDe, satPara, k);
            if (vinheta != null) vinheta.intensity.value = Mathf.Lerp(vinDe, vinPara, k);
            yield return null;
        }
    }

    /// <summary>Arpejo curto e suave (dó, mi, sol), sinal de que algo deu certo.</summary>
    static AudioClip CriarSomArrumar()
    {
        const int taxa = 44100;
        float[] notas = { 523.25f, 659.25f, 783.99f };
        int n = (int)(taxa * 1.1f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)taxa, s = 0f;
            for (int j = 0; j < notas.Length; j++)
            {
                float x = t - j * 0.11f;
                if (x < 0f) continue;
                float env = (1f - Mathf.Exp(-x * 200f)) * Mathf.Exp(-x * 4.5f);
                s += env * (Mathf.Sin(2f * Mathf.PI * notas[j] * x) + 0.25f * Mathf.Sin(4f * Mathf.PI * notas[j] * x));
            }
            d[i] = s * 0.3f;
        }
        AudioClip c = AudioClip.Create("Arrumar", n, 1, taxa, false);
        c.SetData(d, 0);
        return c;
    }
}
