using UnityEngine;

/// <summary>
/// Animação procedural de Cris em miniatura (o boneco controlado pelo jogador).
/// Andando: pernas e braços balançam no ritmo da velocidade, o corpo sobe e desce a cada passo,
/// inclina para a frente e ginga de leve. Parada: respira e, de vez em quando, suspira.
/// Com pouca Vontade a postura cai (corpo mais curvado, passos mais curtos).
/// Montado por Bug In The Neural Network > Versão Cris > 1. Personagem Cris (boneco neutro).
/// Comemorar() faz um pulinho quando um puzzle é resolvido e o quarto começa a ser arrumado.
/// </summary>
public class CrisMiniatura : MonoBehaviour
{
    public CharacterController controller;
    public Transform pernaEsquerda, pernaDireita, bracoEsquerdo, bracoDireito, corpo;

    [Header("Andar")]
    public float amplitudePernas = 32f;
    public float amplitudeBracos = 26f;
    public float passosPorMetro = 2.6f;      // ciclos de pernas por metro andado
    public float alturaPasso = 0.025f;
    public float inclinacaoAndando = 7f;
    public float ginga = 3f;

    [Header("Parada")]
    public float respiracao = 0.006f;
    public float intervaloSuspiro = 7f;

    [Header("Postura (Vontade)")]
    public float curvaturaSemVontade = 10f;

    private PlayerMovement movimento;
    private float fase;
    private float ritmoSuave;
    private float proximoSuspiro;
    private float suspiro;              // 0..1, decai depois de cada suspiro
    private Vector3 corpoBase;
    private float bracoEZ, bracoDZ;
    private float pulo;                 // 1 no início do pulinho de comemoração, decai até 0

    /// <summary>Pulinho rápido de comemoração (chamado pela ArrumacaoQuarto).</summary>
    public void Comemorar() { pulo = 1f; }

    void Start()
    {
        movimento = GetComponent<PlayerMovement>();
        if (corpo != null) corpoBase = corpo.localPosition;
        if (bracoEsquerdo != null) bracoEZ = Angulo(bracoEsquerdo.localEulerAngles.z);
        if (bracoDireito != null) bracoDZ = Angulo(bracoDireito.localEulerAngles.z);
        proximoSuspiro = Time.time + intervaloSuspiro;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float vel = 0f, ritmo = 0f;
        if (movimento != null) { vel = movimento.VelocidadeHorizontal; ritmo = movimento.RitmoNormalizado; }
        else if (controller != null)
        {
            Vector3 v = controller.velocity; v.y = 0f;
            vel = v.magnitude; ritmo = Mathf.Clamp01(vel / 2.4f);
        }
        ritmoSuave = Mathf.Lerp(ritmoSuave, ritmo, 1f - Mathf.Exp(-10f * dt));

        // A fase avança com a distância andada: os pés acompanham o chão, sem "patinar".
        fase += vel * passosPorMetro * Mathf.PI * dt;
        if (ritmoSuave < 0.05f) fase = Mathf.Lerp(fase, Mathf.Round(fase / Mathf.PI) * Mathf.PI, 1f - Mathf.Exp(-8f * dt));

        float vontade01 = GameManager.Instance != null ? GameManager.Instance.vontade / GameManager.Instance.vontadeMax : 0.5f;
        float desanimo = 1f - Mathf.Clamp01(vontade01 * 1.4f);   // só pesa quando a Vontade está baixa
        float passo = Mathf.Lerp(1f, 0.7f, desanimo);           // passos mais curtos sem vontade

        float s = Mathf.Sin(fase);
        float k = 1f - Mathf.Exp(-16f * dt);
        Girar(pernaEsquerda, s * amplitudePernas * ritmoSuave * passo, 0f, k);
        Girar(pernaDireita, -s * amplitudePernas * ritmoSuave * passo, 0f, k);

        // Suspiro de vez em quando, parada: ombros sobem e descem.
        if (ritmoSuave < 0.05f && Time.time > proximoSuspiro)
        {
            suspiro = 1f;
            proximoSuspiro = Time.time + intervaloSuspiro * Random.Range(0.8f, 1.4f);
        }
        if (ritmoSuave > 0.2f) proximoSuspiro = Time.time + intervaloSuspiro;
        suspiro = Mathf.MoveTowards(suspiro, 0f, dt * 0.6f);
        float curvaSuspiro = Mathf.Sin(suspiro * Mathf.PI);   // sobe e desce

        float bracoParado = Mathf.Lerp(0f, 4f, desanimo);     // braços mais caídos, colados ao corpo
        Girar(bracoEsquerdo, -s * amplitudeBracos * ritmoSuave * passo, bracoEZ + bracoParado, k);
        Girar(bracoDireito, s * amplitudeBracos * ritmoSuave * passo, bracoDZ - bracoParado, k);

        if (corpo != null)
        {
            pulo = Mathf.MoveTowards(pulo, 0f, dt * 2.2f);
            float subida = Mathf.Abs(Mathf.Cos(fase)) * alturaPasso * ritmoSuave + Mathf.Sin(pulo * Mathf.PI) * 0.09f;
            float respira = (Mathf.Sin(Time.time * 1.7f) * respiracao + curvaSuspiro * 0.012f) * (1f - ritmoSuave);
            corpo.localPosition = Vector3.Lerp(corpo.localPosition, corpoBase + Vector3.up * (subida + respira), k);

            float frente = inclinacaoAndando * ritmoSuave + curvaturaSemVontade * desanimo - curvaSuspiro * 3f;
            float lado = Mathf.Sin(fase) * ginga * ritmoSuave;
            corpo.localRotation = Quaternion.Slerp(corpo.localRotation, Quaternion.Euler(frente, 0f, lado), k);
        }
    }

    static void Girar(Transform t, float anguloX, float anguloZ, float k)
    {
        if (t == null) return;
        t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(anguloX, 0f, anguloZ), k);
    }

    static float Angulo(float a) { return a > 180f ? a - 360f : a; }
}
