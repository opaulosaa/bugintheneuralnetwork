using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= VERSÃO CRIS =======================
// Mudanças de 19/11/2026:
//  1. A protagonista vira Cris (nome neutro) e os textos deixam de marcar gênero.
//  2. O personagem vira um boneco de manequim, sem rosto, cabelo ou roupa marcada.
//  3. Sem apagão: nada escurece. Ficar parado faz o coração acelerar (GameManager).
//  4. A cada puzzle resolvido uma parte do quarto se arruma sozinha (ArrumacaoQuarto).
//  5. A história se passa no fim da tarde, não de madrugada.
// Cada item do menu pode ser rodado de novo sem duplicar nada. Depois, salve a cena (Ctrl+S).
public static partial class SceneSetup
{
    const string PERFIL_TARDE = "Assets/Settings/TardeProfile.asset";
    const string HORA_TARDE = "16:40";

    [MenuItem("Bug In The Neural Network/Versão Cris/Aplicar Tudo", false, 0)]
    public static void AplicarVersaoCris()
    {
        CriarBonecoCris();
        AplicarTarde();
        TextosNeutrosCris();
        MontarArrumacaoEBatimentos();
        EstilizarUIPixel();
        Debug.Log("Versão Cris aplicada. Salve a cena (Ctrl+S) e teste no Play.");
    }

    // ---------------------------------------------------------------------
    [MenuItem("Bug In The Neural Network/Versão Cris/1. Personagem Cris (boneco neutro)", false, 11)]
    public static void CriarBonecoCris()
    {
        GameObject raiz = GameObject.Find("Faisca");
        if (raiz == null) { Debug.LogError("Objeto 'Faisca' (jogador) não encontrado."); return; }

        var esfera = raiz.GetComponent<MeshRenderer>();
        if (esfera != null) esfera.enabled = false;

        // Tamanho de gente: 1,70 m, proporções normais (cabeça ~1/7 da altura), sem exageros.
        // A raiz tem escala 0,6 (herança da Faísca): tudo abaixo compensa isso.
        const float ALTURA = 1.70f, ESC = 0.6f;
        var cc = raiz.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.radius = 0.24f / ESC;
            cc.height = ALTURA / ESC;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f / ESC;
        }
        Vector3 pos = raiz.transform.position; pos.y = ALTURA * 0.5f + 0.02f; raiz.transform.position = pos;

        foreach (string n in new[] { "Ana_Visual", "Cris_Visual" })
        {
            Transform antigo = raiz.transform.Find(n);
            if (antigo != null) Object.DestroyImmediate(antigo.gameObject);
        }

        // Manequim neutro: uma cor só, juntas discretas, cabeça sem rosto.
        Material corpoMat = MatURP("Cris_Boneco", new Color(0.80f, 0.74f, 0.66f), false);
        Material juntaMat = MatURP("Cris_Juntas", new Color(0.70f, 0.64f, 0.56f), false);
        Material auraMat = MatURP("Cris_Aura", new Color(0.55f, 0.75f, 1f), true);
        corpoMat.SetFloat("_Smoothness", 0.35f);
        juntaMat.SetFloat("_Smoothness", 0.35f);

        GameObject visual = new GameObject("Cris_Visual");
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localScale = Vector3.one / ESC;
        visual.transform.localPosition = new Vector3(0f, -(ALTURA * 0.5f) / ESC, 0f);   // origem nos pés
        Transform v = visual.transform;

