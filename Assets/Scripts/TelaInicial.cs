using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu principal do jogo: Jogar, Como jogar e Sair.
/// Enquanto o menu está aberto o tempo fica pausado (timeScale = 0), assim a
/// Vontade da Ana não drena por inércia antes de o jogador começar.
/// Se o painel tiver um CanvasGroup, as trocas de tela são em fade (em tempo real).
/// Os campos novos são opcionais: se algum for nulo, aquela parte é ignorada.
/// </summary>
public class TelaInicial : MonoBehaviour
{
    [Header("Menu principal")]
    public GameObject painel;
    public GameObject faiscaPlayer;
    public Button botaoComecar;      // botão "Jogar"
    public Button botaoComoJogar;
    public Button botaoSair;

    [Header("Como jogar")]
    public GameObject painelComoJogar;
    public Button botaoVoltar;

    [Header("Transições")]
    public float duracaoSaida = 0.8f;
    public float duracaoTroca = 0.25f;
    [Tooltip("Elementos do jogo (ex.: HUD dos puzzles) que ficam escondidos enquanto o menu está aberto.")]
    public GameObject[] esconderNoMenu;

    private bool saindo;

    void Start()
    {
        Time.timeScale = 0f;
        DefinirMovimento(false);
        MostrarJogo(false);

        if (painel != null) painel.SetActive(true);
        if (painelComoJogar != null) painelComoJogar.SetActive(false);

        if (botaoComecar != null) botaoComecar.onClick.AddListener(Comecar);
        if (botaoComoJogar != null) botaoComoJogar.onClick.AddListener(AbrirComoJogar);
        if (botaoVoltar != null) botaoVoltar.onClick.AddListener(FecharComoJogar);
        if (botaoSair != null) botaoSair.onClick.AddListener(Sair);
    }

    void Comecar()
    {
        if (saindo) return;
        saindo = true;
        // O quarto volta a andar já durante o fade; a Ana só se mexe quando o menu some.
        Time.timeScale = 1f;
        StartCoroutine(FecharMenu(painel != null ? painel.GetComponent<CanvasGroup>() : null));
    }

    IEnumerator FecharMenu(CanvasGroup grupo)
    {
        if (grupo != null)
        {
            grupo.interactable = false;
            float t = 0f;
            while (t < duracaoSaida)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duracaoSaida);
                grupo.alpha = 1f - k * k;
                yield return null;
            }
        }
        if (painel != null) painel.SetActive(false);
        if (grupo != null) { grupo.alpha = 1f; grupo.interactable = true; }
        MostrarJogo(true);
        DefinirMovimento(true);
        saindo = false;
    }

    void AbrirComoJogar()
    {
        if (painelComoJogar == null) return;
        painelComoJogar.SetActive(true);
        StartCoroutine(Fade(painelComoJogar.GetComponent<CanvasGroup>(), 0f, 1f, null));
    }

    void FecharComoJogar()
    {
        if (painelComoJogar == null) return;
        StartCoroutine(Fade(painelComoJogar.GetComponent<CanvasGroup>(), 1f, 0f, () => painelComoJogar.SetActive(false)));
    }

    IEnumerator Fade(CanvasGroup grupo, float de, float para, System.Action fim)
    {
        if (grupo != null)
        {
            float t = 0f;
            while (t < duracaoTroca)
            {
                t += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Lerp(de, para, t / duracaoTroca);
                yield return null;
            }
            grupo.alpha = para;
        }
        fim?.Invoke();
    }

    void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void MostrarJogo(bool mostrar)
    {
        if (esconderNoMenu == null) return;
        foreach (var g in esconderNoMenu) if (g != null) g.SetActive(mostrar);
    }

    void DefinirMovimento(bool pode)
    {
        if (faiscaPlayer == null) return;
        var mov = faiscaPlayer.GetComponent<PlayerMovement>();
        if (mov != null) mov.podeMover = pode;
    }

    void OnDestroy()
    {
        // Garante que o jogo nunca fique pausado ao recarregar a cena ou sair do Play.
        Time.timeScale = 1f;
    }
}
