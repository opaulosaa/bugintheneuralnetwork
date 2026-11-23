using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimento de Cris (objeto "Faisca"). Requer um CharacterController no mesmo GameObject.
/// Usa o novo Input System (o projeto está configurado para ele).
/// Acelera e freia suavemente, gira na direção do movimento e expõe a velocidade
/// para a animação (CrisMiniatura).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float velocidade = 2.4f;
    public float aceleracao = 10f;        // quão rápido chega na velocidade máxima
    public float frenagem = 14f;          // quão rápido para ao soltar as teclas
    public float velocidadeGiro = 720f;   // graus por segundo
    public float gravidade = -9.81f;

    [HideInInspector] public bool podeMover = true;
    /// <summary>Ligado pelo GameManager enquanto um painel de puzzle está aberto (o boneco não anda por trás da UI).</summary>
    [HideInInspector] public bool bloqueadoPorPainel = false;
    /// <summary>Fração da velocidade (1 = normal). Com pouca Vontade o GameManager deixa o passo mais pesado.</summary>
    [HideInInspector] public float multiplicadorVelocidade = 1f;

    /// <summary>Velocidade no plano do chão, em m/s (0 parada, ~velocidade andando).</summary>
    public float VelocidadeHorizontal => new Vector3(velocidadeAtual.x, 0f, velocidadeAtual.z).magnitude;
    /// <summary>0 a 1: fração da velocidade máxima (para a animação).</summary>
    public float RitmoNormalizado => velocidade > 0f ? Mathf.Clamp01(VelocidadeHorizontal / velocidade) : 0f;

    private CharacterController controller;
    private Vector3 velocidadeAtual;
    private float velocidadeY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 desejada = Vector3.zero;
        if (podeMover && !bloqueadoPorPainel && Keyboard.current != null)
        {
            float h = 0f, v = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;

            Vector3 entrada = new Vector3(h, 0f, v);
            if (entrada.sqrMagnitude > 1f) entrada.Normalize();

            Vector3 direcao = entrada;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 frente = cam.transform.forward; frente.y = 0f; frente.Normalize();
                Vector3 direita = cam.transform.right; direita.y = 0f; direita.Normalize();
                direcao = frente * entrada.z + direita * entrada.x;
            }
            desejada = direcao * velocidade * Mathf.Clamp(multiplicadorVelocidade, 0.2f, 1.5f);
        }

        // Acelera em direção à velocidade desejada (ou freia até parar).
        float taxa = desejada.sqrMagnitude > 0.001f ? aceleracao : frenagem;
        velocidadeAtual = Vector3.MoveTowards(velocidadeAtual, desejada, taxa * velocidade * Time.deltaTime);

        // Gira suavemente para onde está indo.
        if (desejada.sqrMagnitude > 0.001f)
        {
            Quaternion alvo = Quaternion.LookRotation(desejada.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, alvo, velocidadeGiro * Time.deltaTime);
        }

        if (controller.isGrounded && velocidadeY < 0f) velocidadeY = -0.5f;
        else velocidadeY += gravidade * Time.deltaTime;

        // Um único Move por quadro: assim controller.velocity também fica correta.
        CollisionFlags batidas = controller.Move((velocidadeAtual + Vector3.up * velocidadeY) * Time.deltaTime);
        // Ao bater numa parede/móvel, perde a velocidade que empurrava contra ele (não "gruda" acelerada).
        if ((batidas & CollisionFlags.Sides) != 0)
        {
            Vector3 real = controller.velocity; real.y = 0f;
            velocidadeAtual = Vector3.ClampMagnitude(velocidadeAtual, real.magnitude + 0.2f);
        }
    }
}
