using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a Vontade de Cris e a "pressão" por ficar parado.
///
/// Versão sem apagão (para não causar gatilhos): nada escurece e o jogador nunca perde o controle.
///  - Ficar parado mais de 'segundosParaInercia' faz tocar um batimento cardíaco que acelera
///    quanto mais o jogador demora (de bpmCalmo até bpmMaximo). Ao voltar a andar, o coração acalma.
///  - Erros em puzzle dão um susto curto: alguns batimentos mais rápidos.
///  - A Vontade continua: cai com erros e com a inércia, sobe com acertos e um pouco ao andar.
///    Com pouca Vontade o passo de Cris fica mais pesado (mais lento e curvado), mas nunca trava.
///  - Se demorar muito (ou a Vontade zerar), aparece uma dica gentil apontando o próximo passo.
///  - Enquanto um painel de puzzle está aberto o relógio da inércia para e o boneco não anda.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Vontade")]
    [Range(0, 100)] public float vontade = 50f;
    public float vontadeMax = 100f;
    public float drenaPorErro = 10f;
    public float drenaPorInerciaPorSegundo = 1.5f;
    public float segundosParaInercia = 8f;
    public float recuperaPorProgresso = 10f;
    public float recuperaAndandoPorSegundo = 0.4f;

    [Header("Vontade baixa (passo pesado, sem apagão)")]
    [Tooltip("Fração da velocidade quando a Vontade está em 0.")]
    [Range(0.2f, 1f)] public float velocidadeSemVontade = 0.6f;
    [Tooltip("Acima deste valor de Vontade o passo é normal.")]
    public float vontadeParaPassoNormal = 40f;

    [Header("Batimentos (quanto mais demora, mais acelera)")]
    [Tooltip("Opcional. Se ficar vazio, um batimento é gerado por código.")]
    public AudioClip somBatimento;
    public float bpmCalmo = 66f;
    public float bpmMaximo = 150f;
    [Tooltip("Segundos parado, depois de começar a inércia, até chegar no bpm máximo.")]
    public float segundosAteMaximo = 25f;
    [Range(0f, 1f)] public float volumeMinimo = 0.25f;
    [Range(0f, 1f)] public float volumeMaximo = 0.9f;
    [Tooltip("Batimentos por minuto somados por um erro (decai em poucos segundos).")]
    public float sustoPorErro = 40f;
    [Tooltip("Quanto a luz em volta do boneco pulsa a cada batida.")]
    public float pulsoNaAura = 0.18f;

    [Header("Dica gentil")]
    public Text textoDica;
    public float segundosParaDica = 16f;

    [Header("Referências visuais")]
    public Light faiscaLuz;              // luz que acompanha o boneco
    public float intensidadeMin = 0.3f;
    public float intensidadeMax = 3f;
    public Light luzAmbiente;            // luz principal do quarto
    public Color corFria = new Color(0.4f, 0.55f, 0.75f);
    public Color corQuente = new Color(1f, 0.85f, 0.6f);
    [Tooltip("Halo no chão em volta do boneco (pulsa com o coração). Achado sozinho se ficar vazio.")]
    public Transform aura;

    [Header("Jogador")]
    public GameObject faiscaPlayer;      // objeto do player (o nome na cena ainda é "Faisca")

    [HideInInspector] public CanvasGroup telaApagao;   // legado: o apagão foi removido
    /// <summary>Ligado pela cortina (PuzzleFinal): no final o coração acalma e não há mais pressão.</summary>
    [HideInInspector] public bool jogoTerminado;

    /// <summary>0 a 1: o quanto o coração está acelerado agora.</summary>
    public float Tensao { get; private set; }
    /// <summary>Batimentos por minuto atuais (0 quando o coração não está audível).</summary>
    public float BpmAtual => volumeAtual > 0.01f ? bpmAtual : 0f;

    private float tempoParado;
    private Vector3 ultimaPosicao;
    private PlayerMovement movimento;
    private Interactable[] interagiveis;
    private AudioSource fonte;
    private float bpmAtual;
    private float volumeAtual;
    private float susto;
    private float proximaBatida;
    private float pulso;
    private Vector3 auraBase;
    private float alphaDica;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (faiscaPlayer != null)
        {
            ultimaPosicao = faiscaPlayer.transform.position;
            movimento = faiscaPlayer.GetComponent<PlayerMovement>();
            if (aura == null)
                foreach (Transform t in faiscaPlayer.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Aura") { aura = t; break; }
        }
        if (aura != null) auraBase = aura.localScale;
        interagiveis = FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        if (telaApagao != null) telaApagao.gameObject.SetActive(false);   // nada de tela preta

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        if (somBatimento == null) somBatimento = CriarBatimento();
        bpmAtual = bpmCalmo;

        if (textoDica != null) { textoDica.text = ""; DefinirAlphaDica(0f); }
        AtualizarVisual();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // menu aberto (timeScale = 0)

        bool painelAberto = AlgumPainelAberto();
        bool podeJogar = movimento == null || movimento.podeMover;

        if (movimento != null)
        {
            movimento.bloqueadoPorPainel = painelAberto;
            float k = Mathf.Clamp01(vontade / Mathf.Max(1f, vontadeParaPassoNormal));
            movimento.multiplicadorVelocidade = Mathf.Lerp(velocidadeSemVontade, 1f, k);
        }

        // ---------- Inércia ----------
        bool andando = false;
        if (faiscaPlayer != null)
        {
            Vector3 p = faiscaPlayer.transform.position;
            if (Vector3.Distance(p, ultimaPosicao) >= 0.01f) { andando = true; ultimaPosicao = p; }
        }

        if (jogoTerminado)
        {
            tempoParado = 0f;
            susto = 0f;
        }
        else if (andando)
        {
            tempoParado = 0f;
            RecuperarVontade(recuperaAndandoPorSegundo * dt);
        }
        else if (podeJogar && !painelAberto)
        {
            tempoParado += dt;
            if (tempoParado >= segundosParaInercia)
                DrenarVontade(drenaPorInerciaPorSegundo * dt);
        }

        AtualizarBatimentos(dt);
        AtualizarDica(dt, painelAberto);
    }

    // ---------- Coração ----------

    void AtualizarBatimentos(float dt)
    {
        float alem = tempoParado - segundosParaInercia;
        float alvoTensao = alem > 0f ? Mathf.Clamp01(alem / Mathf.Max(1f, segundosAteMaximo)) : 0f;
        Tensao = Mathf.MoveTowards(Tensao, alvoTensao, dt * (alvoTensao > Tensao ? 1f : 0.5f));

        susto = Mathf.MoveTowards(susto, 0f, dt * sustoPorErro / 3f);   // o susto some em ~3 s
        bool parado = alem > 0f;
        bool audivel = parado || susto > 1f || Tensao > 0.02f;

        float alvoBpm = Mathf.Lerp(bpmCalmo, bpmMaximo, Tensao) + susto;
        bpmAtual = Mathf.MoveTowards(bpmAtual, alvoBpm, dt * 25f);   // acelera e acalma aos poucos

        float forca = Mathf.Max(Tensao, susto / Mathf.Max(1f, sustoPorErro) * 0.6f);
        float alvoVolume = audivel ? Mathf.Lerp(volumeMinimo, volumeMaximo, forca) : 0f;
        volumeAtual = Mathf.MoveTowards(volumeAtual, alvoVolume, dt * 0.7f);

        if (volumeAtual > 0.01f && somBatimento != null)
        {
            proximaBatida -= dt;
            if (proximaBatida <= 0f)
            {
                fonte.pitch = Mathf.Lerp(1f, 1.12f, Tensao);
                fonte.PlayOneShot(somBatimento, volumeAtual);
                pulso = 1f;
                proximaBatida = 60f / Mathf.Max(30f, bpmAtual);
            }
        }
        else proximaBatida = 0.15f;

        pulso = Mathf.MoveTowards(pulso, 0f, dt * 5f);
        if (aura != null)
            aura.localScale = auraBase * (1f + pulsoNaAura * pulso * Mathf.Clamp01(volumeAtual * 1.5f));
    }

    /// <summary>Batimento "tum-tum" sintetizado: duas batidas graves com um leve estalo.</summary>
    static AudioClip CriarBatimento()
    {
        const int taxa = 44100;
        const float duracao = 0.55f;
        int n = (int)(taxa * duracao);
        float[] dados = new float[n];
        float maior = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)taxa;
            float s = Batida(t, 0f, 58f, 1f) + Batida(t, 0.23f, 70f, 0.7f);
            dados[i] = s;
            maior = Mathf.Max(maior, Mathf.Abs(s));
        }
        if (maior > 0f) for (int i = 0; i < n; i++) dados[i] *= 0.95f / maior;
        AudioClip clip = AudioClip.Create("Batimento", n, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    static float Batida(float t, float inicio, float freq, float amp)
    {
        float x = t - inicio;
        if (x < 0f) return 0f;
        float envelope = (1f - Mathf.Exp(-x * 400f)) * Mathf.Exp(-x * 20f);
        float f = freq * (1f + 0.5f * Mathf.Exp(-x * 35f));   // a "pancada" começa um pouco mais aguda
        float corpo = Mathf.Sin(2f * Mathf.PI * f * x) + 0.45f * Mathf.Sin(2f * Mathf.PI * f * 2.1f * x);
        return amp * envelope * corpo;
    }

    // ---------- Dica gentil ----------

    void AtualizarDica(float dt, bool painelAberto)
    {
        if (textoDica == null) return;
        bool mostrar = !painelAberto && !jogoTerminado && (tempoParado >= segundosParaDica || vontade <= 0.5f);
        if (mostrar && alphaDica < 0.01f) textoDica.text = TextoDaDica();
        alphaDica = Mathf.MoveTowards(alphaDica, mostrar ? 1f : 0f, dt * (mostrar ? 0.8f : 2f));
        DefinirAlphaDica(alphaDica);
    }

    string TextoDaDica()
    {
        var s = StoryState.Instance;
        string alvo;
        if (s == null || !s.puzzle1Concluido) alvo = "a escrivaninha";
        else if (!s.puzzle2Concluido) alvo = "o mural de cortiça";
        else if (!s.puzzle3Concluido) alvo = "o celular na cama";
        else alvo = "a cortina";
        return "Respire. Um passo de cada vez. Que tal " + alvo + "?";
    }

    void DefinirAlphaDica(float a)
    {
        Color c = textoDica.color; c.a = a; textoDica.color = c;
        textoDica.enabled = a > 0.001f;
    }

    // ---------- Vontade ----------

    bool AlgumPainelAberto()
    {
        if (interagiveis == null) return false;
        foreach (var i in interagiveis) if (i != null && i.PainelAberto) return true;
        return false;
    }

    public void DrenarVontade(float quantidade)
    {
        vontade = Mathf.Clamp(vontade - quantidade, 0f, vontadeMax);
        AtualizarVisual();
    }

    public void RecuperarVontade(float quantidade)
    {
        vontade = Mathf.Clamp(vontade + quantidade, 0f, vontadeMax);
        AtualizarVisual();
    }

    public void ErroNoPuzzle()
    {
        DrenarVontade(drenaPorErro);
        susto = sustoPorErro;
        if (bpmAtual < bpmCalmo + 10f) bpmAtual = bpmCalmo + 10f;
    }

    public void ProgressoNoPuzzle()
    {
        RecuperarVontade(recuperaPorProgresso);
        tempoParado = 0f;
    }

    void AtualizarVisual()
    {
        float t = vontade / vontadeMax;
        if (faiscaLuz != null) faiscaLuz.intensity = Mathf.Lerp(intensidadeMin, intensidadeMax, t);
        if (luzAmbiente != null) luzAmbiente.color = Color.Lerp(corFria, corQuente, t);
    }
}