        GameObject corpo = new GameObject("Corpo");
        corpo.transform.SetParent(v, false);
        Transform c = corpo.transform;
        // Medidas em metros a partir do chão.
        Bloco(c, "Quadril", PrimitiveType.Sphere, new Vector3(0f, 0.93f, 0f), new Vector3(0.32f, 0.17f, 0.20f), juntaMat);
        Bloco(c, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.18f, 0f), new Vector3(0.34f, 0.25f, 0.21f), corpoMat);
        Bloco(c, "Pescoco", PrimitiveType.Cylinder, new Vector3(0f, 1.46f, 0f), new Vector3(0.09f, 0.04f, 0.09f), juntaMat);
        Bloco(c, "Cabeca", PrimitiveType.Sphere, new Vector3(0f, 1.585f, 0.01f), new Vector3(0.19f, 0.23f, 0.21f), corpoMat);
        Bloco(c, "Ombro_E", PrimitiveType.Sphere, new Vector3(-0.19f, 1.38f, 0f), Vector3.one * 0.10f, juntaMat);
        Bloco(c, "Ombro_D", PrimitiveType.Sphere, new Vector3(0.19f, 1.38f, 0f), Vector3.one * 0.10f, juntaMat);

        Transform pernaE = CriarMembro(c, "Perna_E", new Vector3(-0.09f, 0.90f, 0f), 0.88f, 0.065f, corpoMat);
        Transform pernaD = CriarMembro(c, "Perna_D", new Vector3(0.09f, 0.90f, 0f), 0.88f, 0.065f, corpoMat);
        Transform bracoE = CriarMembro(c, "Braco_E", new Vector3(-0.21f, 1.37f, 0f), 0.66f, 0.045f, corpoMat);
        Transform bracoD = CriarMembro(c, "Braco_D", new Vector3(0.21f, 1.37f, 0f), 0.66f, 0.045f, corpoMat);
        Bloco(pernaE, "Joelho", PrimitiveType.Sphere, new Vector3(0f, -0.45f, 0f), Vector3.one * 0.105f, juntaMat);
        Bloco(pernaD, "Joelho", PrimitiveType.Sphere, new Vector3(0f, -0.45f, 0f), Vector3.one * 0.105f, juntaMat);
        Bloco(bracoE, "Cotovelo", PrimitiveType.Sphere, new Vector3(0f, -0.31f, 0f), Vector3.one * 0.075f, juntaMat);
        Bloco(bracoD, "Cotovelo", PrimitiveType.Sphere, new Vector3(0f, -0.31f, 0f), Vector3.one * 0.075f, juntaMat);
        Bloco(pernaE, "Pe", PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.10f, 0.06f, 0.24f), juntaMat);
        Bloco(pernaD, "Pe", PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.10f, 0.06f, 0.24f), juntaMat);
        bracoE.localRotation = Quaternion.Euler(0f, 0f, -4f);
        bracoD.localRotation = Quaternion.Euler(0f, 0f, 4f);

        Bloco(v, "Aura", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.9f, 0.004f, 0.9f), auraMat);

        var anim = raiz.GetComponent<CrisMiniatura>();
        if (anim == null) anim = raiz.AddComponent<CrisMiniatura>();
        anim.controller = cc;
        anim.corpo = c;
        anim.pernaEsquerda = pernaE; anim.pernaDireita = pernaD;
        anim.bracoEsquerdo = bracoE; anim.bracoDireito = bracoD;
        // Passo de adulto: menos ciclos por metro, movimentos mais contidos.
        anim.amplitudePernas = 26f;
        anim.amplitudeBracos = 18f;
        anim.passosPorMetro = 1.3f;
        anim.alturaPasso = 0.035f;
        anim.inclinacaoAndando = 4f;
        anim.ginga = 2f;
        anim.respiracao = 0.008f;
        anim.curvaturaSemVontade = 8f;
        EditorUtility.SetDirty(anim);

        var mov = raiz.GetComponent<PlayerMovement>();
        if (mov != null) { mov.velocidade = 2.0f; EditorUtility.SetDirty(mov); }

        Transform luz = raiz.transform.Find("FaiscaLuz");
        if (luz != null) luz.localPosition = new Vector3(0f, (1.25f - ALTURA * 0.5f) / ESC, 0.25f / ESC);

        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null) { gm.aura = v.Find("Aura"); EditorUtility.SetDirty(gm); }

        MarcarCena();
        Debug.Log("Cris: boneco neutro de 1,70 m criado (sem rosto, cabelo ou roupa).");
    }

    // ---------------------------------------------------------------------
    [MenuItem("Bug In The Neural Network/Versão Cris/2. Fim de tarde", false, 12)]
    public static void AplicarTarde()
    {
        // Luz do dia já baixa, entrando fraca porque a cortina está fechada.
        Light sol = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            sol.intensity = 0.75f;
            sol.color = new Color(1f, 0.86f, 0.68f);
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.55f;
            sol.transform.rotation = Quaternion.Euler(48f, 200f, 0f);
            EditorUtility.SetDirty(sol);
        }

        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.corFria = new Color(0.66f, 0.66f, 0.74f);   // dia nublado, sem graça
            gm.corQuente = new Color(1f, 0.86f, 0.66f);    // sol de fim de tarde
            if (gm.luzAmbiente != null) { gm.luzAmbiente.intensity = 0.75f; EditorUtility.SetDirty(gm.luzAmbiente); }
            EditorUtility.SetDirty(gm);
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.40f, 0.41f, 0.46f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.28f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.13f, 0.12f);
        RenderSettings.reflectionIntensity = 0.5f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.11f, 0.12f);
            EditorUtility.SetDirty(cam);
        }

        AjustarLuz("Luz_Monitor", new Color(0.62f, 0.74f, 1f), 1.2f, 3f);
        AjustarLuz("Luz_Luminaria", new Color(1f, 0.78f, 0.52f), 0.5f, 2.2f);
        AjustarLuz("Luz_Janela", new Color(1f, 0.74f, 0.48f), 1.6f, 4.2f);   // sol vazando pela cortina

        // Céu: tarde lá fora; ao abrir a cortina, o pôr do sol.
        var final = Object.FindFirstObjectByType<PuzzleFinal>();
        if (final != null)
        {
            Color[] agora = { new Color(0.52f, 0.68f, 0.90f), new Color(0.98f, 0.82f, 0.60f), new Color(1f, 0.94f, 0.76f) };
            if (final.ceu != null)
                for (int i = 0; i < final.ceu.Length && i < agora.Length; i++)
                {
                    if (final.ceu[i] == null || final.ceu[i].sharedMaterial == null) continue;
                    final.ceu[i].sharedMaterial.SetColor("_BaseColor", agora[i]);
                    final.ceu[i].sharedMaterial.color = agora[i];
                    EditorUtility.SetDirty(final.ceu[i].sharedMaterial);
                }
            final.coresCeuFinal = new[] { new Color(0.98f, 0.66f, 0.40f), new Color(1f, 0.84f, 0.56f), new Color(1f, 0.80f, 0.45f) };
            if (final.estrelas != null)
                foreach (var e in final.estrelas) if (e != null) { e.enabled = false; EditorUtility.SetDirty(e); }
            final.corFinal = new Color(1f, 0.85f, 0.62f);
            final.corLuzJanelaFinal = new Color(1f, 0.76f, 0.45f);
            final.intensidadeLuzFinal = 1.35f;
            final.intensidadeJanelaFinal = 3.5f;
            EditorUtility.SetDirty(final);
        }

        // Relógios e post-it.
        foreach (var tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string novo = tm.text.Replace("03:12", HORA_TARDE).Replace("APRESENTAÇÃO\n9h!!", "APRESENTAÇÃO\namanhã 9h!!");
            if (novo != tm.text) { tm.text = novo; EditorUtility.SetDirty(tm); }
        }
        var menu = Object.FindFirstObjectByType<MenuNeural>(FindObjectsInactive.Include);
        if (menu != null) { menu.horaMenu = HORA_TARDE; EditorUtility.SetDirty(menu); }

        // Pós-processamento: menos vinheta e um pouco menos de cor (o quarto ainda é triste).
        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume != null)
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PERFIL_TARDE);
            if (perfil == null)
            {
                if (volume.sharedProfile != null) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), PERFIL_TARDE);
                perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PERFIL_TARDE);
                if (perfil == null) { perfil = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(perfil, PERFIL_TARDE); }
            }
            Vignette vinheta;
            if (!perfil.TryGet(out vinheta)) vinheta = perfil.Add<Vignette>(true);
            vinheta.active = true;
            vinheta.color.Override(new Color(0.08f, 0.06f, 0.06f));
            vinheta.intensity.Override(0.28f);
            vinheta.smoothness.Override(0.5f);

            ColorAdjustments cores;
            if (!perfil.TryGet(out cores)) cores = perfil.Add<ColorAdjustments>(true);
            cores.active = true;
            cores.saturation.Override(-25f);
            cores.contrast.Override(6f);
            cores.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            Bloom bloom;
            if (!perfil.TryGet(out bloom)) bloom = perfil.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.4f);

            foreach (var comp in perfil.components) EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(perfil);
            volume.sharedProfile = perfil;
            EditorUtility.SetDirty(volume);
        }

        AssetDatabase.SaveAssets();
        MarcarCena();
        Debug.Log("Fim de tarde aplicado (" + HORA_TARDE + ").");
    }

    // ---------------------------------------------------------------------
    [MenuItem("Bug In The Neural Network/Versão Cris/3. Textos neutros (Cris)", false, 13)]
    public static void TextosNeutrosCris()
    {
        var exatos = new Dictionary<string, string>
        {
            { "D: Parar quando eu estiver menos cansada.", "D: Parar quando o cansaço passar." },
            { "D E N T R O   D A   C A B E Ç A   D A   A N A", "D E N T R O   D A   C A B E Ç A   D E   C R I S" },
            { "apresentação às 9h", "apresentação amanhã, 9h" },
        };
        int n = 0;
        foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string s = t.text;
            if (string.IsNullOrEmpty(s)) continue;
            string novo = s;

            if (exatos.ContainsKey(s)) novo = exatos[s];
            else if (s.StartsWith("Faltam poucas horas para a apresentação"))
                novo = TEXTO_PUZZLE1;
            else if (t.name == "Contexto" && s.StartsWith("Cris estuda") || t.name == "Contexto" && s.Contains("miniatura"))
                novo = TEXTO_CONTEXTO;
            else if (t.name == "Texto" && s.StartsWith("Mover"))
            {
                novo = TEXTO_COMO_JOGAR;
                t.resizeTextForBestFit = true;
                t.resizeTextMaxSize = t.fontSize;
                t.resizeTextMinSize = 14;
            }
            else novo = NeutralizarNome(s);

            novo = novo.Replace("03:12", HORA_TARDE);
            if (novo != s) { t.text = novo; EditorUtility.SetDirty(t); n++; }
        }
        foreach (var it in Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string novo = NeutralizarNome(it.textoPrompt);
            if (novo != it.textoPrompt) { it.textoPrompt = novo; EditorUtility.SetDirty(it); n++; }
        }
        var loop = Object.FindFirstObjectByType<PuzzleLoop>(FindObjectsInactive.Include);
        if (loop != null && loop.opcoes != null)
            foreach (var o in loop.opcoes)
            {
                string novo = NeutralizarNome(o.textoFeedback);
                if (novo != o.textoFeedback) { o.textoFeedback = novo; EditorUtility.SetDirty(loop); n++; }
            }

        MarcarCena();
        Debug.Log("Textos neutros: " + n + " textos atualizados para Cris.");
    }

    const string TEXTO_CONTEXTO =
        "Cris estuda Ciência da Computação e tem uma apresentação importante amanhã às 9h. A tarde está passando e nada sai do lugar. " +
        "Você controla Cris: explore o quarto, destrave o que está travado e dê o primeiro passo.";

    const string TEXTO_PUZZLE1 =
        "A apresentação é amanhã às 9h e a tarde está acabando.\nClique nas tarefas na ordem em que Cris deve fazê-las.";

    const string TEXTO_COMO_JOGAR =
        "Mover Cris: W A S D ou as setas.\n" +
        "Interagir: tecla E, perto de um objeto. Esc fecha o painel.\n" +
        "Dentro dos puzzles: clique nas opções com o mouse.\n\n" +
        "Puzzle 1 · Mesa de tarefas: organize as tarefas por prioridade.\n" +
        "Puzzle 2 · Quadro: encontre a condição de parada do loop.\n" +
        "Puzzle 3 · Celular: reconfigure o firewall social.\n" +
        "Cada puzzle resolvido arruma uma parte do quarto. Depois dos três, abra a Cortina.\n\n" +
        "A Vontade é a luz em volta de Cris: sobe quando você avança e desce com erros. " +
        "Se ficar muito tempo parado, o coração acelera. Sem pressa, mas siga em frente.";

    // "a Ana" → "Cris", "da Ana" → "de Cris" etc.: o nome novo vai sem artigo, para não marcar gênero.
    static string NeutralizarNome(string s)
    {
        if (string.IsNullOrEmpty(s) || !s.Contains("Ana")) return s;
        s = Regex.Replace(s, @"\bda Ana\b", "de Cris");
        s = Regex.Replace(s, @"\bna Ana\b", "em Cris");
        s = Regex.Replace(s, @"\bpela Ana\b", "por Cris");
        s = Regex.Replace(s, @"\bà Ana\b", "a Cris");
        s = Regex.Replace(s, @"\b[Aa] Ana\b", "Cris");
        s = Regex.Replace(s, @"\bAna\b", "Cris");
        return s;
    }

    // ---------------------------------------------------------------------
    [MenuItem("Bug In The Neural Network/Versão Cris/4. Arrumação do quarto e batimentos", false, 14)]
    public static void MontarArrumacaoEBatimentos()
    {
        // ---------- GameManager: sem apagão, dica gentil ----------
        var gm = Object.FindFirstObjectByType<GameManager>();
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (gm != null)
        {
            gm.drenaPorErro = 10f;
            gm.drenaPorInerciaPorSegundo = 1.5f;
            gm.segundosParaInercia = 8f;
            if (gm.telaApagao != null) gm.telaApagao.gameObject.SetActive(false);
            Transform apagao = canvasObj != null ? canvasObj.transform.Find("TelaApagao") : null;
            if (apagao != null) apagao.gameObject.SetActive(false);

            if (gm.aura == null && gm.faiscaPlayer != null)
                foreach (Transform t in gm.faiscaPlayer.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Aura") { gm.aura = t; break; }

            if (canvasObj != null)
            {
                Transform velha = canvasObj.transform.Find("TextoDica");
                Text dica = velha != null ? velha.GetComponent<Text>() : null;
                if (dica == null)
                {
                    dica = CriarTexto(canvasObj.transform, "TextoDica", "", 26,
                        new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(1200, 50));
                    dica.alignment = TextAnchor.MiddleCenter;
                    dica.fontStyle = FontStyle.Italic;
                    dica.color = new Color(1f, 0.95f, 0.85f, 0f);
                    var sombra = dica.gameObject.AddComponent<Shadow>();
                    sombra.effectColor = new Color(0f, 0f, 0f, 0.7f);
                    sombra.effectDistance = new Vector2(2f, -2f);
                    // Fica abaixo do menu inicial na ordem de desenho.
                    Transform menu = canvasObj.transform.Find("Painel_Inicial");
                    if (menu != null) dica.transform.SetSiblingIndex(menu.GetSiblingIndex());
                }
                gm.textoDica = dica;
            }
            EditorUtility.SetDirty(gm);
        }

        // ---------- Arrumação ----------
        GameObject obj = GameObject.Find("ArrumacaoQuarto");
        if (obj == null) obj = new GameObject("ArrumacaoQuarto");
        var arr = obj.GetComponent<ArrumacaoQuarto>();
        if (arr == null) arr = obj.AddComponent<ArrumacaoQuarto>();
        arr.grupos = new[]
        {
            new ArrumacaoQuarto.Grupo { nome = "Puzzle 1 · Escrivaninha" },
            new ArrumacaoQuarto.Grupo { nome = "Puzzle 2 · Meio do quarto" },
            new ArrumacaoQuarto.Grupo { nome = "Puzzle 3 · Cama" },
        };
        arr.sacosLixo = new List<Transform>();
        arr.pecasNinho = new List<Transform>();
        arr.lencol = null;
        arr.materialEdredom = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ref_Edredom_Lilas.mat");
        arr.volume = Object.FindFirstObjectByType<Volume>();
        GameObject cestoObj = GameObject.Find("Cesto_Roupa_Suja");
        arr.cesto = cestoObj != null ? cestoObj.transform : null;

        GameObject mesa = GameObject.Find("Mesa_PostIts");
        GameObject mesaVisual = mesa;
        if (mesa != null && mesa.transform.Find("VisualModelo") != null) mesaVisual = mesa.transform.Find("VisualModelo").gameObject;
        GameObject cama = GameObject.Find("bedSingle");
        Bounds bMesa, bCama;
        if (!TentarBounds(mesaVisual, out bMesa)) bMesa = new Bounds(new Vector3(-2.5f, 0.4f, -0.9f), Vector3.one);
        if (!TentarBounds(cama, out bCama)) bCama = new Bounds(new Vector3(2.8f, 0.3f, 0.85f), Vector3.one);
        Vector3[] ancoras = { bMesa.center, (bMesa.center + bCama.center) * 0.5f, bCama.center };

        foreach (string nomeRaiz in new[] { "Bagunca_Depressao", "Bagunca_Quarto" })
        {
            GameObject r = GameObject.Find(nomeRaiz);
            if (r == null) continue;
            foreach (Transform t in r.transform)
            {
                if (t.name == "Ninho")
                {
                    foreach (Transform p in t) ClassificarItemNinho(arr, p);
                    continue;
                }
                if (t.name.StartsWith("SacoLixo")) { arr.sacosLixo.Add(t); continue; }
                if (t.name.StartsWith("Ninho_")) { arr.grupos[2].itens.Add(t); continue; }
                if (Fica(t.name)) continue;

                int g;
                if (t.name.StartsWith("Mesa_") || t.name.StartsWith("MesaChao_")) g = 0;
                else if (t.name.StartsWith("Meio_")) g = 1;
                else if (t.name.StartsWith("CamaChao_") || t.name.StartsWith("Tenis_") || t.name.StartsWith("Cama_")) g = 2;
                else g = MaisPerto(t.position, ancoras);
                arr.grupos[g].itens.Add(t);
            }
        }
        GameObject estante = GameObject.Find("bookcaseOpen");
        arr.estante = estante != null ? estante.transform : null;
        EditorUtility.SetDirty(arr);
        MarcarCena();
        Debug.Log("Arrumação: puzzle 1 = " + arr.grupos[0].itens.Count + " itens, puzzle 2 = " + arr.grupos[1].itens.Count +
                  ", puzzle 3 = " + arr.grupos[2].itens.Count + " itens + " + arr.pecasNinho.Count + " partes do ninho. " +
                  "Cesto: " + (arr.cesto != null ? "ok" : "não achado") + ", sacos de lixo: " + arr.sacosLixo.Count + ".");
    }

    // Peças do casulo do edredom (somem quando a cama é feita) e o lençol (base do edredom arrumado).
    static void ClassificarItemNinho(ArrumacaoQuarto arr, Transform p)
    {
        string n = p.name;
        if (n == "Lencol") { arr.lencol = p; return; }
        if (n.StartsWith("Dobra_") || n.StartsWith("Volume_") || n.StartsWith("Aba_") || n.StartsWith("Escorrendo_") ||
            n.StartsWith("No_Chao") || n == "Lencol_Solto")
        { arr.pecasNinho.Add(p); return; }
        if (!n.StartsWith("Ninho_") && Fica(n)) return;
        arr.grupos[2].itens.Add(p);
    }

    // O que continua no quarto mesmo arrumado.
    static bool Fica(string n)
    {
        string[] ficam = { "Cesto_Roupa_Suja", "Cabo_Carregador", "Mochila", "pillow", "Pillow", "bear", "Bear", "Arara", "Luz_" };
        foreach (string f in ficam) if (n.Contains(f)) return true;
        return false;
    }

    static int MaisPerto(Vector3 p, Vector3[] ancoras)
    {
        int melhor = 0; float menor = float.MaxValue;
        for (int i = 0; i < ancoras.Length; i++)
        {
            float d = new Vector2(p.x - ancoras[i].x, p.z - ancoras[i].z).sqrMagnitude;
            if (d < menor) { menor = d; melhor = i; }
        }
        return melhor;
    }

    static void MarcarCena()
    {
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
