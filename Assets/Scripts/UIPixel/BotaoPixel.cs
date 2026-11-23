using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Botão pixelado dos puzzles. Ao passar o mouse: sobe 1 pixel, troca para a borda clara e mostra
/// um cursor ">" piscando. Ao clicar: afunda 1 pixel. Desabilitado (post-it já usado): fica apagado e riscado.
/// Tudo em tempo real e em degraus inteiros de pixel.
/// </summary>
[RequireComponent(typeof(Button))]
public class BotaoPixel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public Image fundo;
    public Text texto;
    public Graphic cursor;
    public Sprite spriteNormal;
    public Sprite spriteHover;
    public Color corTexto = new Color(0.80f, 0.94f, 1f);
    public Color corTextoHover = Color.white;
    public float pixel = 4f;

    Button botao;
    RectTransform rt;
    Vector2 basePos;
    bool sobre, apertado, iniciado;
    float tremor;
    string textoOriginal;

    void Awake()
    {
        botao = GetComponent<Button>();
        rt = (RectTransform)transform;
        if (texto != null) textoOriginal = texto.text;
    }

    void OnEnable()
    {
        if (!iniciado) { basePos = rt.anchoredPosition; iniciado = true; }
        sobre = apertado = false;
        Aplicar();
    }

    void OnDisable() { if (iniciado) rt.anchoredPosition = basePos; }

    public void OnPointerEnter(PointerEventData e) { sobre = true; }
    public void OnPointerExit(PointerEventData e) { sobre = false; apertado = false; }
    public void OnPointerDown(PointerEventData e) { apertado = true; }
    public void OnPointerUp(PointerEventData e) { apertado = false; }
    public void OnSelect(BaseEventData e) { sobre = true; }
    public void OnDeselect(BaseEventData e) { sobre = false; }

    /// <summary>Chacoalha o botão (resposta errada).</summary>
    public void Tremer() { tremor = 0.3f; }

    void Update() { Aplicar(); }

    void Aplicar()
    {
        bool ativo = botao == null || botao.interactable;
        bool destaque = ativo && sobre;
        tremor = Mathf.Max(0f, tremor - Time.unscaledDeltaTime);

        // A posição base acompanha a entrada animada do PainelPixel (só ajustamos o deslocamento).
        float dy = destaque ? (apertado ? -1f : 1f) * pixel : 0f;
        float dx = tremor > 0f ? (Mathf.Repeat(Time.unscaledTime * 30f, 2f) < 1f ? -1f : 1f) * pixel : 0f;
        Vector2 alvo = basePos + new Vector2(dx, dy);
        if (rt.anchoredPosition != alvo && Mathf.Abs(rt.anchoredPosition.y - basePos.y) <= pixel * 1.01f) rt.anchoredPosition = alvo;

        if (fundo != null)
        {
            if (spriteNormal != null && spriteHover != null) fundo.sprite = destaque ? spriteHover : spriteNormal;
            Color c = fundo.color; c.a = ativo ? 1f : 0.35f; fundo.color = c;
        }
        if (texto != null)
        {
            texto.color = ativo ? (destaque ? corTextoHover : corTexto) : new Color(corTexto.r, corTexto.g, corTexto.b, 0.45f);
        }
        if (cursor != null)
            cursor.enabled = destaque && Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.3f;
    }
}
