using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= DETALHES QUE CONTAM A HISTÓRIA =======================
// Sem precisar de texto na tela: o despertador marcando 3h12 ao lado da cama, os slides
// da apresentação quase vazios no monitor e o post-it lembrando que ela é às 9h.
// Tudo fica em "Detalhes_Historia". Rodar de novo apaga e recria.
public static partial class SceneSetup
{
    const string HORA_MADRUGADA = "16:40";   // a história agora começa no fim da tarde

    [MenuItem("Bug In The Neural Network/Povoar Quarto - 4. Detalhes da Historia")]
    public static void MontarDetalhesHistoria()
    {
        ApagarSeExistir("Detalhes_Historia");
        Transform raiz = new GameObject("Detalhes_Historia").transform;
        InicializarMateriaisComida();

        Material plastico = MatURP("Hist_Plastico_Preto", new Color(0.06f, 0.06f, 0.07f), false);
        Material visor = MatURP("Hist_Visor", new Color(0.12f, 0.02f, 0.02f), true);
        Material slideFundo = MatURP("Hist_Slide_Fundo", new Color(0.86f, 0.88f, 0.92f), true);
        Material slideTitulo = MatURP("Hist_Slide_Titulo", new Color(0.20f, 0.32f, 0.55f), true);
        Material slideLinha = MatURP("Hist_Slide_Linha", new Color(0.62f, 0.65f, 0.72f), true);
        Material barraTarefas = MatURP("Hist_Barra_Tarefas", new Color(0.10f, 0.11f, 0.14f), true);
        Material postit = MatURP("Hist_PostIt", new Color(0.95f, 0.85f, 0.35f), false);
        Material postitRosa = MatURP("Hist_PostIt_Rosa", new Color(0.92f, 0.60f, 0.70f), false);
        Color tinta = new Color(0.12f, 0.12f, 0.16f);
        Color vermelho = new Color(1f, 0.16f, 0.12f);

        // ---------- Despertador no chão, perto da cabeceira ----------
        GameObject cama = GameObject.Find("bedSingle");
        Bounds bCama;
        if (!TentarBounds(cama, out bCama)) bCama = new Bounds(new Vector3(2.79f, 0.28f, 0.85f), new Vector3(2.1f, 0.56f, 1.45f));
        bool camaEmX = bCama.size.x > bCama.size.z;
        Vector3 pRelogio = camaEmX ? new Vector3(bCama.max.x - 0.3f, 0f, bCama.min.z - 0.22f)
                                   : new Vector3(bCama.min.x - 0.22f, 0f, bCama.max.z - 0.3f);
        // De frente para a câmera (que olha do sul para o norte), levemente virado para o quarto.
        Transform relogio = Filho(raiz, "Despertador", pRelogio, new Vector3(0f, 200f, 0f));
        Bloco(relogio, "Corpo", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.18f, 0.1f, 0.08f), plastico);
        Bloco(relogio, "Visor", PrimitiveType.Cube, new Vector3(0f, 0.052f, 0.041f), new Vector3(0.15f, 0.065f, 0.002f), visor);
        TextoNoMundo(relogio, "Hora", HORA_MADRUGADA, new Vector3(0f, 0.052f, 0.043f), new Vector3(0f, 180f, 0f), 0.05f, vermelho, TextAnchor.MiddleCenter);
        GameObject brilho = new GameObject("Luz_Despertador");
        brilho.transform.SetParent(relogio, false);
        brilho.transform.localPosition = new Vector3(0f, 0.06f, 0.15f);
        Light lr = brilho.AddComponent<Light>();
        lr.type = LightType.Point; lr.color = vermelho; lr.intensity = 0.12f; lr.range = 0.45f; lr.shadows = LightShadows.None;

