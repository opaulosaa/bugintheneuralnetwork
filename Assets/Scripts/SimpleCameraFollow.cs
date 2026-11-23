using UnityEngine;

/// <summary>
/// Câmera simples em terceira pessoa, seguindo a Faísca a uma distância fixa.
/// A posição é limitada para nunca sair da sala (evita atravessar paredes
/// quando o jogador chega perto delas, como no quadro do puzzle 2).
/// Coloque na Main Camera e arraste a Faísca no campo "alvo".
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    public Transform alvo;
    public Vector3 deslocamento = new Vector3(0f, 5f, -4.5f);
    public float suavidade = 6f;

    [Header("Limites da sala (evita atravessar paredes)")]
    public float limiteX = 9f;
    public float limiteZ = 9f;
    [Tooltip("Limite do lado sul (atrás da Ana). Pode ficar fora da sala quando a parede sul é invisível.")]
    public float limiteZSul = -9f;

    void LateUpdate()
    {
        if (alvo == null) return;

        Vector3 posicaoDesejada = alvo.position + deslocamento;
        posicaoDesejada.x = Mathf.Clamp(posicaoDesejada.x, -limiteX, limiteX);
        posicaoDesejada.z = Mathf.Clamp(posicaoDesejada.z, limiteZSul, limiteZ);

        transform.position = Vector3.Lerp(transform.position, posicaoDesejada, suavidade * Time.deltaTime);
        transform.LookAt(alvo.position + Vector3.up * 1f);
    }
}
