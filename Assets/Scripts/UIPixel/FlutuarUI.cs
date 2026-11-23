using UnityEngine;

/// <summary>
/// Balanço contínuo de um elemento da UI (post-its, ícones, relógio), em degraus de pixel e em tempo real.
/// </summary>
public class FlutuarUI : MonoBehaviour
{
    public float amplitudePixels = 1f;
    public float frequencia = 0.6f;
    public float giroGraus = 0f;
    public float pixel = 4f;

    RectTransform rt;
    Vector2 basePos;
    float baseGiro, fase;
    bool iniciado;

    void OnEnable()
    {
        rt = (RectTransform)transform;
        if (!iniciado) { basePos = rt.anchoredPosition; baseGiro = rt.localEulerAngles.z; fase = Random.value * 10f; iniciado = true; }
    }

    void LateUpdate()
    {
        float s = Mathf.Sin((Time.unscaledTime + fase) * frequencia * Mathf.PI * 2f);
        float dy = Mathf.Round(s * amplitudePixels) * pixel;
        // Não briga com a entrada do PainelPixel: só mexe quando já está perto da posição final.
        if (amplitudePixels > 0f && Mathf.Abs(rt.anchoredPosition.y - basePos.y) <= (amplitudePixels + 1f) * pixel)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, basePos.y + dy);
        if (giroGraus != 0f)
            rt.localRotation = Quaternion.Euler(0f, 0f, baseGiro + Mathf.Round(s * 2f) / 2f * giroGraus);
    }
}
