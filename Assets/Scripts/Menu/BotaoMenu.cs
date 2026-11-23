using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Botão do menu com animação própria (em tempo real, funciona com o jogo pausado):
/// cresce e clareia ao passar o mouse, a barrinha de destaque se estende e afunda ao clicar.
/// </summary>
[RequireComponent(typeof(Button))]
public class BotaoMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public Image fundo;
    public Text texto;
    public RectTransform destaque;          // barrinha à esquerda
    public Color corFundo = new Color(0.30f, 0.85f, 0.95f, 0.10f);
    public Color corFundoHover = new Color(0.30f, 0.85f, 0.95f, 0.28f);
    public Color corTexto = new Color(0.75f, 0.95f, 1f);
    public Color corTextoHover = Color.white;
    public float escalaHover = 1.06f;
    public float larguraDestaque = 6f;

    bool sobre, apertado;
    float k;   // 0..1 do hover

    void OnEnable() { sobre = apertado = false; k = 0f; Aplicar(); }

    public void OnPointerEnter(PointerEventData e) { sobre = true; }
    public void OnPointerExit(PointerEventData e) { sobre = false; apertado = false; }
    public void OnPointerDown(PointerEventData e) { apertado = true; }
    public void OnPointerUp(PointerEventData e) { apertado = false; }
    public void OnSelect(BaseEventData e) { sobre = true; }
    public void OnDeselect(BaseEventData e) { sobre = false; }

    void Update()
    {
        k = Mathf.MoveTowards(k, sobre ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        Aplicar();
    }

    void Aplicar()
    {
        float e = 1f - (1f - k) * (1f - k);
        float escala = Mathf.Lerp(1f, escalaHover, e) * (apertado ? 0.96f : 1f);
        transform.localScale = new Vector3(escala, escala, 1f);
        if (fundo != null) fundo.color = Color.Lerp(corFundo, corFundoHover, e);
        if (texto != null) texto.color = Color.Lerp(corTexto, corTextoHover, e);
        if (destaque != null) destaque.sizeDelta = new Vector2(Mathf.Lerp(larguraDestaque, larguraDestaque * 3f, e), destaque.sizeDelta.y);
    }
}
