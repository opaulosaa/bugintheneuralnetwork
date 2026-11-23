using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Coloque no objeto da Faísca (player). Detecta o Interactable mais próximo
/// dentro do raio e, ao apertar E, chama Interagir() nele.
/// Arraste um Text da UI (Legacy) em "textoPrompt" para mostrar a dica.
/// </summary>
public class ProximityInteractor : MonoBehaviour
{
    public float raioInteracao = 2.5f;
    public LayerMask camadaInteragiveis = ~0;
    public Text textoPrompt;

    private Interactable atual;

    void Update()
    {
        AtualizarAlvo();

        if (atual != null && textoPrompt != null)
        {
            textoPrompt.gameObject.SetActive(atual.interagivel);
            textoPrompt.text = atual.textoPrompt;
        }
        else if (textoPrompt != null)
        {
            textoPrompt.gameObject.SetActive(false);
        }

        // Esc fecha qualquer painel de puzzle aberto.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            foreach (var i in FindObjectsByType<Interactable>(FindObjectsSortMode.None)) i.FecharPainel();

        if (atual != null && atual.interagivel && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            atual.Interagir();
        }
    }

    void AtualizarAlvo()
    {
        Collider[] proximos = Physics.OverlapSphere(transform.position, raioInteracao, camadaInteragiveis);
        Interactable maisProximo = null;
        float menorDistancia = float.MaxValue;

        foreach (var col in proximos)
        {
            var interactable = col.GetComponent<Interactable>();
            if (interactable == null) continue;

            float distancia = Vector3.Distance(transform.position, col.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                maisProximo = interactable;
            }
        }

        atual = maisProximo;
    }
}
