using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= LIMITE SUL DO QUARTO =======================
// No "Quarto da Referência" o chão e as paredes laterais continuavam 3 m além da parede sul
// (invisível), onde a câmera fica. Isso mostrava um chão em que a Ana não conseguia pisar:
// parecia uma barreira invisível no meio do caminho. Agora o chão termina junto com a parede
// e uma mureta baixa, em corte, mostra onde o quarto acaba (como numa casa de bonecas).
public static partial class SceneSetup
{
    const float ALTURA_MURETA = 0.22f;

    [MenuItem("Bug In The Neural Network/Corrigir Limite Sul do Quarto")]
    public static void CorrigirLimiteSul()
    {
        GameObject chao = GameObject.Find("Chao_Quarto");
        if (chao != null)
        {
            chao.transform.position = new Vector3(0f, 0f, 0f);
            chao.transform.localScale = new Vector3(2f * REF_X / 10f, 1f, 2f * REF_Z / 10f);
            EditorUtility.SetDirty(chao);
        }

        foreach (string n in new[] { "Parede_Leste", "Parede_Oeste" })
        {
            GameObject p = GameObject.Find(n);
            if (p == null) continue;
            p.transform.position = new Vector3(p.transform.position.x, p.transform.position.y, 0f);
            Vector3 e = p.transform.localScale;
            p.transform.localScale = new Vector3(e.x, e.y, 2f * REF_Z + 0.3f);
            EditorUtility.SetDirty(p);
        }

        GameObject tabuas = GameObject.Find("Piso_Tabuas");
        if (tabuas != null)
            foreach (Transform f in tabuas.transform)
            {
                f.position = new Vector3(f.position.x, f.position.y, 0f);
                f.localScale = new Vector3(f.localScale.x, f.localScale.y, 2f * REF_Z);
            }

        // A parede sul continua invisível (só colisão); a mureta é a parte "cortada" que aparece.
        ApagarSeExistir("Mureta_Corte_Sul");
        Transform mureta = new GameObject("Mureta_Corte_Sul").transform;
        Material parede = MatURP("Ref_Parede", new Color(0.93f, 0.91f, 0.95f), false);
        Material corte = MatURP("Ref_Corte_Parede", new Color(0.10f, 0.09f, 0.13f), false);
        float largura = 2f * REF_X + 0.3f;
        Bloco(mureta, "Mureta", PrimitiveType.Cube, new Vector3(0f, ALTURA_MURETA * 0.5f, -REF_Z), new Vector3(largura, ALTURA_MURETA, 0.3f), parede);
        Bloco(mureta, "Corte", PrimitiveType.Cube, new Vector3(0f, ALTURA_MURETA + 0.005f, -REF_Z), new Vector3(largura, 0.01f, 0.3f), corte);
        // Corte no topo das paredes laterais, na ponta sul.
        foreach (float x in new[] { -REF_X, REF_X })
            Bloco(mureta, "Corte_Lateral", PrimitiveType.Cube, new Vector3(x, REF_ALTURA * 0.5f, -REF_Z - 0.151f), new Vector3(0.3f, REF_ALTURA, 0.004f), corte);

        // A câmera pode ficar além da parede, sobre o escuro, sem limite novo.
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Limite sul corrigido: o chão termina na parede e a mureta marca a borda. Salve a cena (Ctrl+S).");
    }

    // A cena guarda os valores antigos do Inspector (velocidade 4 m/s, rápida demais para uma
    // miniatura de 95 cm); isto aplica os valores pensados para o movimento com aceleração.
    [MenuItem("Bug In The Neural Network/Ajustar Movimento de Cris")]
    public static void AjustarMovimentoAna()
    {
        var mov = Object.FindFirstObjectByType<PlayerMovement>();
        if (mov != null)
        {
            mov.velocidade = 2.4f;
            mov.aceleracao = 10f;
            mov.frenagem = 14f;
            mov.velocidadeGiro = 720f;
            EditorUtility.SetDirty(mov);
        }
        var anim = Object.FindFirstObjectByType<CrisMiniatura>();
        if (anim != null)
        {
            anim.amplitudePernas = 32f;
            anim.amplitudeBracos = 26f;
            anim.passosPorMetro = 2.6f;
            anim.alturaPasso = 0.025f;
            anim.inclinacaoAndando = 7f;
            anim.ginga = 3f;
            EditorUtility.SetDirty(anim);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Movimento de Cris ajustado. Salve a cena (Ctrl+S).");
    }

    // Linha de comando: -executeMethod SceneSetup.AplicarCorrecoesBatch -quit
    public static void AplicarCorrecoesBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        CorrigirLimiteSul();
        AjustarMovimentoAna();
        EstilizarMenuInicial();
        EditorSceneManager.SaveOpenScenes();
    }

    // Linha de comando: -executeMethod SceneSetup.EstilizarMenuBatch -quit
    public static void EstilizarMenuBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EstilizarMenuInicial();
        EditorSceneManager.SaveOpenScenes();
    }
}
