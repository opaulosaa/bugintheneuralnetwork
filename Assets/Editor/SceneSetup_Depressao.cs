using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= QUARTO DE QUEM ESTÁ EM CRISE =======================
// Camada extra de bagunça por cima do "Quarto da Referência": o acúmulo de dias sem energia.
// A bagunça se concentra onde a Ana passa o tempo (cama e escrivaninha) e vai rareando no resto.
// Tudo fica dentro de "Bagunca_Depressao", sem colisores. Rodar de novo apaga e recria.
public static partial class SceneSetup
{
    static Material matPrato, matSacoLixo, matPalha, matSola, matAgua, matTampaAgua, matMochila, matCabo, matLilas;
    static Vector2 anaXZ;

    [MenuItem("Bug In The Neural Network/Povoar Quarto - 1. Acumulo")]
    public static void PovoarQuartoAcumulo()
    {
        ApagarSeExistir("Bagunca_Depressao");
        rngBagunca = new System.Random(23);
        raizBagunca = new GameObject("Bagunca_Depressao").transform;
        InicializarMateriaisComida();

        matPrato = MatURP("Dep_Prato", new Color(0.90f, 0.90f, 0.88f), false);
        matSacoLixo = MatURP("Dep_Saco_Lixo", new Color(0.05f, 0.05f, 0.06f), false);
        matSacoLixo.SetFloat("_Smoothness", 0.75f);
        matPalha = MatURP("Dep_Cesto_Palha", new Color(0.62f, 0.50f, 0.32f), false);
        matSola = MatURP("Dep_Sola", new Color(0.92f, 0.92f, 0.90f), false);
        matAgua = MatURP("Dep_Garrafa_Agua", new Color(0.60f, 0.80f, 0.95f), false);
        matTampaAgua = MatURP("Dep_Tampa_Agua", new Color(0.15f, 0.35f, 0.80f), false);
        matMochila = MatURP("Dep_Mochila", new Color(0.22f, 0.25f, 0.32f), false);
        matCabo = MatURP("Dep_Cabo", new Color(0.95f, 0.95f, 0.95f), false);
        matLilas = MatURP("Ref_Edredom_Lilas", new Color(0.74f, 0.64f, 0.88f), false);

        Material[] roupas = {
            MatURP("Bagunca_Roupa_Marinho", new Color(0.15f, 0.20f, 0.38f), false),
            MatURP("Bagunca_Roupa_Cinza", new Color(0.55f, 0.55f, 0.58f), false),
            MatURP("Bagunca_Roupa_Mostarda", new Color(0.85f, 0.65f, 0.20f), false),
            MatURP("Bagunca_Roupa_Vinho", new Color(0.50f, 0.12f, 0.18f), false),
            MatURP("Bagunca_Roupa_Teal", new Color(0.15f, 0.50f, 0.50f), false),
            MatURP("Bagunca_Roupa_Preto", new Color(0.10f, 0.10f, 0.12f), false),
            MatURP("Bagunca_Roupa_Rosa", new Color(0.85f, 0.55f, 0.62f), false),
            MatURP("Bagunca_Roupa_Branco", new Color(0.88f, 0.88f, 0.85f), false),
            MatURP("Bagunca_Roupa_Jeans", new Color(0.25f, 0.35f, 0.55f), false) };
        Material[] latas = { matVermelho, matVidro, MatURP("Bagunca_Lata_Azul", new Color(0.15f, 0.35f, 0.75f), false) };

        float xi = REF_X - 0.15f, zi = REF_Z - 0.15f, zs = -REF_Z + 0.3f;

        GameObject mesa = GameObject.Find("Mesa_PostIts");
        GameObject mesaVisual = mesa;
        if (mesa != null && mesa.transform.Find("VisualModelo") != null) mesaVisual = mesa.transform.Find("VisualModelo").gameObject;
        GameObject cama = GameObject.Find("bedSingle");
        Bounds bMesa, bCama;
        if (!TentarBounds(mesaVisual, out bMesa)) bMesa = new Bounds(new Vector3(-xi + 0.45f, 0.39f, -0.9f), new Vector3(0.9f, 0.78f, 1.6f));
        if (!TentarBounds(cama, out bCama)) bCama = new Bounds(new Vector3(xi - 0.5f, 0.3f, zi - 1.05f), new Vector3(1f, 0.6f, 2.1f));

        GameObject ana = GameObject.Find("Faisca");
        anaXZ = ana != null ? new Vector2(ana.transform.position.x, ana.transform.position.z) : new Vector2(0.3f, -2f);

        // Colisores temporários nos móveis: servem para apoiar coisas em cima e para não largar nada "dentro" deles.
        try
        {
            PrepararSuperficie(mesaVisual);
            PrepararSuperficie(cama);
            foreach (string n in new[] { "chairDesk", "bookcaseOpen", "Arara_Roupas", "Edredom" })
                PrepararSuperficie(GameObject.Find(n));

            // ---------- Cama: o "ninho" (SceneSetup_Ninho.cs) ----------
            MontarNinho(bCama, roupas);

            // Chão ao lado da cama: tudo o que dá para alcançar sem levantar.
            // A cama pode estar no comprido em Z (cabeceira no fundo) ou em X (cabeceira na parede leste).
            bool camaEmX = bCama.size.x > bCama.size.z;
            float bx0, bx1, bz0, bz1;
            if (camaEmX) { bx0 = bCama.min.x + 0.1f; bx1 = bCama.max.x - 0.3f; bz0 = bCama.min.z - 0.75f; bz1 = bCama.min.z - 0.08f; }
            else { bx0 = bCama.min.x - 0.75f; bx1 = bCama.min.x - 0.08f; bz0 = bCama.min.z - 0.3f; bz1 = bCama.max.z - 0.3f; }
            for (int i = 0; i < 4; i++) { Vector3 p; if (PontoLivre(bx0, bx1, bz0, bz1, out p)) Caneca("CamaChao_Caneca_" + i, p, Ang()); }
            for (int i = 0; i < 2; i++) { Vector3 p; if (PontoLivre(bx0, bx1, bz0, bz1, out p)) GarrafaAgua("CamaChao_Agua_" + i, p, Ang(), i == 0); }
            for (int i = 0; i < 12; i++) { Vector3 p; if (PontoLivre(bx0 - 0.3f, bx1, bz0 - 0.4f, bz1, out p)) Lenco("CamaChao_Lenco_" + i, p); }
            for (int i = 0; i < 3; i++) { Vector3 p; if (PontoLivre(bx0, bx1, bz0, bz1, out p)) Lata("CamaChao_Lata_" + i, p, Ang(), latas[i % latas.Length], i != 1); }
            { Vector3 p; if (PontoLivre(bx0, bx1, bz0, bz1, out p)) PilhaPratos("CamaChao_Pratos", p, 3); }
            { Vector3 p; if (PontoLivre(bx0, bx1, bz0, bz1, out p)) Prato("CamaChao_Prato", p, Ang(), true); }
            { Vector3 p; if (PontoLivre(bx0 - 0.5f, bx1, bz0 - 0.6f, bz0, out p)) MonteDeRoupa("CamaChao_Monte", p, roupas); }

            if (camaEmX)
            {
                CaboCarregador("Cabo_Carregador", new Vector3(bCama.max.x - 0.5f, 0f, bCama.min.z - 0.05f), new Vector3(bCama.max.x - 1.3f, 0f, bCama.min.z - 0.85f));
                // Pé da cama virado para o meio do quarto: tênis largados ali; o cesto vai para o canto que a cama liberou.
                Cesto("Cesto_Roupa_Suja", new Vector3(xi - 0.4f, 0f, Mathf.Min(bCama.max.z + 0.55f, zi - 0.4f)), roupas);
                Tenis("Tenis_E", new Vector3(bCama.min.x - 0.45f, 0f, bCama.center.z - 0.2f), 35f, roupas[3]);
                Tenis("Tenis_D", new Vector3(bCama.min.x - 0.75f, 0f, bCama.center.z + 0.15f), 130f, roupas[3]);
                SacoLixo("SacoLixo_Cama", new Vector3(xi - 0.35f, 0f, Mathf.Max(bCama.min.z - 1.5f, zs + 0.2f)), 1f);
            }
            else
            {
                CaboCarregador("Cabo_Carregador", new Vector3(bCama.min.x - 0.05f, 0f, bCama.max.z - 0.5f), new Vector3(bCama.min.x - 0.9f, 0f, bz0));
                // Pé da cama: cesto transbordando e tênis largados.
                Cesto("Cesto_Roupa_Suja", new Vector3(Mathf.Min(bCama.max.x - 0.3f, xi - 0.35f), 0f, bCama.min.z - 0.45f), roupas);
                Tenis("Tenis_E", new Vector3(bCama.center.x - 0.4f, 0f, bCama.min.z - 1.05f), 35f, roupas[3]);
                Tenis("Tenis_D", new Vector3(bCama.center.x - 0.1f, 0f, bCama.min.z - 1.3f), 130f, roupas[3]);
                SacoLixo("SacoLixo_Cama", new Vector3(xi - 0.35f, 0f, Mathf.Max(bCama.min.z - 2.0f, zs + 0.2f)), 1f);
            }

            // ---------- Escrivaninha: dias de "vou fazer o trabalho daqui a pouco" ----------
            float[,] canecas = { { 0.2f, 0.25f }, { 0.28f, 0.34f }, { 0.15f, 0.4f } };
            for (int i = 0; i < canecas.GetLength(0); i++)
                Caneca("Mesa_Caneca_Acumulada_" + i, NaSuperficie(bMesa, canecas[i, 0], canecas[i, 1]), Ang());
            float[,] latasMesa = { { 0.4f, 0.7f }, { 0.34f, 0.77f }, { 0.46f, 0.8f }, { 0.3f, 0.66f } };
            for (int i = 0; i < latasMesa.GetLength(0); i++)
                Lata("Mesa_Lata_" + i, NaSuperficie(bMesa, latasMesa[i, 0], latasMesa[i, 1]), Ang(), latas[i % latas.Length], false);
            Prato("Mesa_Prato", NaSuperficie(bMesa, 0.85f, 0.62f), Ang(), true);
            float[,] folhas = { { 0.75f, 0.2f }, { 0.82f, 0.3f }, { 0.7f, 0.95f }, { 0.88f, 0.08f } };
            for (int i = 0; i < folhas.GetLength(0); i++)
                Folha("Mesa_Folha_" + i, NaSuperficie(bMesa, folhas[i, 0], folhas[i, 1]), Ang());

            // Chão em volta da mesa e da cadeira.
            float mx0 = bMesa.max.x + 0.05f, mx1 = bMesa.max.x + 1.3f;
            float mz0 = Mathf.Max(bMesa.min.z - 0.7f, zs), mz1 = bMesa.max.z + 0.5f;
            for (int i = 0; i < 8; i++) { Vector3 p; if (PontoLivre(mx0, mx1, mz0, mz1, out p)) Folha("MesaChao_Folha_" + i, p, Ang()); }
            for (int i = 0; i < 6; i++) { Vector3 p; if (PontoLivre(mx0, mx1, mz0, mz1, out p)) PapelAmassado("MesaChao_Papel_" + i, p); }
            for (int i = 0; i < 4; i++) { Vector3 p; if (PontoLivre(mx0, mx1, mz0, mz1, out p)) Lata("MesaChao_Lata_" + i, p, Ang(), latas[i % latas.Length], true); }
            { Vector3 p; if (PontoLivre(mx0, mx1, mz0, mz1, out p)) Salgadinho("MesaChao_Salgadinho", p, Ang()); }
            { Vector3 p; if (PontoLivre(mx0, mx1, mz0, mz1, out p)) Prato("MesaChao_Prato", p, Ang(), true); }
            Mochila("Mochila_Aberta", new Vector3(bMesa.max.x + 0.55f, 0f, Mathf.Max(bMesa.min.z - 0.45f, zs + 0.1f)), 60f);
            SacoLixo("SacoLixo_Mesa_1", new Vector3(-xi + 0.4f, 0f, Mathf.Max(bMesa.min.z - 1.7f, zs + 0.15f)), 1.1f);
            SacoLixo("SacoLixo_Mesa_2", new Vector3(-xi + 0.85f, 0f, Mathf.Max(bMesa.min.z - 1.85f, zs + 0.05f)), 0.85f);

            // ---------- Meio do quarto: o caminho cama ↔ mesa, onde as coisas vão caindo ----------
            float cx0 = bMesa.max.x + 1.0f, cx1 = bCama.min.x - 0.9f, cz0 = zs, cz1 = zi - 0.9f;
            for (int i = 0; i < 3; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) MonteDeRoupa("Meio_Monte_" + i, p, roupas); }
            for (int i = 0; i < 3; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Camiseta("Meio_Camiseta_" + i, p, Ang(), roupas[rngBagunca.Next(roupas.Length)], i == 1); }
            for (int i = 0; i < 2; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Calca("Meio_Calca_" + i, p, Ang(), roupas[i == 0 ? 8 : 5]); }
            for (int i = 0; i < 4; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Meia("Meio_Meia_" + i, p, Ang(), roupas[rngBagunca.Next(roupas.Length)]); }
            { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Toalha("Meio_Toalha", p, Ang(), roupas[1]); }
            { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) CaixaPizza("Meio_Pizza_Aberta", p, Ang(), true, 1); }
            { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) CaixaPizza("Meio_Pizzas", p, Ang(), false, 2); }
            for (int i = 0; i < 2; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Kenney("books", p, Ang(), 90f, 0.2f, "y", 0f); }
            for (int i = 0; i < 2; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Prato("Meio_Prato_" + i, p, Ang(), true); }
            { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Marmita("Meio_Marmita", p, Ang()); }
            { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) CopoMiojo("Meio_Miojo", p, Ang(), true); }
            for (int i = 0; i < 6; i++) { Vector3 p; if (PontoLivre(cx0, cx1, cz0, cz1, out p)) Folha("Meio_Folha_" + i, p, Ang()); }

            // ---------- Resto do quarto: rastro espalhado ----------
            for (int i = 0; i < 50; i++)
            {
                Vector3 p;
                if (!PontoLivre(-xi + 0.25f, xi - 0.25f, zs, zi - 0.3f, out p)) continue;
                switch (rngBagunca.Next(6))
                {
                    case 0: Lenco("Rastro_Lenco_" + i, p); break;
                    case 1: PapelAmassado("Rastro_Papel_" + i, p); break;
                    case 2: Meia("Rastro_Meia_" + i, p, Ang(), roupas[rngBagunca.Next(roupas.Length)]); break;
                    case 3: Lata("Rastro_Lata_" + i, p, Ang(), latas[rngBagunca.Next(latas.Length)], true); break;
                    case 4: Folha("Rastro_Folha_" + i, p, Ang()); break;
                    default: Embalagem("Rastro_Embalagem_" + i, p, Ang()); break;
                }
            }
            Tenis("Chinelo_E", new Vector3(-0.9f, 0f, zs + 0.3f), -20f, roupas[0]);
            Tenis("Chinelo_D", new Vector3(-0.55f, 0f, zs + 0.15f), 75f, roupas[0]);
            CaixaPizza("Pizzas_Perto_Porta", new Vector3(1.4f, 0f, zs + 0.25f), 25f, false, 2);
        }
        finally
        {
            LimparSuperficies();
        }

        // Tudo o que está jogado perde a cor: roupas e embalagens desbotadas, nada de tom alegre.
        Desbotar(raizBagunca);
        GameObject antiga = GameObject.Find("Bagunca_Quarto");
        if (antiga != null) Desbotar(antiga.transform);
        foreach (string n in new[] { "Edredom", "Edredom_Aba" })   // senão o edredom da cama destoa das partes emboladas
        {
            GameObject e = GameObject.Find(n);
            if (e != null) Desbotar(e.transform);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Quarto povoado (" + raizBagunca.childCount + " itens em 'Bagunca_Depressao'). Salve a cena (Ctrl+S).");
    }

    // Sorteia um ponto do chão no retângulo que não esteja embaixo de móvel nem em cima da Ana.
    static bool PontoLivre(float x0, float x1, float z0, float z1, out Vector3 p)
    {
        for (int t = 0; t < 30; t++)
        {
            float x = Mathf.Clamp(Mathf.Lerp(x0, x1, (float)rngBagunca.NextDouble()), -REF_X + 0.35f, REF_X - 0.35f);
            float z = Mathf.Clamp(Mathf.Lerp(z0, z1, (float)rngBagunca.NextDouble()), -REF_Z + 0.25f, REF_Z - 0.35f);
            if (AlturaEm(x, z, 0f) > 0.04f) continue;
            if (Vector2.Distance(new Vector2(x, z), anaXZ) < 0.6f) continue;
            p = new Vector3(x, 0f, z);
            return true;
        }
        p = Vector3.zero;
        return false;
    }

    // Troca cada material por uma versão desbotada (menos saturada e um pouco mais escura),
    // guardada em Assets/Materials/Apagados. Materiais já desbotados ficam como estão.
    const float DESBOTAR_SATURACAO = 0.6f;   // 0 = cor original, 1 = cinza
    const float DESBOTAR_ESCURECER = 0.82f;

    static void Desbotar(Transform raiz)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Apagados"))
            AssetDatabase.CreateFolder("Assets/Materials", "Apagados");
        var cache = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || m.name.StartsWith("Apagado_")) continue;
                Material novo;
                if (!cache.TryGetValue(m, out novo))
                {
                    novo = MaterialDesbotado(m);
                    cache[m] = novo;
                }
                mats[i] = novo;
            }
            r.sharedMaterials = mats;
        }
    }

    static Material MaterialDesbotado(Material m)
    {
        string caminho = "Assets/Materials/Apagados/Apagado_" + m.name + ".mat";
        Material novo = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (novo == null)
        {
            novo = new Material(m);
            AssetDatabase.CreateAsset(novo, caminho);
        }
        else novo.CopyPropertiesFromMaterial(m);
        novo.name = "Apagado_" + m.name;

        Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
        float cinza = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        Color d = Color.Lerp(c, new Color(cinza, cinza, cinza, c.a), DESBOTAR_SATURACAO) * DESBOTAR_ESCURECER;
        d.a = c.a;
        if (novo.HasProperty("_BaseColor")) novo.SetColor("_BaseColor", d);
        novo.color = d;
        if (novo.HasProperty("_Smoothness")) novo.SetFloat("_Smoothness", Mathf.Min(novo.GetFloat("_Smoothness"), 0.35f));
        EditorUtility.SetDirty(novo);
        return novo;
    }

    // ----- Itens novos -----

    static void Prato(string nome, Vector3 pos, float rotY, bool sujo)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Bloco(g, "Prato", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0f), new Vector3(0.24f, 0.008f, 0.24f), matPrato);
        Bloco(g, "Aba", PrimitiveType.Cylinder, new Vector3(0f, 0.014f, 0f), new Vector3(0.17f, 0.003f, 0.17f), matCaneca);
        if (!sujo) return;
        Bloco(g, "Resto_1", PrimitiveType.Sphere, new Vector3(0.03f, 0.018f, -0.02f), new Vector3(0.08f, 0.015f, 0.06f), matFeijao);
        Bloco(g, "Resto_2", PrimitiveType.Sphere, new Vector3(-0.04f, 0.018f, 0.03f), new Vector3(0.05f, 0.012f, 0.05f), matQueijo);
        BlocoR(g, "Garfo", PrimitiveType.Cube, new Vector3(0.02f, 0.02f, 0.05f), new Vector3(0.015f, 0.004f, 0.17f), new Vector3(0f, 35f, 0f), matAluminio);
    }

    static void PilhaPratos(string nome, Vector3 pos, int n)
    {
        Transform g = Grupo(nome, pos, Ang(), 0f).transform;
        for (int i = 0; i < n; i++)
        {
            Transform t = Filho(g, "Prato_" + i, new Vector3(Jit(0.02f), i * 0.018f, Jit(0.02f)), new Vector3(Jit(3f), 0f, Jit(3f)));
            Bloco(t, "Prato", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0f), new Vector3(0.24f, 0.008f, 0.24f), matPrato);
            Bloco(t, "Resto", PrimitiveType.Sphere, new Vector3(Jit(0.05f), 0.016f, Jit(0.05f)), new Vector3(0.06f, 0.008f, 0.05f), matFeijao);
        }
        BlocoR(g, "Talheres", PrimitiveType.Cube, new Vector3(0f, n * 0.018f + 0.004f, 0f), new Vector3(0.015f, 0.004f, 0.18f), new Vector3(0f, 60f, 0f), matAluminio);
    }

    static void SacoLixo(string nome, Vector3 pos, float escala)
    {
        Transform g = Grupo(nome, pos, Ang(), 4f).transform;
        g.localScale = Vector3.one * escala;
        // Corpo largo e caroçudo (não pode parecer uma bola), boca franzida e as duas orelhas do nó.
        Bloco(g, "Saco", PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0f), new Vector3(0.6f, 0.34f, 0.5f), matSacoLixo);
        Bloco(g, "Volume_1", PrimitiveType.Sphere, new Vector3(0.14f, 0.2f, 0.08f), new Vector3(0.3f, 0.3f, 0.3f), matSacoLixo);
        Bloco(g, "Volume_2", PrimitiveType.Sphere, new Vector3(-0.12f, 0.15f, -0.1f), new Vector3(0.32f, 0.26f, 0.3f), matSacoLixo);
        BlocoR(g, "Boca", PrimitiveType.Cylinder, new Vector3(0f, 0.36f, 0f), new Vector3(0.12f, 0.06f, 0.12f), new Vector3(0f, 0f, 8f), matSacoLixo);
        BlocoR(g, "Orelha_E", PrimitiveType.Cube, new Vector3(-0.05f, 0.43f, 0f), new Vector3(0.1f, 0.09f, 0.015f), new Vector3(0f, 0f, 35f), matSacoLixo);
        BlocoR(g, "Orelha_D", PrimitiveType.Cube, new Vector3(0.05f, 0.43f, 0f), new Vector3(0.1f, 0.09f, 0.015f), new Vector3(0f, 0f, -35f), matSacoLixo);
        BlocoR(g, "Ponta_Papel", PrimitiveType.Cube, new Vector3(0.26f, 0.22f, 0.12f), new Vector3(0.1f, 0.12f, 0.003f), new Vector3(0f, 40f, 15f), matPapel);
    }

    static void Tenis(string nome, Vector3 pos, float rotY, Material cor)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        bool deLado = rngBagunca.NextDouble() < 0.35;
        Transform t = deLado ? Filho(g, "Tenis", new Vector3(0f, 0.045f, 0f), new Vector3(0f, 0f, 90f)) : Filho(g, "Tenis", Vector3.zero, Vector3.zero);
        Bloco(t, "Sola", PrimitiveType.Cube, new Vector3(0f, 0.012f, 0f), new Vector3(0.1f, 0.024f, 0.27f), matSola);
        Bloco(t, "Corpo", PrimitiveType.Cube, new Vector3(0f, 0.045f, -0.02f), new Vector3(0.09f, 0.045f, 0.22f), cor);
        Bloco(t, "Bico", PrimitiveType.Sphere, new Vector3(0f, 0.035f, 0.1f), new Vector3(0.09f, 0.04f, 0.08f), cor);
        Bloco(t, "Cano", PrimitiveType.Cube, new Vector3(0f, 0.075f, -0.09f), new Vector3(0.088f, 0.04f, 0.07f), cor);
    }

    static void Cesto(string nome, Vector3 pos, Material[] cores)
    {
        Transform g = Grupo(nome, pos, Ang(), 0f).transform;
        Bloco(g, "Cesto", PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0f), new Vector3(0.45f, 0.27f, 0.45f), matPalha);
        Bloco(g, "Borda", PrimitiveType.Cylinder, new Vector3(0f, 0.54f, 0f), new Vector3(0.48f, 0.015f, 0.48f), matKraftEsc);
        for (int i = 0; i < 6; i++)
            BlocoR(g, "Roupa_" + i, PrimitiveType.Sphere, new Vector3(Jit(0.12f), 0.56f + i * 0.03f, Jit(0.12f)),
                new Vector3(0.3f, 0.12f, 0.24f), new Vector3(Jit(15f), Ang(), Jit(15f)), cores[rngBagunca.Next(cores.Length)]);
        // Uma manga pendurada pela borda e uma peça que já caiu.
        BlocoR(g, "Manga_Pendurada", PrimitiveType.Cube, new Vector3(0.24f, 0.42f, 0f), new Vector3(0.03f, 0.3f, 0.12f), new Vector3(0f, 0f, 8f), cores[rngBagunca.Next(cores.Length)]);
        BlocoR(g, "Caiu", PrimitiveType.Sphere, new Vector3(0.45f, 0.04f, 0.1f), new Vector3(0.4f, 0.08f, 0.3f), new Vector3(0f, Ang(), 0f), cores[rngBagunca.Next(cores.Length)]);
    }

    // Mochila tombada com o material da apresentação escorregando para fora.
    static void Mochila(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Transform m = Filho(g, "Mochila", new Vector3(0f, 0.11f, 0f), new Vector3(78f, 0f, 0f));
        Bloco(m, "Corpo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.32f, 0.42f, 0.2f), matMochila);
        Bloco(m, "Bolso", PrimitiveType.Cube, new Vector3(0f, -0.08f, 0.11f), new Vector3(0.26f, 0.2f, 0.05f), matMochila);
        Bloco(m, "Ziper", PrimitiveType.Cube, new Vector3(0f, 0.21f, 0f), new Vector3(0.3f, 0.01f, 0.205f), matAmarelo);
        BlocoR(m, "Alca", PrimitiveType.Cube, new Vector3(0.12f, 0f, -0.12f), new Vector3(0.04f, 0.38f, 0.02f), new Vector3(0f, 0f, 6f), matSombra);
        for (int i = 0; i < 4; i++)
        {
            Transform f = Filho(g, "Folha_" + i, new Vector3(Jit(0.06f), 0.003f + i * 0.002f, 0.28f + i * 0.09f), new Vector3(0f, Jit(25f), 0f));
            Bloco(f, "Papel", PrimitiveType.Cube, Vector3.zero, new Vector3(0.21f, 0.002f, 0.297f), matPapel);
            Bloco(f, "Linhas", PrimitiveType.Cube, new Vector3(0f, 0.0015f, 0.02f), new Vector3(0.15f, 0.001f, 0.16f), matCaneca);
        }
    }

    static void Folha(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 2f).transform;
        Bloco(g, "Papel", PrimitiveType.Cube, new Vector3(0f, 0.002f, 0f), new Vector3(0.21f, 0.002f, 0.297f), matPapel);
        Bloco(g, "Linhas", PrimitiveType.Cube, new Vector3(0f, 0.0035f, 0.03f), new Vector3(0.15f, 0.001f, 0.17f), matCaneca);
        if (rngBagunca.NextDouble() < 0.4)
            Bloco(g, "Rabisco", PrimitiveType.Cube, new Vector3(0.03f, 0.0038f, -0.08f), new Vector3(0.09f, 0.001f, 0.012f), matVermelho);
    }

    static void Lenco(string nome, Vector3 pos)
    {
        float s = 0.045f + Jit(0.01f);
        BlocoR(raizBagunca, nome, PrimitiveType.Sphere, pos + Vector3.up * s * 0.4f,
            new Vector3(s + Jit(0.01f), s * 0.8f, s + Jit(0.01f)), new Vector3(Ang(), Ang(), Ang()), matBranco);
    }

    static void Embalagem(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 3f).transform;
        Material cor = rngBagunca.NextDouble() < 0.5 ? matVermelho : matAmarelo;
        BlocoR(g, "Embalagem", PrimitiveType.Cube, new Vector3(0f, 0.006f, 0f), new Vector3(0.12f, 0.012f, 0.07f), new Vector3(0f, 0f, Jit(10f)), cor);
        Bloco(g, "Ponta", PrimitiveType.Cube, new Vector3(0.065f, 0.006f, 0f), new Vector3(0.02f, 0.008f, 0.075f), matAluminio);
    }

    static void GarrafaAgua(string nome, Vector3 pos, float rotY, bool caida)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Transform c = caida ? Filho(g, "Garrafa", new Vector3(0f, 0.035f, 0f), new Vector3(0f, 0f, 90f))
                            : Filho(g, "Garrafa", new Vector3(0f, 0.11f, 0f), Vector3.zero);
        Bloco(c, "Corpo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.07f, 0.11f, 0.07f), matAgua);
        Bloco(c, "Rotulo", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.072f, 0.03f, 0.072f), matTampaAgua);
        Bloco(c, "Tampa", PrimitiveType.Cylinder, new Vector3(0f, 0.125f, 0f), new Vector3(0.03f, 0.015f, 0.03f), matTampaAgua);
    }

    // Fio do carregador saindo da cama e serpenteando pelo chão.
    static void CaboCarregador(string nome, Vector3 de, Vector3 ate)
    {
        Transform g = Filho(raizBagunca, nome, Vector3.zero, Vector3.zero);
        const int n = 10;
        Vector3 ant = de;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(de, ate, t) + new Vector3(Mathf.Sin(t * 9f) * 0.12f, 0f, Mathf.Cos(t * 7f) * 0.08f);
            p.y = 0.006f;
            Vector3 meio = (ant + p) * 0.5f; meio.y = 0.006f;
            Vector3 d = p - ant; d.y = 0f;
            GameObject s = Bloco(g, "Trecho_" + i, PrimitiveType.Cube, meio, new Vector3(0.01f, 0.008f, d.magnitude + 0.01f), matCabo);
            s.transform.rotation = Quaternion.LookRotation(d.sqrMagnitude > 0.0001f ? d : Vector3.forward);
            ant = p;
        }
        Bloco(g, "Plug", PrimitiveType.Cube, new Vector3(ant.x, 0.012f, ant.z), new Vector3(0.03f, 0.02f, 0.04f), matCabo);
    }
}