        // ---------- Slides da apresentação no monitor ----------
        Transform monitor = null;
        GameObject decor = GameObject.Find("Quarto_Decor");
        if (decor != null) monitor = decor.transform.Find("Kenney_computerScreen");
        Bounds bMon;
        if (monitor != null && TentarBounds(monitor.gameObject, out bMon))
        {
            // O monitor está virado para +X (para o quarto). A "tela" é um plano logo à frente dele.
            float altura = bMon.size.y * 0.62f, largura = bMon.size.z * 0.86f;
            Vector3 centro = new Vector3(bMon.max.x + 0.004f, bMon.max.y - altura * 0.5f - bMon.size.y * 0.06f, bMon.center.z);
            Transform tela = Filho(raiz, "Slide_No_Monitor", centro, new Vector3(0f, -90f, 0f));
            // Em coordenadas locais: x = largura (da esquerda para a direita de quem olha), y = altura, -z = para fora da tela.
            Bloco(tela, "Fundo", PrimitiveType.Cube, Vector3.zero, new Vector3(largura, altura, 0.002f), slideFundo);
            Bloco(tela, "Barra_Tarefas", PrimitiveType.Cube, new Vector3(0f, -altura * 0.45f, -0.001f), new Vector3(largura, altura * 0.1f, 0.002f), barraTarefas);
            Bloco(tela, "Faixa_Titulo", PrimitiveType.Cube, new Vector3(0f, altura * 0.3f, -0.001f), new Vector3(largura * 0.9f, altura * 0.2f, 0.002f), slideTitulo);
            TextoNoMundo(tela, "Titulo", "Apresentação Final", new Vector3(-largura * 0.42f, altura * 0.3f, -0.003f), Vector3.zero, altura * 0.11f, Color.white, TextAnchor.MiddleLeft);
            // Um tópico escrito, o segundo começado e largado no meio, o resto em branco.
            Bloco(tela, "Linha_1", PrimitiveType.Cube, new Vector3(-largura * 0.12f, altura * 0.08f, -0.001f), new Vector3(largura * 0.6f, altura * 0.035f, 0.002f), slideLinha);
            Bloco(tela, "Linha_2", PrimitiveType.Cube, new Vector3(-largura * 0.3f, -altura * 0.04f, -0.001f), new Vector3(largura * 0.24f, altura * 0.035f, 0.002f), slideLinha);
            Bloco(tela, "Cursor", PrimitiveType.Cube, new Vector3(-largura * 0.17f, -altura * 0.04f, -0.0015f), new Vector3(largura * 0.008f, altura * 0.07f, 0.002f), slideTitulo);
            TextoNoMundo(tela, "Contador", "slide 2 de 15", new Vector3(largura * 0.42f, -altura * 0.3f, -0.003f), Vector3.zero, altura * 0.06f, tinta, TextAnchor.MiddleRight);
            TextoNoMundo(tela, "Relogio_Barra", HORA_MADRUGADA, new Vector3(largura * 0.44f, -altura * 0.45f, -0.003f), Vector3.zero, altura * 0.06f, Color.white, TextAnchor.MiddleRight);

            // ---------- Post-its colados no canto da tela ----------
            Vector3 pPost = new Vector3(bMon.max.x + 0.008f, bMon.max.y - 0.07f, bMon.max.z - 0.06f);
            Transform p1 = Filho(raiz, "PostIt_9h", pPost, new Vector3(0f, -90f, -8f));
            Bloco(p1, "Papel", PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.1f, 0.002f), postit);
            TextoNoMundo(p1, "Texto", "APRESENTAÇÃO\namanhã 9h!!", new Vector3(0f, 0f, -0.002f), Vector3.zero, 0.0105f, tinta, TextAnchor.MiddleCenter);

            Vector3 pPost2 = new Vector3(bMon.max.x + 0.008f, bMon.max.y - 0.17f, bMon.max.z - 0.05f);
            Transform p2 = Filho(raiz, "PostIt_Ensaiar", pPost2, new Vector3(0f, -90f, 6f));
            Bloco(p2, "Papel", PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.1f, 0.002f), postitRosa);
            TextoNoMundo(p2, "Texto", "ensaiar\npelo menos\n1 vez", new Vector3(0f, 0f, -0.002f), Vector3.zero, 0.0095f, tinta, TextAnchor.MiddleCenter);
        }
        else Debug.LogWarning("Monitor 'Kenney_computerScreen' não encontrado em Quarto_Decor: slides e post-its não foram criados.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Detalhes da história montados em 'Detalhes_Historia'. Salve a cena (Ctrl+S).");
    }

    // Texto 3D (TextMesh) com a fonte embutida. "altura" é a altura aproximada de uma linha em metros.
    // O texto é legível para quem olha na direção do +Z local do objeto.
    static TextMesh TextoNoMundo(Transform pai, string nome, string texto, Vector3 pos, Vector3 rot, float altura, Color cor, TextAnchor ancora)
    {
        GameObject g = new GameObject(nome);
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(rot);
        TextMesh tm = g.AddComponent<TextMesh>();
        Font fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = fonte;
        tm.text = texto;
        tm.fontSize = 64;
        tm.characterSize = 1f;
        tm.anchor = ancora;
        tm.alignment = ancora == TextAnchor.MiddleLeft ? TextAlignment.Left : ancora == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
        tm.color = cor;
        g.GetComponent<MeshRenderer>().sharedMaterial = fonte.material;
        // Com fontSize 64 e characterSize 1, uma linha mede ~6,4 unidades; a escala leva à altura pedida.
        // A escala do pai é desfeita para o texto não herdar distorção.
        Vector3 ep = pai.lossyScale;
        float s = altura / 6.4f;
        g.transform.localScale = new Vector3(s / Mathf.Max(ep.x, 1e-4f), s / Mathf.Max(ep.y, 1e-4f), s / Mathf.Max(ep.z, 1e-4f));
        return tm;
    }
}
