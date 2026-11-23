using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= A CAMA COMO "NINHO" =======================
// A cama é onde a Ana passa quase o dia todo: sai do canto e avança para o meio do quarto
// (cabeceira na parede leste), mais larga, com o edredom formando um casulo em volta do buraco
// onde ela estava deitada. No fundo do buraco, o celular aceso (Puzzle 3) ilumina o casulo.
public static partial class SceneSetup
{
    const float LARGURA_CAMA = 1.45f;
    const float COMPRIMENTO_CAMA = 2.1f;
    const float Z_CAMA = 0.85f;   // centro da cama na parede leste

    [MenuItem("Bug In The Neural Network/Povoar Quarto - 3. Cama Ninho")]
    public static void MontarCamaNinho()
    {
        float xi = REF_X - 0.15f;
        GameObject cama = GameObject.Find("bedSingle");
        if (cama == null) { Debug.LogError("Cama 'bedSingle' não encontrada."); return; }

        // ---------- Cabeceira na parede leste, pé apontando para o meio do quarto ----------
        // (o modelo Kenney tem a cabeceira no -Z local; com Y = -90 ela fica no +X do mundo)
        cama.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        Bounds b;
        TentarBounds(cama, out b);
        Vector3 e = cama.transform.localScale;
        if (b.size.z > 0.01f && Mathf.Abs(b.size.z - LARGURA_CAMA) > 0.05f) e.x *= LARGURA_CAMA / b.size.z;
        if (b.size.x > 0.01f && Mathf.Abs(b.size.x - COMPRIMENTO_CAMA) > 0.05f) e.z *= COMPRIMENTO_CAMA / b.size.x;
        cama.transform.localScale = e;
        TentarBounds(cama, out b);
        cama.transform.position += new Vector3(0f, 0f, Z_CAMA - b.center.z);
        EncostarX(cama, xi - 0.01f, false);
        ColisorPorBounds(cama);
        TentarBounds(cama, out b);
        Debug.Log("Cama: " + b.size.x.ToString("0.00") + " x " + b.size.z.ToString("0.00") + " m, centro " + b.center);

        // O edredom e os travesseiros antigos (do quarto da referência) dão lugar ao ninho.
        GameObject decor = GameObject.Find("Quarto_Decor");
        if (decor != null)
            for (int i = decor.transform.childCount - 1; i >= 0; i--)
            {
                Transform t = decor.transform.GetChild(i);
                if (t.name == "Edredom" || t.name == "Edredom_Aba" || t.name.StartsWith("Kenney_pillow"))
                    Object.DestroyImmediate(t.gameObject);
            }
        // Coisas da bagunça antiga que estavam na cama velha ou onde a cama nova ficou.
        foreach (string n in new[] { "Cama_Camiseta", "Cama_Moletom", "Cama_Calca", "Cama_Lata", "Cama_Meia", "Chao_Monte_Cama" })
            ApagarSeExistir(n);
        GameObject antiga = GameObject.Find("Bagunca_Quarto");
        if (antiga != null)
            for (int i = antiga.transform.childCount - 1; i >= 0; i--)
            {
                Transform t = antiga.transform.GetChild(i);
                Vector3 p = t.position;
                if (p.x > b.min.x - 0.05f && p.z > b.min.z - 0.05f && p.z < b.max.z + 0.05f)
                    Object.DestroyImmediate(t.gameObject);
            }

        // A bagunça da camada 1 depende da cama: recria tudo (já com o ninho).
        PovoarQuartoAcumulo();
        Debug.Log("Cama-ninho montada. Salve a cena (Ctrl+S).");
    }

