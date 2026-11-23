using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quando outro script troca o texto (ex.: feedback do puzzle), ele aparece digitado letra por letra
/// e treme rapidinho, em degraus de pixel. As letras que faltam ficam transparentes (o layout não muda).
/// Funciona com o jogo pausado.
/// </summary>
[RequireComponent(typeof(Text))]
public class TextoDigitado : MonoBehaviour
{
    public float letrasPorSegundo = 90f;
    public float pixel = 4f;

    Text t;
    string alvo = "", mostrado = "";
    float n, tremor;
    Vector2 basePos;
    RectTransform rt;

    void Awake() { t = GetComponent<Text>(); rt = t.rectTransform; basePos = rt.anchoredPosition; alvo = mostrado = t.text; n = alvo.Length; }

    /// <summary>Digita de novo o mesmo texto (o puzzle repetiu a mensagem, ex.: outro erro).</summary>
    public void Repetir() { n = 0f; tremor = 0.25f; }

    void LateUpdate()
    {
        if (t.text != mostrado) { alvo = t.text; n = 0f; tremor = 0.2f; }
        if (n < alvo.Length)
        {
            n += Time.unscaledDeltaTime * letrasPorSegundo;
            int k = Mathf.Min(alvo.Length, Mathf.FloorToInt(n));
            mostrado = PainelPixel.Revelar(alvo, k);
            t.text = mostrado;
        }
        else if (t.text != alvo) { t.text = alvo; mostrado = alvo; }
        else mostrado = t.text;

        tremor = Mathf.Max(0f, tremor - Time.unscaledDeltaTime);
        rt.anchoredPosition = basePos + (tremor > 0f ? new Vector2(Mathf.Repeat(Time.unscaledTime * 40f, 2f) < 1f ? pixel : -pixel, 0f) : Vector2.zero);
    }
}
