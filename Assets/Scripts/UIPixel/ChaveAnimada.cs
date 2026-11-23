using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Chave liga/desliga do firewall: dá um "pulo" de pixel ao mudar de estado.</summary>
[RequireComponent(typeof(Toggle))]
public class ChaveAnimada : MonoBehaviour
{
    public RectTransform chave;
    public Text rotulo;
    public Color corLigada = new Color(0.75f, 1f, 0.95f);
    public Color corDesligada = new Color(0.62f, 0.66f, 0.76f);

    Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(_ => { if (isActiveAndEnabled) StartCoroutine(Pular()); Pintar(); });
    }

    void OnEnable() { Pintar(); if (chave != null) chave.localScale = Vector3.one; }

    void Pintar() { if (rotulo != null) rotulo.color = toggle.isOn ? corLigada : corDesligada; }

    IEnumerator Pular()
    {
        if (chave == null) yield break;
        float[] passos = { 1.25f, 1.12f, 0.94f, 1f };
        foreach (float s in passos)
        {
            chave.localScale = new Vector3(s, 2f - s, 1f);
            yield return new WaitForSecondsRealtime(0.04f);
        }
        chave.localScale = Vector3.one;
    }
}