    // Chamado de dentro de PovoarQuartoAcumulo, com os colisores temporários da cama já prontos.
    // Tudo é montado num referencial da própria cama: +Z local = cabeceira, +X local = lado do quarto.
    static void MontarNinho(Bounds b, Material[] roupas)
    {
        GameObject cama = GameObject.Find("bedSingle");
        Vector3 cabeca = cama != null ? cama.transform.rotation * Vector3.back : Vector3.forward;
        cabeca.y = 0f; cabeca.Normalize();
        bool emX = Mathf.Abs(cabeca.x) > Mathf.Abs(cabeca.z);
        float L = emX ? b.size.x : b.size.z;   // comprimento
        float W = emX ? b.size.z : b.size.x;   // largura

        Transform f = Filho(raizBagunca, "Ninho", new Vector3(b.center.x, 0f, b.center.z), Vector3.zero);
        f.rotation = Quaternion.LookRotation(cabeca);
        // O lado do quarto (por onde ela sai, quando sai) é o que aponta para o centro.
        float lado = Vector3.Dot(f.right, new Vector3(-b.center.x, 0f, -b.center.z)) >= 0f ? 1f : -1f;

        float yColchao = AlturaEm(b.center.x, b.center.z, b.max.y);
        Vector3 Mundo(float lx, float ly, float lz) { return f.TransformPoint(new Vector3(lx * lado, ly, lz)); }
        GameObject Peca(string nome, PrimitiveType tipo, float lx, float ly, float lz, Vector3 esc, Vector3 rot, Material m)
        {
            return BlocoR(f, nome, tipo, new Vector3(lx * lado, ly, lz), esc, rot, m);
        }

        Material lencol = MatURP("Dep_Lencol", new Color(0.80f, 0.80f, 0.84f), false);
        float zBuraco = -L * 0.02f;

        // Lençol solto, com a ponta do pé escapando do colchão.
        Peca("Lencol", PrimitiveType.Cube, 0f, yColchao + 0.006f, -L * 0.05f, new Vector3(W * 0.97f, 0.012f, L * 0.78f), new Vector3(0f, 2f, 0f), lencol);
        Peca("Lencol_Solto", PrimitiveType.Cube, W * 0.25f, yColchao - 0.1f, -L * 0.5f - 0.02f, new Vector3(0.5f, 0.22f, 0.02f), new Vector3(-12f, 0f, 6f), lencol);

        // Casulo: o edredom forma uma parede em volta do buraco (mais alta junto da parede e no pé),
        // aberta para o lado do quarto.
        // Espaçamento, raio e tamanho irregulares para não virar uma "boia".
        const int partes = 8;
        for (int i = 0; i < partes; i++)
        {
            float a = (Mathf.Lerp(-105f, 95f, i / (float)(partes - 1)) + Jit(12f)) * Mathf.Deg2Rad;
            float r = 1f + Jit(0.18f);
            float lx = -Mathf.Cos(a) * W * 0.33f * r;
            float lz = zBuraco + Mathf.Sin(a) * L * 0.29f * r;
            float alto = (Mathf.Abs(Mathf.Sin(a)) > 0.7f ? 0.26f : 0.19f) + Jit(0.05f);
            Peca("Dobra_" + i, PrimitiveType.Sphere, lx, yColchao + alto * 0.4f, lz,
                new Vector3(0.38f + Jit(0.14f), alto, 0.36f + Jit(0.14f)), new Vector3(Jit(15f), Ang(), Jit(15f)), matLilas);
        }
        // Volumes maiores e achatados por cima, emendando as dobras como um pano só.
        Peca("Volume_Parede", PrimitiveType.Sphere, -W * 0.3f, yColchao + 0.1f, zBuraco + Jit(0.1f), new Vector3(0.55f, 0.16f, L * 0.55f), new Vector3(0f, Jit(8f), 6f), matLilas);
        Peca("Volume_Pe", PrimitiveType.Sphere, W * 0.05f, yColchao + 0.08f, zBuraco - L * 0.3f, new Vector3(W * 0.7f, 0.14f, 0.45f), new Vector3(Jit(6f), 15f, 0f), matLilas);
        // Aba do edredom puxada por cima de parte do buraco.
        Peca("Aba_Por_Cima", PrimitiveType.Sphere, -W * 0.1f, yColchao + 0.15f, zBuraco + L * 0.14f, new Vector3(0.55f, 0.1f, 0.5f), new Vector3(0f, 30f, 10f), matLilas);
        // A ponta escorrendo pelo lado do quarto até o chão, em dobras.
        for (int i = 0; i < 3; i++)
        {
            float t = i / 2f;
            Peca("Escorrendo_" + i, PrimitiveType.Sphere, W * 0.5f + 0.04f + t * 0.1f, Mathf.Lerp(yColchao - 0.02f, 0.06f, t), zBuraco - 0.1f + Jit(0.05f),
                new Vector3(0.14f, 0.26f, 0.75f - t * 0.1f), new Vector3(0f, Jit(6f), 0f), matLilas);
        }
        Peca("No_Chao", PrimitiveType.Sphere, W * 0.5f + 0.32f, 0.03f, zBuraco - 0.15f, new Vector3(0.5f, 0.07f, 0.9f), new Vector3(0f, 12f, 0f), matLilas);

        // Travesseiro amassado e torto na cabeceira; o outro caiu no chão.
        Vector3 p = Mundo(-W * 0.05f, 0f, L * 0.37f);
        // (escala pela altura: com a rotação, a largura medida em X/Z vira a profundidade)
        Kenney("pillowBlue", p, f.eulerAngles.y + 18f, 10f, 0.13f, "y", yColchao + 0.02f);
        p = Mundo(W * 0.5f + 0.35f, 0f, L * 0.28f);
        Kenney("pillow", p, f.eulerAngles.y + 70f, 0f, 0.11f, "y", 0f);

        // Dentro do ninho: urso de pelúcia, moletom, lenços, o roteiro da apresentação e uma caneca.
        p = Mundo(-W * 0.22f, 0f, L * 0.16f);
        Kenney("bear", p, f.eulerAngles.y + 200f, 0f, 0.2f, "y", yColchao + 0.02f);
        Camiseta("Ninho_Moletom", Mundo(-W * 0.05f, yColchao + 0.012f, zBuraco - L * 0.14f), f.eulerAngles.y + 140f, roupas[3], true);
        for (int i = 0; i < 4; i++)
            Lenco("Ninho_Lenco_" + i, Mundo(Jit(W * 0.22f), yColchao + 0.012f, zBuraco + Jit(L * 0.18f)));
        Folha("Ninho_Roteiro", Mundo(W * 0.16f, yColchao + 0.014f, zBuraco + L * 0.06f), Ang());
        Caneca("Ninho_Caneca", Mundo(W * 0.42f, yColchao + 0.01f, -L * 0.25f), Ang());

        // O celular (Puzzle 3) no fundo do buraco, com a tela acesa.
        GameObject celular = GameObject.Find("Celular_Firewall");
        if (celular != null)
        {
            celular.transform.position = Mundo(0.05f, yColchao + 0.03f, zBuraco + 0.02f);
            celular.transform.rotation = Quaternion.Euler(0f, f.eulerAngles.y - 20f, 0f);
            Transform luzT = celular.transform.Find("Luz_Celular");
            if (luzT == null)
            {
                luzT = new GameObject("Luz_Celular").transform;
                luzT.SetParent(celular.transform, false);
            }
            luzT.position = celular.transform.position + Vector3.up * 0.3f;
            Light l = luzT.GetComponent<Light>();
            if (l == null) l = luzT.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.55f, 0.80f, 1f);
            l.intensity = 0.55f;
            l.range = 1.5f;
            l.shadows = LightShadows.None;
            EditorUtility.SetDirty(celular);
        }
    }
}
