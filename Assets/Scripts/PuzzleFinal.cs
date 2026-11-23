using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle final: a cortina. Só fica interagível quando os três puzzles
/// anteriores foram concluídos (StoryState.TodosConcluidos). Ao interagir,
/// toca a sequência de final do jogo.
/// </summary>
public class PuzzleFinal : Interactable
{
    public StoryState storyState;
    public GameObject painelBloqueado;
    public GameObject painelFinal;
    public Text textoMensagemFinal;
    public Light luzAmbiente;
    public Color corFinal = new Color(1f, 0.85f, 0.6f);

    [Header("Janela e cortinas (opcional, montadas por 'Modelar Janela, Cortinas e Quadro')")]
    public Transform cortinaEsquerda;
    public Transform cortinaDireita;
    public Renderer[] ceu;                 // alto, baixo, lua
    public Color[] coresCeuFinal;          // mesma ordem de "ceu"
    public Renderer[] estrelas;
    public Light luzJanela;
    public Color corLuzJanelaFinal = new Color(1f, 0.82f, 0.5f);
    [Tooltip("Intensidades ao fim da abertura (o quarto sai da madrugada). Valores negativos mantêm a atual.")]
    public float intensidadeLuzFinal = -1f;
    public float intensidadeJanelaFinal = -1f;
    public float duracaoAbertura = 2.5f;

    private bool concluido = false;

    void Start()
    {
        if (painelBloqueado != null) painelBloqueado.SetActive(false);
        if (painelFinal != null) painelFinal.SetActive(false);
        interagivel = false;
        if (storyState != null) storyState.OnTodosPuzzlesConcluidos.AddListener(Liberar);
    }

    void Liberar()
    {
        interagivel = true;
        textoPrompt = "Aperte E: abrir a cortina";
    }

    public override void Interagir()
    {
        if (concluido) return;

        if (!interagivel)
        {
            if (painelBloqueado != null)
            {
                painelBloqueado.SetActive(true);
                Invoke(nameof(FecharBloqueado), 1.5f);
            }
            return;
        }

        concluido = true;
        interagivel = false;
        if (GameManager.Instance != null) GameManager.Instance.jogoTerminado = true;

        if (luzAmbiente != null) luzAmbiente.color = corFinal;
        if (textoMensagemFinal != null)
            textoMensagemFinal.text = "Ainda é difícil. Mas hoje Cris pediu ajuda.\n\nSe você ou alguém que você conhece precisa de apoio, o CVV atende de graça, 24h, pelo 188 ou cvv.org.br.";
        if (cortinaEsquerda != null || cortinaDireita != null)
            StartCoroutine(SequenciaFinal());
        else if (painelFinal != null)
            painelFinal.SetActive(true);
    }

    System.Collections.IEnumerator SequenciaFinal()
    {
        Vector3 escE = cortinaEsquerda != null ? cortinaEsquerda.localScale : Vector3.one;
        Vector3 escD = cortinaDireita != null ? cortinaDireita.localScale : Vector3.one;
        int n = ceu != null ? ceu.Length : 0;
        Color[] iniciais = new Color[n];
        for (int i = 0; i < n; i++)
            iniciais[i] = ceu[i] != null ? ceu[i].material.GetColor("_BaseColor") : Color.white;
        Color luzIni = luzJanela != null ? luzJanela.color : Color.white;
        float ambIni = luzAmbiente != null ? luzAmbiente.intensity : 1f;
        float janIni = luzJanela != null ? luzJanela.intensity : 1f;

        float t = 0f;
        while (t < duracaoAbertura)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracaoAbertura));
            float esc = Mathf.Lerp(1f, 0.28f, k);
            if (cortinaEsquerda != null) cortinaEsquerda.localScale = new Vector3(escE.x * esc, escE.y, escE.z);
            if (cortinaDireita != null) cortinaDireita.localScale = new Vector3(escD.x * esc, escD.y, escD.z);
            for (int i = 0; i < n; i++)
            {
                if (ceu[i] == null || coresCeuFinal == null || i >= coresCeuFinal.Length) continue;
                ceu[i].material.SetColor("_BaseColor", Color.Lerp(iniciais[i], coresCeuFinal[i], k));
            }
            if (luzJanela != null) luzJanela.color = Color.Lerp(luzIni, corLuzJanelaFinal, k);
            if (luzAmbiente != null && intensidadeLuzFinal >= 0f) luzAmbiente.intensity = Mathf.Lerp(ambIni, intensidadeLuzFinal, k);
            if (luzJanela != null && intensidadeJanelaFinal >= 0f) luzJanela.intensity = Mathf.Lerp(janIni, intensidadeJanelaFinal, k);
            yield return null;
        }

        if (estrelas != null)
            foreach (var e in estrelas) if (e != null) e.enabled = false;

        yield return new WaitForSeconds(1.2f);
        if (painelFinal != null) painelFinal.SetActive(true);
    }

    void FecharBloqueado()
    {
        if (painelBloqueado != null) painelBloqueado.SetActive(false);
    }
}
