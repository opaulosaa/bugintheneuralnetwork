using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Linhas de varredura de monitor antigo por cima de uma caixa ou do menu (RawImage com a textura ui_scanline
/// repetida). As linhas descem devagar e o brilho dá umas piscadas raras, como um CRT.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ScanlinesUI : MonoBehaviour
{
    [Tooltip("Altura de cada linha (a textura tem 4 px), em unidades do Canvas.")]
    public float alturaLinha = 4f;
    public float velocidade = 0.6f;
    [Range(0f, 1f)] public float opacidade = 0.22f;

    RawImage img;
    float proximaPiscada, piscada;

    void Awake() { img = GetComponent<RawImage>(); img.raycastTarget = false; }

    void Update()
    {
        Rect r = img.rectTransform.rect;
        float linhas = Mathf.Max(1f, r.height / (alturaLinha * 4f));
        Rect uv = img.uvRect;
        uv.width = 1f;
        uv.height = linhas;
        uv.y = Mathf.Repeat(uv.y - Time.unscaledDeltaTime * velocidade, 1f);
        img.uvRect = uv;

        if (Time.unscaledTime > proximaPiscada) { piscada = 1f; proximaPiscada = Time.unscaledTime + Random.Range(3f, 8f); }
        piscada = Mathf.MoveTowards(piscada, 0f, Time.unscaledDeltaTime * 8f);
        Color c = img.color; c.a = opacidade * (1f + piscada * 0.8f); img.color = c;
    }
}
