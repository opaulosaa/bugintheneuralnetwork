using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Monta a cena base do jogo com um clique: sala, Faísca, câmera, UI dos 4 puzzles
/// e o GameManager, tudo já referenciado. Rode uma única vez por cena vazia.
/// Menu: Bug In The Neural Network > Montar Cena Base
/// </summary>
public static partial class SceneSetup
{
    [MenuItem("Bug In The Neural Network/Montar Cena Base")]
    public static void Montar()
    {
        // ---------- Luz ambiente e câmera (já existem na cena padrão) ----------
        Light luzAmbiente = Object.FindFirstObjectByType<Light>();
        Camera camPrincipal = Camera.main;

        // ---------- EventSystem (necessário para os botões de UI funcionarem) ----------
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var uiModule = es.AddComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
        }

        // ---------- Chão e paredes ----------
        GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Chao_Quarto";
        chao.transform.position = Vector3.zero;
        chao.transform.localScale = new Vector3(2f, 1f, 2f);

        CriarParede("Parede_Norte", new Vector3(0, 2.5f, 10f), new Vector3(20f, 5f, 0.3f));
        CriarParede("Parede_Sul", new Vector3(0, 2.5f, -10f), new Vector3(20f, 5f, 0.3f));
        CriarParede("Parede_Leste", new Vector3(10f, 2.5f, 0), new Vector3(0.3f, 5f, 20f));
        CriarParede("Parede_Oeste", new Vector3(-10f, 2.5f, 0), new Vector3(0.3f, 5f, 20f));

        // ---------- Faísca (player) ----------
        GameObject faisca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        faisca.name = "Faisca";
        faisca.transform.position = new Vector3(0f, 0.5f, -6f);
        faisca.transform.localScale = Vector3.one * 0.6f;
        Object.DestroyImmediate(faisca.GetComponent<SphereCollider>());

        CharacterController cc = faisca.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0, 0);
        cc.radius = 0.3f;
        cc.height = 0.6f;

        var movimento = faisca.AddComponent<PlayerMovement>();
        var interactor = faisca.AddComponent<ProximityInteractor>();

        GameObject faiscaLuzObj = new GameObject("FaiscaLuz");
        faiscaLuzObj.transform.SetParent(faisca.transform);
        faiscaLuzObj.transform.localPosition = Vector3.zero;
        Light faiscaLuz = faiscaLuzObj.AddComponent<Light>();
        faiscaLuz.type = LightType.Point;
        faiscaLuz.range = 6f;
        faiscaLuz.intensity = 1.5f;
        faiscaLuz.color = new Color(0.6f, 0.75f, 1f);

        // ---------- Câmera segue a Faísca ----------
        if (camPrincipal != null)
        {
            var followCam = camPrincipal.GetComponent<SimpleCameraFollow>();
            if (followCam == null) followCam = camPrincipal.gameObject.AddComponent<SimpleCameraFollow>();
            followCam.alvo = faisca.transform;
        }

        // ---------- Canvas principal ----------
        GameObject canvasObj = new GameObject("Canvas_UI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Prompt de interação
        Text promptTexto = CriarTexto(canvasObj.transform, "TextoPrompt", "", 28,
            new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(0, 0), new Vector2(700, 50));
        promptTexto.alignment = TextAnchor.MiddleCenter;
        promptTexto.gameObject.SetActive(false);
        interactor.textoPrompt = promptTexto;

        // Tela de apagão
        GameObject apagaoObj = new GameObject("TelaApagao");
        apagaoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform apagaoRect = apagaoObj.AddComponent<RectTransform>();
        apagaoRect.anchorMin = Vector2.zero; apagaoRect.anchorMax = Vector2.one;
        apagaoRect.offsetMin = Vector2.zero; apagaoRect.offsetMax = Vector2.zero;
        Image apagaoImg = apagaoObj.AddComponent<Image>();
        apagaoImg.color = Color.black;
        CanvasGroup apagaoGrupo = apagaoObj.AddComponent<CanvasGroup>();
        apagaoGrupo.alpha = 0f;
        apagaoGrupo.blocksRaycasts = false;
        apagaoGrupo.interactable = false;

        // ---------- StoryState ----------
        GameObject storyObj = new GameObject("StoryState");
        StoryState storyState = storyObj.AddComponent<StoryState>();

        // ---------- Painel Puzzle 1: fila de prioridade ----------
        GameObject painel1 = CriarPainel(canvasObj.transform, "Painel_Puzzle1", TEXTO_PUZZLE1);
        string[] labelsP1 = { "Estudar para a prova de Redes", "Corrigir o bug do projeto", "Dormir", "Comer algo", "Ensaiar a apresentação" };
        int[] ordemP1 = { 3, 1, 4, 0, 2 };
        var postIts = new PuzzlePriorityQueue.PostIt[5];
        for (int i = 0; i < 5; i++)
        {
            Button b = CriarBotao(painel1.transform, "Post_" + i, labelsP1[i], new Vector2(0.5f, 0.75f - i * 0.13f));
            postIts[i] = new PuzzlePriorityQueue.PostIt { botao = b, ordemCorreta = ordemP1[i] };
        }
        Text feedback1 = CriarTexto(painel1.transform, "Feedback", "", 22, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(700, 40));

        GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesa.name = "Mesa_PostIts";
        mesa.transform.position = new Vector3(-4f, 0.5f, -3f);
        mesa.transform.localScale = new Vector3(1.6f, 1f, 0.9f);
        var puzzle1 = mesa.AddComponent<PuzzlePriorityQueue>();
        puzzle1.textoPrompt = "Aperte E: Mesa de tarefas";
        puzzle1.painel = painel1;
        puzzle1.postIts = postIts;
        puzzle1.textoFeedback = feedback1;
        puzzle1.storyState = storyState;
        painel1.SetActive(false);

        // ---------- Painel Puzzle 2: loop de pensamento ----------
        GameObject painel2 = CriarPainel(canvasObj.transform, "Painel_Puzzle2",
            "enquanto (verdadeiro) { pensar(\"Eu não dou conta.\"); }\nO laço não tem teste de saída. Qual condição poderia interrompê-lo?");
        (string texto, string feedback, bool correta)[] opcoesP2 = new (string, string, bool)[]
        {
            ("A: Parar quando eu fizer tudo sem errar.", "Se a saída exige perfeição, o laço nunca sai. Sempre haverá um erro para conferir.", false),
            ("B: Parar quando alguém me der razão.", "Uma condição que depende dos outros deixa o laço na mão deles. E eles nem sabem que ele roda.", false),
            ("C: Parar quando os fatos disserem o contrário.", "Banco de Dados: entregue. Estruturas de Dados: aprovada. Eu dou conta de mais do que lembro.", true),
            ("D: Parar quando o cansaço passar.", "O cansaço alimenta o laço, mas não é o teste de saída. Esperar passar só adia a próxima volta.", false),
        };
        var opcoes = new PuzzleLoop.Opcao[4];
        for (int i = 0; i < 4; i++)
        {
            Button b = CriarBotao(painel2.transform, "Opcao_" + i, opcoesP2[i].texto, new Vector2(0.5f, 0.62f - i * 0.13f));
            opcoes[i] = new PuzzleLoop.Opcao { botao = b, textoFeedback = opcoesP2[i].feedback, correta = opcoesP2[i].correta };
        }
        Text feedback2 = CriarTexto(painel2.transform, "Feedback", "", 22, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(800, 60));

        GameObject quadro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        quadro.name = "Quadro_Loop";
        quadro.transform.position = new Vector3(0f, 1.2f, -9.6f);
        quadro.transform.localScale = new Vector3(2f, 1.2f, 0.1f);
        var puzzle2 = quadro.AddComponent<PuzzleLoop>();
        puzzle2.textoPrompt = "Aperte E: Quadro";
        puzzle2.painel = painel2;
        puzzle2.opcoes = opcoes;
        puzzle2.textoFeedback = feedback2;
        puzzle2.storyState = storyState;
        painel2.SetActive(false);

        // ---------- Painel Puzzle 3: firewall social ----------
        GameObject painel3 = CriarPainel(canvasObj.transform, "Painel_Puzzle3", "A Bia mandou várias mensagens e nenhuma foi respondida.\nRevise as regras do celular e clique em Aplicar.");
        string[] labelsP3 = { "Só responder quando eu tiver boas notícias", "Não incomodar ninguém com meus problemas", "Responder depois, quando eu estiver melhor", "Silenciar spam e promoções" };
        var toggles = new Toggle[4];
        for (int i = 0; i < 4; i++)
        {
            toggles[i] = CriarToggle(painel3.transform, "Regra_" + i, labelsP3[i], new Vector2(0.5f, 0.68f - i * 0.11f), true);
        }
        Button botaoAplicar = CriarBotao(painel3.transform, "BotaoAplicar", "Aplicar", new Vector2(0.5f, 0.2f));
        Text feedback3 = CriarTexto(painel3.transform, "Feedback", "", 22, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(800, 40));

        GameObject celular = GameObject.CreatePrimitive(PrimitiveType.Cube);
        celular.name = "Celular_Firewall";
        celular.transform.position = new Vector3(4f, 0.5f, -3f);
        celular.transform.localScale = new Vector3(0.3f, 0.6f, 0.05f);
        var puzzle3 = celular.AddComponent<PuzzleFirewall>();
        puzzle3.textoPrompt = "Aperte E: Celular";
        puzzle3.painel = painel3;
        puzzle3.regras = toggles;
        puzzle3.botaoAplicar = botaoAplicar;
        puzzle3.textoFeedback = feedback3;
        puzzle3.storyState = storyState;
        painel3.SetActive(false);

        // ---------- Painel final e cortina ----------
        GameObject painelBloqueado = CriarPainel(canvasObj.transform, "Painel_Bloqueado", "Ainda não é hora.");
        painelBloqueado.SetActive(false);

        GameObject painelFinal = CriarPainel(canvasObj.transform, "Painel_Final", "");
        Text textoFinal = painelFinal.transform.Find("Titulo").GetComponent<Text>();
        textoFinal.fontSize = 26;
        painelFinal.SetActive(false);

        GameObject cortina = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cortina.name = "Cortina";
        cortina.transform.position = new Vector3(0f, 1.5f, 9.6f);
        cortina.transform.localScale = new Vector3(3f, 3f, 0.1f);
        var puzzleFinal = cortina.AddComponent<PuzzleFinal>();
        puzzleFinal.textoPrompt = "Aperte E: Cortina";
        puzzleFinal.storyState = storyState;
        puzzleFinal.painelBloqueado = painelBloqueado;
        puzzleFinal.painelFinal = painelFinal;
        puzzleFinal.textoMensagemFinal = textoFinal;
        puzzleFinal.luzAmbiente = luzAmbiente;

        // ---------- GameManager ----------
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.faiscaLuz = faiscaLuz;
        gm.luzAmbiente = luzAmbiente;
        gm.telaApagao = apagaoGrupo;
        gm.faiscaPlayer = faisca;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cena base montada. Salve a cena (Ctrl+S) e aperte Play para testar.");
    }

    static void CriarParede(string nome, Vector3 pos, Vector3 escala)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = nome;
        p.transform.position = pos;
        p.transform.localScale = escala;
    }

    static GameObject CriarPainel(Transform pai, string nome, string titulo)
    {
        GameObject painel = new GameObject(nome);
        painel.transform.SetParent(pai, false);
        RectTransform rt = painel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 700);
        rt.anchoredPosition = Vector2.zero;
        Image fundo = painel.AddComponent<Image>();
        fundo.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);

        CriarTexto(painel.transform, "Titulo", titulo, 26, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(800, 120));
        return painel;
    }

    static Text CriarTexto(Transform pai, string nome, string conteudo, int tamanho, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 tamanhoRect)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanhoRect;
        Text txt = obj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = tamanho;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    static Button CriarBotao(Transform pai, string nome, string texto, Vector2 anchorPos01)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos01;
        rt.anchorMax = anchorPos01;
        rt.sizeDelta = new Vector2(750, 55);
        rt.anchoredPosition = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f);
        Button botao = obj.AddComponent<Button>();

        GameObject textoObj = new GameObject("Texto");
        textoObj.transform.SetParent(obj.transform, false);
        RectTransform textoRt = textoObj.AddComponent<RectTransform>();
        textoRt.anchorMin = Vector2.zero; textoRt.anchorMax = Vector2.one;
        textoRt.offsetMin = Vector2.zero; textoRt.offsetMax = Vector2.zero;
        Text txt = textoObj.AddComponent<Text>();
        txt.text = texto;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;

        return botao;
    }

    static Toggle CriarToggle(Transform pai, string nome, string texto, Vector2 anchorPos01, bool ligado)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos01;
        rt.anchorMax = anchorPos01;
        rt.sizeDelta = new Vector2(750, 45);
        rt.anchoredPosition = Vector2.zero;

        GameObject fundoObj = new GameObject("Fundo");
        fundoObj.transform.SetParent(obj.transform, false);
        RectTransform fundoRt = fundoObj.AddComponent<RectTransform>();
        fundoRt.anchorMin = new Vector2(0f, 0.5f);
        fundoRt.anchorMax = new Vector2(0f, 0.5f);
        fundoRt.sizeDelta = new Vector2(35, 35);
        fundoRt.anchoredPosition = new Vector2(20, 0);
        Image fundoImg = fundoObj.AddComponent<Image>();
        fundoImg.color = Color.white;

        GameObject marcaObj = new GameObject("Marca");
        marcaObj.transform.SetParent(fundoObj.transform, false);
        RectTransform marcaRt = marcaObj.AddComponent<RectTransform>();
        marcaRt.anchorMin = Vector2.zero; marcaRt.anchorMax = Vector2.one;
        marcaRt.offsetMin = new Vector2(5, 5); marcaRt.offsetMax = new Vector2(-5, -5);
        Image marcaImg = marcaObj.AddComponent<Image>();
        marcaImg.color = new Color(0.2f, 0.6f, 0.3f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = fundoImg;
        toggle.graphic = marcaImg;
        toggle.isOn = ligado;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.5f);
        labelRt.anchorMax = new Vector2(1f, 0.5f);
        labelRt.offsetMin = new Vector2(50, -20);
        labelRt.offsetMax = new Vector2(0, 20);
        Text labelTxt = labelObj.AddComponent<Text>();
        labelTxt.text = texto;
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.fontSize = 18;
        labelTxt.color = Color.white;
        labelTxt.alignment = TextAnchor.MiddleLeft;
        labelTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

        return toggle;
    }

    [MenuItem("Bug In The Neural Network/Adicionar Tela Inicial")]
    public static void AdicionarTelaInicial()
    {
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        GameObject faisca = GameObject.Find("Faisca");
        if (canvasObj == null || faisca == null)
        {
            Debug.LogError("Rode primeiro 'Montar Cena Base'.");
            return;
        }

        // Remove a tela inicial antiga (se existir) para reconstruir o menu completo.
        Transform painelAntigo = canvasObj.transform.Find("Painel_Inicial");
        if (painelAntigo != null) Object.DestroyImmediate(painelAntigo.gameObject);
        TelaInicial controllerAntigo = Object.FindFirstObjectByType<TelaInicial>();
        if (controllerAntigo != null) Object.DestroyImmediate(controllerAntigo.gameObject);

        Color corFundo = new Color(0.03f, 0.04f, 0.07f, 1f);

        // ---------- Menu principal ----------
        GameObject painel = new GameObject("Painel_Inicial");
        painel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = painel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image fundo = painel.AddComponent<Image>();
        fundo.color = corFundo;

        Text titulo = CriarTexto(painel.transform, "Titulo", "Bug in the Neural Network", 56,
            new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(1200, 90));
        titulo.fontStyle = FontStyle.Bold;

        Text subtitulo = CriarTexto(painel.transform, "Subtitulo", "Um escape room sobre saúde mental e computação", 24,
            new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1200, 40));
        subtitulo.color = new Color(0.70f, 0.75f, 0.90f);

        CriarTexto(painel.transform, "Contexto",
            TEXTO_CONTEXTO,
            22, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(1000, 200));

        Button botaoJogar = CriarBotao(painel.transform, "BotaoComecar", "Jogar", new Vector2(0.5f, 0.36f));
        Button botaoComoJogar = CriarBotao(painel.transform, "BotaoComoJogar", "Como jogar", new Vector2(0.5f, 0.28f));
        Button botaoSair = CriarBotao(painel.transform, "BotaoSair", "Sair", new Vector2(0.5f, 0.20f));
        foreach (Button b in new[] { botaoJogar, botaoComoJogar, botaoSair })
        {
            ((RectTransform)b.transform).sizeDelta = new Vector2(420, 60);
        }

        // ---------- Painel "Como jogar" (fica dentro do menu, começa desligado) ----------
        GameObject painelComoJogar = new GameObject("Painel_ComoJogar");
        painelComoJogar.transform.SetParent(painel.transform, false);
        RectTransform cjRt = painelComoJogar.AddComponent<RectTransform>();
        cjRt.anchorMin = Vector2.zero; cjRt.anchorMax = Vector2.one;
        cjRt.offsetMin = Vector2.zero; cjRt.offsetMax = Vector2.zero;
        Image cjFundo = painelComoJogar.AddComponent<Image>();
        cjFundo.color = corFundo;

        Text cjTitulo = CriarTexto(painelComoJogar.transform, "Titulo", "Como jogar", 44,
            new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(1000, 70));
        cjTitulo.fontStyle = FontStyle.Bold;

        Text cjTexto = CriarTexto(painelComoJogar.transform, "Texto",
            TEXTO_COMO_JOGAR,
            24, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1100, 520));
        cjTexto.alignment = TextAnchor.MiddleLeft;

        Button botaoVoltar = CriarBotao(painelComoJogar.transform, "BotaoVoltar", "Voltar", new Vector2(0.5f, 0.10f));
        ((RectTransform)botaoVoltar.transform).sizeDelta = new Vector2(420, 60);
        painelComoJogar.SetActive(false);

        // ---------- Controller ----------
        GameObject controllerObj = new GameObject("TelaInicialController");
        TelaInicial controller = controllerObj.AddComponent<TelaInicial>();
        controller.painel = painel;
        controller.faiscaPlayer = faisca;
        controller.botaoComecar = botaoJogar;
        controller.botaoComoJogar = botaoComoJogar;
        controller.botaoSair = botaoSair;
        controller.painelComoJogar = painelComoJogar;
        controller.botaoVoltar = botaoVoltar;

        // O menu fica por cima de tudo (inclusive do HUD dos puzzles).
        painel.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Menu principal criado (Jogar, Como jogar, Sair). Salve a cena e teste o Play.");
    }

    [MenuItem("Bug In The Neural Network/Rotular Puzzles 1, 2 e 3")]
    public static void RotularPuzzles()
    {
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null)
        {
            Debug.LogError("Rode primeiro 'Montar Cena Base'.");
            return;
        }

        PuzzlePriorityQueue p1 = Object.FindFirstObjectByType<PuzzlePriorityQueue>();
        PuzzleLoop p2 = Object.FindFirstObjectByType<PuzzleLoop>();
        PuzzleFirewall p3 = Object.FindFirstObjectByType<PuzzleFirewall>();
        if (p1 == null || p2 == null || p3 == null)
        {
            Debug.LogError("Não encontrei os três puzzles na cena.");
            return;
        }

        RotularPuzzle(p1, p1.painel, 1, "Fila de prioridade", "Mesa de tarefas");
        RotularPuzzle(p2, p2.painel, 2, "Loop de pensamento", "Quadro");
        RotularPuzzle(p3, p3.painel, 3, "Firewall social", "Celular");

        // A cortina avisa que depende dos três puzzles.
        PuzzleFinal final = Object.FindFirstObjectByType<PuzzleFinal>();
        if (final != null && final.painelBloqueado != null)
        {
            Transform tituloBloqueado = final.painelBloqueado.transform.Find("Titulo");
            Text txtBloqueado = tituloBloqueado != null ? tituloBloqueado.GetComponent<Text>() : null;
            if (txtBloqueado != null) txtBloqueado.text = "Ainda não é hora.\nConclua os Puzzles 1, 2 e 3 primeiro.";
        }

        // HUD fixo com a lista dos três puzzles.
        Transform hudAntigo = canvasObj.transform.Find("HUD_Puzzles");
        if (hudAntigo != null) Object.DestroyImmediate(hudAntigo.gameObject);

        GameObject hud = new GameObject("HUD_Puzzles");
        hud.transform.SetParent(canvasObj.transform, false);
        RectTransform hudRt = hud.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.sizeDelta = new Vector2(560, 120);
        hudRt.anchoredPosition = new Vector2(24, -24);
        Image hudFundo = hud.AddComponent<Image>();
        hudFundo.color = new Color(0.03f, 0.04f, 0.07f, 0.6f);
        hudFundo.raycastTarget = false;

        Text hudTexto = CriarTexto(hud.transform, "Texto", "", 22, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hudTexto.rectTransform.offsetMin = new Vector2(16, 10);
        hudTexto.rectTransform.offsetMax = new Vector2(-16, -10);
        hudTexto.alignment = TextAnchor.MiddleLeft;
        hudTexto.raycastTarget = false;

        PuzzleProgressHUD progresso = hud.AddComponent<PuzzleProgressHUD>();
        progresso.texto = hudTexto;
        hud.transform.SetAsFirstSibling();

        // Se o menu já existe, garante que ele fica por cima.
        Transform menu = canvasObj.transform.Find("Painel_Inicial");
        if (menu != null) menu.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Puzzles 1, 2 e 3 rotulados (prompts, painéis e HUD). Salve a cena.");
    }

    static void RotularPuzzle(Interactable puzzle, GameObject painel, int numero, string nomePuzzle, string nomeObjeto)
    {
        puzzle.textoPrompt = "Puzzle " + numero + " · Aperte E: " + nomeObjeto;

        if (painel == null) return;

        Transform antigo = painel.transform.Find("Badge_Puzzle");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        // Etiqueta roxa encaixada acima do painel: "PUZZLE 1 DE 3 · FILA DE PRIORIDADE"
        GameObject badge = new GameObject("Badge_Puzzle");
        badge.transform.SetParent(painel.transform, false);
        RectTransform rt = badge.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(560, 46);
        rt.anchoredPosition = new Vector2(0, 8);
        Image fundo = badge.AddComponent<Image>();
        fundo.color = new Color(0.50f, 0.28f, 0.85f, 1f);

        Text txt = CriarTexto(badge.transform, "Texto",
            "PUZZLE " + numero + " DE 3 · " + nomePuzzle.ToUpperInvariant(), 22,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;
        txt.fontStyle = FontStyle.Bold;
    }

    [MenuItem("Bug In The Neural Network/Atualizar Textos dos Puzzles")]
    public static void AtualizarTextosDosPuzzles()
    {
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null) { Debug.LogError("Canvas_UI não encontrado."); return; }
        Transform c = canvasObj.transform;

        // Puzzle 1: ordem lógica (rótulo -> ordem)
        var p1 = Object.FindFirstObjectByType<PuzzlePriorityQueue>();
        if (p1 != null)
        {
            var ordem = new System.Collections.Generic.Dictionary<string, int> {
                { "Comer algo", 0 }, { "Corrigir o bug do projeto", 1 }, { "Ensaiar a apresentação", 2 },
                { "Estudar para a prova de Redes", 3 }, { "Dormir", 4 } };
            foreach (var pi in p1.postIts)
            {
                string t = pi.botao.GetComponentInChildren<Text>().text;
                if (ordem.ContainsKey(t)) pi.ordemCorreta = ordem[t];
                else Debug.LogWarning("Rótulo não reconhecido no Puzzle 1: " + t);
            }
            EditorUtility.SetDirty(p1);
        }
        DefinirTitulo(c, "Painel_Puzzle1", TEXTO_PUZZLE1);

        // Puzzle 2
        var p2 = Object.FindFirstObjectByType<PuzzleLoop>();
        if (p2 != null && p2.opcoes != null && p2.opcoes.Length == 4)
        {
            string[] textos = { "A: Parar quando eu fizer tudo sem errar.", "B: Parar quando alguém me der razão.", "C: Parar quando os fatos disserem o contrário.", "D: Parar quando o cansaço passar." };
            string[] fb = {
                "Se a saída exige perfeição, o laço nunca sai. Sempre haverá um erro para conferir.",
                "Uma condição que depende dos outros deixa o laço na mão deles. E eles nem sabem que ele roda.",
                "Banco de Dados: entregue. Estruturas de Dados: aprovada. Eu dou conta de mais do que lembro.",
                "O cansaço alimenta o laço, mas não é o teste de saída. Esperar passar só adia a próxima volta." };
            for (int i = 0; i < 4; i++)
            {
                p2.opcoes[i].botao.GetComponentInChildren<Text>().text = textos[i];
                p2.opcoes[i].textoFeedback = fb[i];
                p2.opcoes[i].correta = (i == 2);
            }
            EditorUtility.SetDirty(p2);
        }
        DefinirTitulo(c, "Painel_Puzzle2", "enquanto (verdadeiro) { pensar(\"Eu não dou conta.\"); }\nO laço não tem teste de saída. Qual condição poderia interrompê-lo?");

        // Puzzle 3
        var p3 = Object.FindFirstObjectByType<PuzzleFirewall>();
        if (p3 != null && p3.regras != null && p3.regras.Length == 4)
        {
            string[] regras = { "Só responder quando eu tiver boas notícias", "Não incomodar ninguém com meus problemas", "Responder depois, quando eu estiver melhor", "Silenciar spam e promoções" };
            for (int i = 0; i < 4; i++) p3.regras[i].GetComponentInChildren<Text>().text = regras[i];
            EditorUtility.SetDirty(p3);
        }
        DefinirTitulo(c, "Painel_Puzzle3", "A Bia mandou várias mensagens e nenhuma foi respondida.\nRevise as regras do celular e clique em Aplicar.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Textos e ordem dos puzzles atualizados. Salve a cena (Ctrl+S).");
    }

    static void DefinirTitulo(Transform canvas, string painel, string texto)
    {
        Transform p = canvas.Find(painel);
        if (p == null) { Debug.LogWarning(painel + " não encontrado."); return; }
        Transform t = p.Find("Titulo");
        if (t != null) t.GetComponent<Text>().text = texto;
    }

    [MenuItem("Bug In The Neural Network/Corrigir Texto do Puzzle 3")]
    public static void CorrigirTextoPuzzle3()
    {
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj == null)
        {
            Debug.LogError("Rode primeiro 'Montar Cena Base'.");
            return;
        }

        Transform painel3 = canvasObj.transform.Find("Painel_Puzzle3");
        if (painel3 == null)
        {
            Debug.LogError("Painel_Puzzle3 não encontrado.");
            return;
        }

        Text titulo = painel3.Find("Titulo").GetComponent<Text>();
        titulo.text = "Desligue as regras que bloqueiam as mensagens da Bia.\nMantenha o filtro de spam ligado. Depois, clique em Aplicar.";
        titulo.fontSize = 22;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Texto do Puzzle 3 atualizado. Salve a cena.");
    }

    [MenuItem("Bug In The Neural Network/Melhorar Visual da Faisca")]
    public static void MelhorarVisualFaisca()
    {
        GameObject faisca = GameObject.Find("Faisca");
        if (faisca == null)
        {
            Debug.LogError("Rode primeiro 'Montar Cena Base'.");
            return;
        }

        string pastaMateriais = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(pastaMateriais))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        string caminhoMaterial = pastaMateriais + "/Faisca_Emissiva.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminhoMaterial);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, caminhoMaterial);
        }

        Color corBase = new Color(0.55f, 0.75f, 1f);
        mat.color = corBase;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", corBase * 2.5f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        var renderer = faisca.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;

        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            Bloom bloom;
            if (!volume.profile.TryGet(out bloom))
            {
                bloom = volume.profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.2f;
        }
        else
        {
            Debug.LogWarning("Nenhum Volume global encontrado na cena: o brilho (Bloom) não foi ativado, mas o material emissivo foi aplicado normalmente.");
        }

        EditorUtility.SetDirty(mat);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Visual da Faísca atualizado. Salve a cena.");
    }

    [MenuItem("Bug In The Neural Network/Antigos/Substituir Moveis por Modelos Kenney")]
    public static void SubstituirMoveisKenney()
    {
        const string pasta = "Assets/Models/FBX format/";

        SubstituirVisual("Mesa_PostIts", pasta + "desk.fbx", 0.78f, Quaternion.identity, true);
        SubstituirVisual("Quadro_Loop", pasta + "computerScreen.fbx", 0.42f, Quaternion.identity, false);

        AdicionarMobilia(pasta + "bedSingle.fbx", new Vector3(-8.3f, 0f, 6f), Quaternion.Euler(0, 90, 0), 0.55f, "y");
        AdicionarMobilia(pasta + "chairDesk.fbx", new Vector3(-4f, 0f, -1.7f), Quaternion.Euler(0, 180, 0), 0.85f, "y");
        AdicionarMobilia(pasta + "bookcaseOpen.fbx", new Vector3(8.7f, 0f, 2f), Quaternion.Euler(0, -90, 0), 1.8f, "y");
        AdicionarMobilia(pasta + "rugRectangle.fbx", new Vector3(0f, 0.02f, -5f), Quaternion.identity, 2.2f, "x");
        AdicionarMobilia(pasta + "lampRoundFloor.fbx", new Vector3(-8f, 0f, 8.3f), Quaternion.identity, 1.5f, "y");
        AdicionarMobilia(pasta + "pottedPlant.fbx", new Vector3(8.3f, 0f, 8.3f), Quaternion.identity, 0.6f, "y");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Móveis do Kenney adicionados/trocados, com escala ajustada automaticamente pro tamanho real da sala. Ajuste posição/rotação na Scene view se precisar, depois salve.");
    }

    static void SubstituirVisual(string nomeObjeto, string caminhoModelo, float alturaAlvoMetros, Quaternion rotacaoLocal, bool ancorarNoChao)
    {
        GameObject alvo = GameObject.Find(nomeObjeto);
        if (alvo == null) { Debug.LogWarning(nomeObjeto + " não encontrado na cena. Rode 'Montar Cena Base' primeiro."); return; }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoModelo);
        if (prefab == null) { Debug.LogWarning("Modelo não encontrado: " + caminhoModelo + " (dê um clique na janela do Unity pra garantir que ele importou os arquivos antes de rodar de novo)."); return; }

        var mr = alvo.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Transform existente = alvo.transform.Find("VisualModelo");
        if (existente != null) Object.DestroyImmediate(existente.gameObject);

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, alvo.transform);
        instancia.name = "VisualModelo";
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = rotacaoLocal;
        instancia.transform.localScale = Vector3.one;

        AjustarEscala(instancia, alturaAlvoMetros, "y", ancorarNoChao);
    }

    static void AdicionarMobilia(string caminhoModelo, Vector3 posicao, Quaternion rotacao, float tamanhoAlvoMetros, string eixo)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoModelo);
        if (prefab == null) { Debug.LogWarning("Modelo não encontrado: " + caminhoModelo); return; }

        // Evita duplicar se essa mobília já foi colocada numa execução anterior.
        GameObject anterior = GameObject.Find(prefab.name);
        if (anterior != null) Object.DestroyImmediate(anterior);

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instancia.name = prefab.name;
        instancia.transform.position = posicao;
        instancia.transform.rotation = rotacao;
        instancia.transform.localScale = Vector3.one;

        AjustarEscala(instancia, tamanhoAlvoMetros, eixo, true);
    }

    static void AjustarEscala(GameObject obj, float alvoMetros, string eixo, bool ancorarNoChao)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("Modelo sem Renderer para medir: " + obj.name);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float tamanhoAtual = eixo == "x" ? bounds.size.x : (eixo == "z" ? bounds.size.z : bounds.size.y);
        if (tamanhoAtual <= 0.0001f)
        {
            Debug.LogWarning("Não consegui medir o tamanho de " + obj.name + " nesse eixo.");
            return;
        }

        float fator = alvoMetros / tamanhoAtual;
        obj.transform.localScale *= fator;

        if (ancorarNoChao)
        {
            renderers = obj.GetComponentsInChildren<Renderer>();
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float deltaY = -bounds.min.y;
            obj.transform.position += new Vector3(0, deltaY, 0);
        }
    }

    [MenuItem("Bug In The Neural Network/Antigos/Mover Quadro Para Perto da Janela")]
    public static void MoverQuadroParaJanela()
    {
        ModelarJanelaCortinasEQuadro();
        return;
#pragma warning disable CS0162
        GameObject quadro = GameObject.Find("Quadro_Loop");
        if (quadro == null) { Debug.LogWarning("Quadro_Loop não encontrado. Rode 'Montar Cena Base' primeiro."); return; }

        // Parede leste, longe do ponto onde a Faísca começa o jogo (evita o atravessar por colisão fina).
        quadro.transform.position = new Vector3(9.6f, 1.2f, -6f);
        quadro.transform.rotation = Quaternion.Euler(0, -90, 0);
        quadro.transform.localScale = new Vector3(0.3f, 1.2f, 2f);

        // Reajusta o modelo visual (computerScreen) pro novo tamanho/posição do pai.
        SubstituirVisual("Quadro_Loop", "Assets/Models/FBX format/computerScreen.fbx", 0.42f, Quaternion.identity, false);

        // Janela ao lado, só decorativa (não bloqueia nem interage).
        GameObject prefabJanela = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FBX format/wallWindow.fbx");
        if (prefabJanela != null)
        {
            GameObject anteriorJanela = GameObject.Find(prefabJanela.name);
            if (anteriorJanela != null) Object.DestroyImmediate(anteriorJanela);

            GameObject janela = (GameObject)PrefabUtility.InstantiatePrefab(prefabJanela);
            janela.name = prefabJanela.name;
            janela.transform.position = new Vector3(9.75f, 2.1f, -8.5f);
            janela.transform.rotation = Quaternion.Euler(0, -90, 0);
            janela.transform.localScale = Vector3.one;
            AjustarEscala(janela, 1.4f, "y", false);

            var colJanela = janela.GetComponentInChildren<Collider>();
            if (colJanela != null) Object.DestroyImmediate(colJanela);
        }
        else
        {
            Debug.LogWarning("Modelo wallWindow.fbx não encontrado (é só decorativo, não essencial).");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Quadro movido pra parede leste, perto da janela. Salve a cena.");
#pragma warning restore CS0162
    }

    // ======================= JANELA, CORTINAS E QUADRO =======================

    static Material MatURP(string nome, Color cor, bool unlit)
    {
        string caminho = "Assets/Materials/" + nome + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            Shader sh = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, caminho);
        }
        m.SetColor("_BaseColor", cor);
        m.color = cor;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Bloco(Transform pai, string nome, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nome;
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        var col = g.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        return g;
    }

    static void ApagarSeExistir(string nome)
    {
        GameObject g;
        while ((g = GameObject.Find(nome)) != null) Object.DestroyImmediate(g);
    }

    [MenuItem("Bug In The Neural Network/Antigos/Modelar Janela, Cortinas e Quadro")]
    public static void ModelarJanelaCortinasEQuadro()
    {
        // Parede norte: face interna em z = 9.85 (a câmera olha para ela). Tudo abaixo fica na frente dela.
        ApagarSeExistir("Janela_Cortinas");
        ApagarSeExistir("Quadro_Decor");
        ApagarSeExistir("wallWindow");

        Material madeira = MatURP("Madeira_Janela", new Color(0.62f, 0.46f, 0.32f), false);
        Material metal = MatURP("Metal_Cortineiro", new Color(0.25f, 0.22f, 0.30f), false);
        Material ceuAlto = MatURP("Ceu_Alto", new Color(0.08f, 0.11f, 0.26f), true);
        Material ceuBaixo = MatURP("Ceu_Baixo", new Color(0.20f, 0.26f, 0.45f), true);
        Material lua = MatURP("Ceu_Lua", new Color(0.90f, 0.93f, 1f), true);
        Material estrela = MatURP("Ceu_Estrela", new Color(0.85f, 0.90f, 1f), true);
        Material cortinaA = MatURP("Cortina_A", new Color(0.36f, 0.26f, 0.50f), false);
        Material cortinaB = MatURP("Cortina_B", new Color(0.28f, 0.20f, 0.42f), false);

        GameObject raiz = new GameObject("Janela_Cortinas");

        // Céu atrás do vidro: duas faixas (gradiente) + lua + estrelas.
        var rAlto = Bloco(raiz.transform, "Ceu_Alto", PrimitiveType.Cube, new Vector3(0f, 3.35f, 9.83f), new Vector3(4.2f, 1.3f, 0.02f), ceuAlto);
        var rBaixo = Bloco(raiz.transform, "Ceu_Baixo", PrimitiveType.Cube, new Vector3(0f, 2.05f, 9.83f), new Vector3(4.2f, 1.3f, 0.02f), ceuBaixo);
        var rLua = Bloco(raiz.transform, "Lua", PrimitiveType.Sphere, new Vector3(-1.0f, 3.5f, 9.81f), new Vector3(0.5f, 0.5f, 0.05f), lua);
        Vector2[] posEstrelas = { new Vector2(0.9f, 3.7f), new Vector2(1.5f, 3.2f), new Vector2(0.3f, 3.85f), new Vector2(-1.7f, 3.0f), new Vector2(1.2f, 2.3f), new Vector2(-0.6f, 2.55f), new Vector2(1.8f, 3.85f) };
        var estrelasR = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < posEstrelas.Length; i++)
            estrelasR.Add(Bloco(raiz.transform, "Estrela_" + i, PrimitiveType.Cube, new Vector3(posEstrelas[i].x, posEstrelas[i].y, 9.81f), new Vector3(0.07f, 0.07f, 0.02f), estrela).GetComponent<Renderer>());

        // Moldura, divisórias e peitoril.
        Bloco(raiz.transform, "Moldura_Topo", PrimitiveType.Cube, new Vector3(0f, 4.09f, 9.72f), new Vector3(4.56f, 0.18f, 0.2f), madeira);
        Bloco(raiz.transform, "Moldura_Esq", PrimitiveType.Cube, new Vector3(-2.19f, 2.7f, 9.72f), new Vector3(0.18f, 2.78f, 0.2f), madeira);
        Bloco(raiz.transform, "Moldura_Dir", PrimitiveType.Cube, new Vector3(2.19f, 2.7f, 9.72f), new Vector3(0.18f, 2.78f, 0.2f), madeira);
        Bloco(raiz.transform, "Divisoria_V", PrimitiveType.Cube, new Vector3(0f, 2.7f, 9.76f), new Vector3(0.08f, 2.6f, 0.14f), madeira);
        Bloco(raiz.transform, "Divisoria_H", PrimitiveType.Cube, new Vector3(0f, 2.7f, 9.76f), new Vector3(4.2f, 0.08f, 0.14f), madeira);
        Bloco(raiz.transform, "Peitoril", PrimitiveType.Cube, new Vector3(0f, 1.3f, 9.6f), new Vector3(4.9f, 0.1f, 0.55f), madeira);

        // Varão e ponteiras.
        Bloco(raiz.transform, "Varao", PrimitiveType.Cube, new Vector3(0f, 4.4f, 9.45f), new Vector3(6.6f, 0.07f, 0.07f), metal);
        Bloco(raiz.transform, "Ponteira_Esq", PrimitiveType.Sphere, new Vector3(-3.35f, 4.4f, 9.45f), new Vector3(0.18f, 0.18f, 0.18f), metal);
        Bloco(raiz.transform, "Ponteira_Dir", PrimitiveType.Sphere, new Vector3(3.35f, 4.4f, 9.45f), new Vector3(0.18f, 0.18f, 0.18f), metal);

        // Cortinas: dois pivôs nas extremidades; as pregas ficam como filhos (a escala X do pivô "abre" a cortina).
        Transform esq = CriarCortina(raiz.transform, "Cortina_Esq", new Vector3(-3.1f, 0f, 9.45f), 1f, cortinaA, cortinaB);
        Transform dir = CriarCortina(raiz.transform, "Cortina_Dir", new Vector3(3.1f, 0f, 9.45f), -1f, cortinaA, cortinaB);

        // Luz fria do luar.
        GameObject luzObj = new GameObject("Luz_Janela");
        luzObj.transform.SetParent(raiz.transform, false);
        luzObj.transform.localPosition = new Vector3(0f, 2.7f, 8.6f);
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.55f, 0.65f, 1f);
        luz.intensity = 3f;
        luz.range = 9f;

        // O objeto "Cortina" (interativo) vira um volume invisível na frente da janela.
        GameObject cortinaObj = GameObject.Find("Cortina");
        if (cortinaObj != null)
        {
            var mr = cortinaObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            cortinaObj.transform.position = new Vector3(0f, 1.5f, 9.45f);
            cortinaObj.transform.rotation = Quaternion.identity;
            cortinaObj.transform.localScale = new Vector3(6.2f, 3f, 0.6f);

            var final = cortinaObj.GetComponent<PuzzleFinal>();
            if (final != null)
            {
                final.cortinaEsquerda = esq;
                final.cortinaDireita = dir;
                final.ceu = new Renderer[] { rAlto.GetComponent<Renderer>(), rBaixo.GetComponent<Renderer>(), rLua.GetComponent<Renderer>() };
                final.coresCeuFinal = new Color[] { new Color(0.95f, 0.71f, 0.36f), new Color(0.99f, 0.89f, 0.65f), new Color(1f, 0.95f, 0.78f) };
                final.estrelas = estrelasR.ToArray();
                final.luzJanela = luz;
                EditorUtility.SetDirty(final);
            }
        }
        else Debug.LogWarning("Objeto 'Cortina' não encontrado (rode 'Montar Cena Base').");

        // ----- Quadro do Puzzle 2, na mesma parede, à esquerda da janela -----
        GameObject quadro = GameObject.Find("Quadro_Loop");
        if (quadro != null)
        {
            Transform visual = quadro.transform.Find("VisualModelo");
            if (visual != null) Object.DestroyImmediate(visual.gameObject);
            var mr = quadro.GetComponent<MeshRenderer>();
            if (mr != null) { mr.enabled = true; mr.sharedMaterial = MatURP("Quadro_Verde", new Color(0.10f, 0.20f, 0.18f), false); }
            quadro.transform.position = new Vector3(-6.2f, 1.8f, 9.78f);
            quadro.transform.rotation = Quaternion.identity;
            quadro.transform.localScale = new Vector3(3.4f, 1.7f, 0.12f);

            GameObject deco = new GameObject("Quadro_Decor");
            Material giz = MatURP("Giz", new Color(0.92f, 0.95f, 0.92f), true);
            Bloco(deco.transform, "Moldura_Topo", PrimitiveType.Cube, new Vector3(-6.2f, 2.7f, 9.74f), new Vector3(3.6f, 0.1f, 0.16f), madeira);
            Bloco(deco.transform, "Moldura_Base", PrimitiveType.Cube, new Vector3(-6.2f, 0.9f, 9.74f), new Vector3(3.6f, 0.1f, 0.16f), madeira);
            Bloco(deco.transform, "Moldura_Esq", PrimitiveType.Cube, new Vector3(-7.95f, 1.8f, 9.74f), new Vector3(0.1f, 1.9f, 0.16f), madeira);
            Bloco(deco.transform, "Moldura_Dir", PrimitiveType.Cube, new Vector3(-4.45f, 1.8f, 9.74f), new Vector3(0.1f, 1.9f, 0.16f), madeira);
            Bloco(deco.transform, "Bandeja", PrimitiveType.Cube, new Vector3(-6.2f, 0.86f, 9.66f), new Vector3(3.2f, 0.05f, 0.2f), madeira);
            // Linhas de "código" em giz: enquanto (...) { pensar(...); }
            float[] larg = { 1.7f, 2.2f, 1.3f, 0.5f };
            float[] indent = { 0f, 0.3f, 0.3f, 0f };
            float[] ys = { 2.3f, 2.0f, 1.7f, 1.4f };
            for (int i = 0; i < larg.Length; i++)
            {
                float xIni = -7.7f + indent[i];
                Bloco(deco.transform, "Giz_" + i, PrimitiveType.Cube, new Vector3(xIni + larg[i] * 0.5f, ys[i], 9.705f), new Vector3(larg[i], 0.05f, 0.02f), giz);
            }
        }
        else Debug.LogWarning("Quadro_Loop não encontrado.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Janela, cortinas e quadro montados na parede norte. Salve a cena (Ctrl+S).");
    }

    static Transform CriarCortina(Transform pai, string nome, Vector3 pivoPos, float sentido, Material a, Material b)
    {
        GameObject pivo = new GameObject(nome);
        pivo.transform.SetParent(pai, false);
        pivo.transform.localPosition = pivoPos;
        const int pregas = 8;
        const float largura = 2.75f;
        float w = largura / pregas;
        for (int i = 0; i < pregas; i++)
        {
            float zOff = (i % 2 == 0) ? 0.04f : -0.04f;
            Bloco(pivo.transform, "Prega_" + i, PrimitiveType.Cube,
                new Vector3(sentido * (i + 0.5f) * w, 2.35f, zOff), new Vector3(w, 4.0f, 0.08f), (i % 2 == 0) ? a : b);
        }
        return pivo.transform;
    }

    // ======================= ANA EM MINIATURA =======================

    [MenuItem("Bug In The Neural Network/Antigos/Transformar Faisca em Ana Miniatura (substituído por Versão Cris)")]
    public static void TransformarFaiscaEmAna()
    {
        GameObject raiz = GameObject.Find("Faisca");
        if (raiz == null) { Debug.LogError("Objeto 'Faisca' não encontrado. Rode 'Montar Cena Base'."); return; }

        // Esconde a esfera, mas mantém o objeto (movimento, interação, câmera e GameManager continuam ligados a ele).
        var esfera = raiz.GetComponent<MeshRenderer>();
        if (esfera != null) esfera.enabled = false;

        // Cápsula de colisão para o tamanho da Ana (~0,95 m de altura no mundo; a raiz tem escala 0,6).
        var cc = raiz.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.radius = 0.2f / 0.6f;
            cc.height = 0.9f / 0.6f;
            cc.center = Vector3.zero;
        }
        Vector3 p = raiz.transform.position; p.y = 0.5f; raiz.transform.position = p;

        Transform antigo = raiz.transform.Find("Ana_Visual");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        Material pele = MatURP("Ana_Pele", new Color(0.93f, 0.74f, 0.60f), false);
        Material cabelo = MatURP("Ana_Cabelo", new Color(0.20f, 0.12f, 0.10f), false);
        Material moletom = MatURP("Ana_Moletom", new Color(0.55f, 0.38f, 0.75f), false);
        Material calca = MatURP("Ana_Calca", new Color(0.18f, 0.20f, 0.30f), false);
        Material olho = MatURP("Ana_Olho", new Color(0.05f, 0.05f, 0.08f), false);
        Material aura = MatURP("Ana_Aura", new Color(0.55f, 0.75f, 1f), true);

        // Filho em "metros do mundo": compensa a escala 0,6 da raiz; origem nos pés.
        GameObject visual = new GameObject("Ana_Visual");
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localScale = Vector3.one / 0.6f;
        visual.transform.localPosition = new Vector3(0f, -0.45f / 0.6f, 0f);
        Transform v = visual.transform;

        // Corpo (balança junto ao andar)
        GameObject corpo = new GameObject("Corpo");
        corpo.transform.SetParent(v, false);
        Transform c = corpo.transform;
        Bloco(c, "Torso", PrimitiveType.Capsule, new Vector3(0f, 0.53f, 0f), new Vector3(0.21f, 0.17f, 0.14f), moletom);
        Bloco(c, "Cabeca", PrimitiveType.Sphere, new Vector3(0f, 0.80f, 0f), new Vector3(0.26f, 0.26f, 0.26f), pele);
        Bloco(c, "Cabelo_Topo", PrimitiveType.Sphere, new Vector3(0f, 0.825f, -0.015f), new Vector3(0.29f, 0.27f, 0.29f), cabelo);
        Bloco(c, "Cabelo_Costas", PrimitiveType.Capsule, new Vector3(0f, 0.71f, -0.1f), new Vector3(0.22f, 0.15f, 0.09f), cabelo);
        Bloco(c, "Olho_E", PrimitiveType.Sphere, new Vector3(-0.045f, 0.80f, 0.125f), Vector3.one * 0.032f, olho);
        Bloco(c, "Olho_D", PrimitiveType.Sphere, new Vector3(0.045f, 0.80f, 0.125f), Vector3.one * 0.032f, olho);

        // Pernas e braços com pivô no quadril/ombro, para balançar.
        Transform pernaE = CriarMembro(c, "Perna_E", new Vector3(-0.07f, 0.37f, 0f), 0.36f, 0.055f, calca);
        Transform pernaD = CriarMembro(c, "Perna_D", new Vector3(0.07f, 0.37f, 0f), 0.36f, 0.055f, calca);
        Transform bracoE = CriarMembro(c, "Braco_E", new Vector3(-0.14f, 0.67f, 0f), 0.30f, 0.04f, moletom);
        Transform bracoD = CriarMembro(c, "Braco_D", new Vector3(0.14f, 0.67f, 0f), 0.30f, 0.04f, moletom);
        bracoE.localRotation = Quaternion.Euler(0f, 0f, -6f);
        bracoD.localRotation = Quaternion.Euler(0f, 0f, 6f);

        // Halo de luz no chão: a "vontade" que acompanha a Ana.
        Bloco(v, "Aura", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.7f, 0.004f, 0.7f), aura);

        var anim = raiz.GetComponent<CrisMiniatura>();
        if (anim == null) anim = raiz.AddComponent<CrisMiniatura>();
        anim.controller = cc;
        anim.corpo = c;
        anim.pernaEsquerda = pernaE; anim.pernaDireita = pernaD;
        anim.bracoEsquerdo = bracoE; anim.bracoDireito = bracoD;
        EditorUtility.SetDirty(anim);

        // A luz ("vontade") fica na altura do peito dela.
        Transform luz = raiz.transform.Find("FaiscaLuz");
        if (luz != null) luz.localPosition = new Vector3(0f, 0.4f / 0.6f, 0.15f / 0.6f);

        // Textos do menu inicial, se já existirem na cena.
        GameObject canvasObj = GameObject.Find("Canvas_UI");
        if (canvasObj != null)
        {
            foreach (var t in canvasObj.GetComponentsInChildren<Text>(true))
            {
                if (t.name == "Contexto" && t.text.Contains("Faísca"))
                    t.text = "Ana, estudante de Ciência da Computação, está sobrecarregada às vésperas de uma apresentação importante. " +
                             "Você controla uma versão em miniatura dela, dentro da própria cabeça. Explore o quarto, resolva o que está travado e ajude Ana a dar o primeiro passo.";
                else if (t.name == "Texto" && t.text.Contains("Faísca"))
                    t.text = t.text.Replace("Mover a Faísca", "Mover a Ana").Replace("é o brilho da Faísca", "é a luz que acompanha a Ana");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Ana em miniatura criada no lugar da esfera. Salve a cena (Ctrl+S).");
    }

    static Transform CriarMembro(Transform pai, string nome, Vector3 pivoPos, float comprimento, float raio, Material mat)
    {
        GameObject pivo = new GameObject(nome);
        pivo.transform.SetParent(pai, false);
        pivo.transform.localPosition = pivoPos;
        // Cápsula pendurada abaixo do pivô.
        Bloco(pivo.transform, "Malha", PrimitiveType.Capsule, new Vector3(0f, -comprimento * 0.5f, 0f),
            new Vector3(raio * 2f, comprimento * 0.5f, raio * 2f), mat);
        return pivo.transform;
    }

    // ======================= QUARTO BAGUNÇADO =======================
    // Tudo fica dentro de "Bagunca_Quarto" e sem colisores (a Ana passa por cima).
    // Rodar de novo apaga e recria a bagunça.

    static System.Random rngBagunca = new System.Random(7);
    static Transform raizBagunca;
    static readonly System.Collections.Generic.List<Collider> collidersTemp = new System.Collections.Generic.List<Collider>();
    static Material matKraft, matKraftEsc, matQueijo, matMassa, matPepperoni, matBranco, matVermelho,
                    matAluminio, matPapel, matCaneca, matCafe, matSombra, matFeijao, matVidro, matAmarelo;

    static float Jit(float amp) { return ((float)rngBagunca.NextDouble() * 2f - 1f) * amp; }
    static float Ang() { return (float)rngBagunca.NextDouble() * 360f; }

    [MenuItem("Bug In The Neural Network/Antigos/Baguncar o Quarto (layout antigo)")]
    public static void BaguncarQuarto()
    {
        ApagarSeExistir("Bagunca_Quarto");
        rngBagunca = new System.Random(7);
        salaBagunca = MeiaSalaAtual();
        raizBagunca = new GameObject("Bagunca_Quarto").transform;

        // ----- Materiais -----
        Material marinho = MatURP("Bagunca_Roupa_Marinho", new Color(0.15f, 0.20f, 0.38f), false);
        Material cinza = MatURP("Bagunca_Roupa_Cinza", new Color(0.55f, 0.55f, 0.58f), false);
        Material mostarda = MatURP("Bagunca_Roupa_Mostarda", new Color(0.85f, 0.65f, 0.20f), false);
        Material vinho = MatURP("Bagunca_Roupa_Vinho", new Color(0.50f, 0.12f, 0.18f), false);
        Material teal = MatURP("Bagunca_Roupa_Teal", new Color(0.15f, 0.50f, 0.50f), false);
        Material preto = MatURP("Bagunca_Roupa_Preto", new Color(0.10f, 0.10f, 0.12f), false);
        Material rosa = MatURP("Bagunca_Roupa_Rosa", new Color(0.85f, 0.55f, 0.62f), false);
        Material brancoRoupa = MatURP("Bagunca_Roupa_Branco", new Color(0.88f, 0.88f, 0.85f), false);
        Material jeans = MatURP("Bagunca_Roupa_Jeans", new Color(0.25f, 0.35f, 0.55f), false);
        Material toalhaAzul = MatURP("Bagunca_Toalha", new Color(0.40f, 0.65f, 0.85f), false);
        Material[] roupas = { marinho, cinza, mostarda, vinho, teal, preto, rosa, brancoRoupa, jeans };

        matKraft = MatURP("Bagunca_Kraft", new Color(0.72f, 0.55f, 0.36f), false);
        matKraftEsc = MatURP("Bagunca_KraftEscuro", new Color(0.55f, 0.40f, 0.25f), false);
        matQueijo = MatURP("Bagunca_Queijo", new Color(0.98f, 0.78f, 0.35f), false);
        matMassa = MatURP("Bagunca_Massa", new Color(0.80f, 0.55f, 0.25f), false);
        matPepperoni = MatURP("Bagunca_Pepperoni", new Color(0.70f, 0.15f, 0.10f), false);
        matBranco = MatURP("Bagunca_Branco", new Color(0.93f, 0.92f, 0.88f), false);
        matVermelho = MatURP("Bagunca_Vermelho", new Color(0.80f, 0.15f, 0.15f), false);
        matAluminio = MatURP("Bagunca_Aluminio", new Color(0.75f, 0.76f, 0.78f), false);
        matPapel = MatURP("Bagunca_Papel", new Color(0.95f, 0.95f, 0.92f), false);
        matCaneca = MatURP("Bagunca_Caneca", new Color(0.85f, 0.82f, 0.75f), false);
        matCafe = MatURP("Bagunca_Cafe", new Color(0.25f, 0.15f, 0.08f), false);
        matSombra = MatURP("Bagunca_Sombra", new Color(0.08f, 0.08f, 0.10f), false);
        matFeijao = MatURP("Bagunca_Feijao", new Color(0.40f, 0.22f, 0.14f), false);
        matVidro = MatURP("Bagunca_Vidro", new Color(0.20f, 0.45f, 0.25f), false);
        matAmarelo = MatURP("Bagunca_Amarelo", new Color(0.95f, 0.75f, 0.15f), false);
        Material lataVerde = MatURP("Bagunca_Lata_Verde", new Color(0.20f, 0.55f, 0.30f), false);
        Material lataAzul = MatURP("Bagunca_Lata_Azul", new Color(0.15f, 0.35f, 0.75f), false);

        // ----- Referências dos móveis (com valores de reserva se não existirem) -----
        GameObject cama = GameObject.Find("bedSingle");
        GameObject mesa = GameObject.Find("Mesa_PostIts");
        GameObject mesaVisual = mesa;
        if (mesa != null)
        {
            Transform vm = mesa.transform.Find("VisualModelo");
            if (vm != null) mesaVisual = vm.gameObject;
        }
        GameObject cadeira = GameObject.Find("chairDesk");

        Bounds bCama, bMesa, bCadeira;
        if (!TentarBounds(cama, out bCama)) bCama = new Bounds(new Vector3(-8.3f, 0.3f, 6f), new Vector3(2f, 0.6f, 1.2f));
        if (!TentarBounds(mesaVisual, out bMesa)) bMesa = new Bounds(new Vector3(-4f, 0.39f, -3f), new Vector3(1.6f, 0.78f, 0.9f));
        bool temCadeira = TentarBounds(cadeira, out bCadeira);

        // ----- Em cima dos móveis (altura achada por raycast em colisores temporários) -----
        try
        {
            PrepararSuperficie(cama);
            PrepararSuperficie(mesaVisual);
            PrepararSuperficie(cadeira);

            // Cama
            Camiseta("Cama_Camiseta", NaSuperficie(bCama, 0.45f, 0.35f), 35f, cinza, false);
            Camiseta("Cama_Moletom", NaSuperficie(bCama, 0.65f, 0.65f), -70f, vinho, true);
            CaixaPizza("Cama_Pizza", NaSuperficie(bCama, 0.28f, 0.70f), 15f, false, 1);
            Lata("Cama_Lata", NaSuperficie(bCama, 0.80f, 0.28f), 50f, matVermelho, true);
            Meia("Cama_Meia", NaSuperficie(bCama, 0.20f, 0.30f), 120f, brancoRoupa);

            // Mesa de tarefas
            CopoMiojo("Mesa_Miojo_1", NaSuperficie(bMesa, 0.12f, 0.35f), 0f, false);
            CopoMiojo("Mesa_Miojo_2", NaSuperficie(bMesa, 0.28f, 0.68f), 40f, false);
            Marmita("Mesa_Marmita", NaSuperficie(bMesa, 0.45f, 0.32f), 12f);
            Caneca("Mesa_Caneca_1", NaSuperficie(bMesa, 0.60f, 0.68f), 120f);
            Lata("Mesa_Lata", NaSuperficie(bMesa, 0.74f, 0.35f), 0f, lataVerde, false);
            Caneca("Mesa_Caneca_2", NaSuperficie(bMesa, 0.88f, 0.62f), -30f);
            PapelAmassado("Mesa_Papel_1", NaSuperficie(bMesa, 0.66f, 0.18f));
            PapelAmassado("Mesa_Papel_2", NaSuperficie(bMesa, 0.92f, 0.25f));

            // Cadeira: moletom largado no assento, com a manga caindo pela lateral
            if (temCadeira)
            {
                float ySeat = AlturaEm(bCadeira.center.x, bCadeira.center.z, bCadeira.center.y);
                Transform g = Grupo("Cadeira_Moletom", new Vector3(bCadeira.center.x, ySeat, bCadeira.center.z), Ang(), 4f).transform;
                Bloco(g, "Volume_1", PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.38f, 0.1f, 0.30f), teal);
                Bloco(g, "Volume_2", PrimitiveType.Sphere, new Vector3(0.06f, 0.08f, -0.04f), new Vector3(0.26f, 0.08f, 0.22f), teal);
                Bloco(raizBagunca, "Cadeira_Manga", PrimitiveType.Cube,
                    new Vector3(bCadeira.max.x - 0.03f, ySeat - 0.13f, bCadeira.center.z), new Vector3(0.08f, 0.3f, 0.12f), teal);
            }
        }
        finally
        {
            LimparSuperficies();
        }

        // ----- Chão ao lado da cama -----
        float xc = bCama.max.x;
        float zc = bCama.center.z;
        MonteDeRoupa("Chao_MonteRoupa_Cama", new Vector3(xc + 0.6f, 0f, zc - 0.3f), roupas);
        Calca("Chao_Calca_Jeans", new Vector3(xc + 1.0f, 0f, zc + 0.9f), 70f, jeans);
        CaixaPizza("Chao_Pizzas_Empilhadas", new Vector3(xc + 0.55f, 0f, bCama.min.z - 0.55f), -20f, false, 3);
        Meia("Chao_Meia_Cama", new Vector3(xc + 1.5f, 0f, zc - 1.0f), 30f, cinza);
        Kenney("pillow", new Vector3(xc + 0.45f, 0f, bCama.max.z - 0.35f), 20f, 0f, 0.55f, "x", 0f);

        // ----- Chão pelo quarto -----
        CaixaPizza("Chao_Pizza_Aberta", M(-2.2f, 0f, 2.6f), 20f, true, 1);
        Meia("Chao_Meia_1", M(-1.0f, 0f, 0.5f), 75f, brancoRoupa);
        Meia("Chao_Meia_2", M(1.8f, 0f, -1.3f), -40f, preto);
        Meia("Chao_Meia_Tapete", M(0.9f, 0.03f, -4.4f), 10f, rosa);
        Camiseta("Chao_Camiseta_1", M(2.1f, 0f, 1.6f), -30f, mostarda, false);
        Camiseta("Chao_Camiseta_2", M(7.1f, 0f, 3.6f), 60f, teal, false);
        Camiseta("Chao_Moletom", M(-5.9f, 0f, 2.9f), 110f, preto, true);
        Calca("Chao_Calca_Moletom", M(4.6f, 0f, 1.2f), -20f, cinza);
        MonteDeRoupa("Chao_MonteRoupa_Janela", M(-3.3f, 0f, 7.4f), roupas);
        Toalha("Chao_Toalha", M(6.0f, 0f, -5.6f), 15f, toalhaAzul);

        Lata("Chao_Lata_1", M(-3.0f, 0f, -0.6f), 30f, matVermelho, true);
        Lata("Chao_Lata_2", M(2.7f, 0f, 3.3f), 0f, lataVerde, false);
        Lata("Chao_Lata_3", M(5.2f, 0f, -1.8f), -60f, lataAzul, true);
        CopoMiojo("Chao_Miojo_1", M(-2.6f, 0f, -1.2f), 80f, true);
        CopoMiojo("Chao_Miojo_2", M(3.1f, 0f, 5.7f), 0f, false);
        Marmita("Chao_Marmita", M(-1.6f, 0f, 4.0f), -25f);
        SacoDelivery("Chao_Saco_Delivery", M(6.5f, 0f, 5.4f), 35f);
        Salgadinho("Chao_Salgadinho", M(1.3f, 0f, 3.9f), 55f);
        Garrafa("Chao_Garrafa", M(-5.5f, 0f, 1.2f), 20f, true);

        // ----- Lixeira transbordando, ao lado da mesa -----
        GameObject lixo = Kenney("trashcan", new Vector3(bMesa.min.x - 0.7f, 0f, bMesa.center.z - 0.3f), 0f, 0f, 0.55f, "y", 0f);
        Bounds bl;
        if (lixo != null && TentarBounds(lixo, out bl))
        {
            for (int i = 0; i < 3; i++)
                PapelAmassado("Lixo_Papel_Topo_" + i, new Vector3(bl.center.x + Jit(0.08f), bl.max.y - 0.01f + i * 0.03f, bl.center.z + Jit(0.08f)));
            for (int i = 0; i < 5; i++)
            {
                float a = Ang() * Mathf.Deg2Rad;
                float r = 0.35f + (float)rngBagunca.NextDouble() * 0.5f;
                PapelAmassado("Lixo_Papel_Chao_" + i, new Vector3(bl.center.x + Mathf.Cos(a) * r, 0f, bl.center.z + Mathf.Sin(a) * r));
            }
        }

        // ----- Caixas de papelão (delivery/mudança) e livros -----
        Kenney("cardboardBoxOpen", M(7.5f, 0f, -1.6f), 25f, 0f, 0.45f, "y", 0f);
        GameObject cx1 = Kenney("cardboardBoxClosed", M(8.3f, 0f, -3.0f), -10f, 0f, 0.40f, "y", 0f);
        Bounds bcx;
        if (cx1 != null && TentarBounds(cx1, out bcx))
            Kenney("cardboardBoxClosed", M(8.25f, 0f, -2.95f), 20f, 0f, 0.32f, "y", bcx.max.y);
        Kenney("cardboardBoxOpen", M(6.6f, 0f, 6.9f), -35f, 0f, 0.40f, "y", 0f);
        Kenney("books", M(6.9f, 0f, 1.0f), 70f, 90f, 0.20f, "y", 0f);
        Kenney("books", M(0.9f, 0f, 6.3f), -20f, 0f, 0.22f, "y", 0f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Quarto bagunçado (" + raizBagunca.childCount + " itens em 'Bagunca_Quarto'). Ajuste o que quiser na Scene view e salve a cena (Ctrl+S).");
    }

    // ----- Utilitários da bagunça -----

    static bool TentarBounds(GameObject g, out Bounds b)
    {
        b = new Bounds();
        if (g == null) return false;
        bool ok = false;
        foreach (var r in g.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!ok) { b = r.bounds; ok = true; }
            else b.Encapsulate(r.bounds);
        }
        return ok;
    }

    static void PrepararSuperficie(GameObject g)
    {
        if (g == null) return;
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
        {
            var r = mf.GetComponent<Renderer>();
            if (r == null || !r.enabled || mf.sharedMesh == null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            collidersTemp.Add(mc);
        }
        Physics.SyncTransforms();
    }

    static float AlturaEm(float x, float z, float padrao)
    {
        float melhor = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(new Vector3(x, 10f, z), Vector3.down, 20f, ~0, QueryTriggerInteraction.Collide))
            if (collidersTemp.Contains(hit.collider) && hit.point.y > melhor) melhor = hit.point.y;
        return float.IsNegativeInfinity(melhor) ? padrao : melhor;
    }

    static Vector3 NaSuperficie(Bounds b, float fx, float fz)
    {
        float x = Mathf.Lerp(b.min.x, b.max.x, fx);
        float z = Mathf.Lerp(b.min.z, b.max.z, fz);
        return new Vector3(x, AlturaEm(x, z, b.max.y), z);
    }

    static void LimparSuperficies()
    {
        foreach (var c in collidersTemp) if (c != null) Object.DestroyImmediate(c);
        collidersTemp.Clear();
    }

    static GameObject Grupo(string nome, Vector3 pos, float rotY, float inclinacao)
    {
        GameObject g = new GameObject(nome);
        g.transform.SetParent(raizBagunca, false);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(Jit(inclinacao), rotY, Jit(inclinacao));
        return g;
    }

    static GameObject BlocoR(Transform pai, string nome, PrimitiveType tipo, Vector3 pos, Vector3 escala, Vector3 rot, Material mat)
    {
        GameObject b = Bloco(pai, nome, tipo, pos, escala, mat);
        b.transform.localRotation = Quaternion.Euler(rot);
        return b;
    }

    static Transform Filho(Transform pai, string nome, Vector3 pos, Vector3 rot)
    {
        Transform t = new GameObject(nome).transform;
        t.SetParent(pai, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(rot);
        return t;
    }

    static GameObject Kenney(string arquivo, Vector3 pos, float rotY, float rotZ, float alvo, string eixo, float yBase)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FBX format/" + arquivo + ".fbx");
        if (prefab == null) { Debug.LogWarning("Modelo não encontrado: " + arquivo); return null; }
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raizBagunca);
        inst.name = "Kenney_" + arquivo;
        inst.transform.position = new Vector3(pos.x, 0f, pos.z);
        inst.transform.rotation = Quaternion.Euler(0f, rotY, rotZ);
        inst.transform.localScale = Vector3.one;
        AjustarEscala(inst, alvo, eixo, true);
        inst.transform.position += Vector3.up * yBase;
        foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        return inst;
    }

    // ----- Roupas -----

    static void Camiseta(string nome, Vector3 pos, float rotY, Material cor, bool mangaLonga)
    {
        Transform g = Grupo(nome, pos, rotY, 3f).transform;
        const float esp = 0.025f;
        Bloco(g, "Corpo", PrimitiveType.Cube, new Vector3(0f, esp * 0.5f, 0f), new Vector3(0.48f, esp, 0.6f), cor);
        float L = mangaLonga ? 0.5f : 0.2f;
        float a = mangaLonga ? 40f : 25f;
        float rad = a * Mathf.Deg2Rad;
        float cx = 0.24f + L * 0.5f * Mathf.Cos(rad) - 0.03f;
        float cz = 0.2f - L * 0.5f * Mathf.Sin(rad);
        BlocoR(g, "Manga_E", PrimitiveType.Cube, new Vector3(-cx, esp * 0.45f, cz), new Vector3(L, esp * 0.9f, 0.15f), new Vector3(0f, -a, 0f), cor);
        BlocoR(g, "Manga_D", PrimitiveType.Cube, new Vector3(cx, esp * 0.45f, cz), new Vector3(L, esp * 0.9f, 0.15f), new Vector3(0f, a, 0f), cor);
        Bloco(g, "Gola", PrimitiveType.Cylinder, new Vector3(0f, esp + 0.001f, 0.25f), new Vector3(0.13f, 0.001f, 0.07f), matSombra);
        Bloco(g, "Dobra", PrimitiveType.Sphere, new Vector3(Jit(0.1f), esp, Jit(0.15f)), new Vector3(0.3f, 0.03f, 0.15f), cor);
        if (mangaLonga)
            Bloco(g, "Capuz", PrimitiveType.Sphere, new Vector3(0f, esp * 0.8f, 0.36f), new Vector3(0.28f, 0.05f, 0.18f), cor);
    }

    static void Calca(string nome, Vector3 pos, float rotY, Material cor)
    {
        Transform g = Grupo(nome, pos, rotY, 2f).transform;
        const float esp = 0.03f;
        Bloco(g, "Cintura", PrimitiveType.Cube, new Vector3(0f, esp * 0.5f, 0.42f), new Vector3(0.4f, esp, 0.1f), cor);
        float[] lados = { -1f, 1f };
        float[] angulos = { 6f, 14f };
        for (int i = 0; i < 2; i++)
        {
            float b = angulos[i] * Mathf.Deg2Rad;
            Vector3 quadril = new Vector3(lados[i] * 0.1f, esp * 0.5f, 0.4f);
            Vector3 dir = new Vector3(lados[i] * Mathf.Sin(b), 0f, -Mathf.Cos(b));
            BlocoR(g, i == 0 ? "Perna_E" : "Perna_D", PrimitiveType.Cube, quadril + dir * 0.42f,
                new Vector3(0.18f, esp * 0.95f, 0.84f), new Vector3(0f, -lados[i] * angulos[i], 0f), cor);
        }
    }

    static void Meia(string nome, Vector3 pos, float rotY, Material cor)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        BlocoR(g, "Cano", PrimitiveType.Capsule, new Vector3(0f, 0.035f, 0f), new Vector3(0.07f, 0.1f, 0.07f), new Vector3(90f, 0f, 0f), cor);
        BlocoR(g, "Pe", PrimitiveType.Capsule, new Vector3(0.05f, 0.035f, -0.1f), new Vector3(0.07f, 0.06f, 0.07f), new Vector3(0f, 0f, 90f), cor);
    }

    static void Toalha(string nome, Vector3 pos, float rotY, Material cor)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Bloco(g, "Toalha", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(0.75f, 0.02f, 0.45f), cor);
        Bloco(g, "Dobra", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0.12f), new Vector3(0.75f, 0.02f, 0.2f), cor);
        Bloco(g, "Listra", PrimitiveType.Cube, new Vector3(0.28f, 0.0105f, 0f), new Vector3(0.06f, 0.022f, 0.45f), matBranco);
    }

    static void MonteDeRoupa(string nome, Vector3 pos, Material[] cores)
    {
        Transform g = Grupo(nome, pos, Ang(), 0f).transform;
        for (int i = 0; i < 7; i++)
        {
            Material m = cores[rngBagunca.Next(cores.Length)];
            BlocoR(g, "Peca_" + i, PrimitiveType.Sphere,
                new Vector3(Jit(0.28f), 0.05f + i * 0.025f, Jit(0.22f)),
                new Vector3(0.42f + Jit(0.1f), 0.12f + Jit(0.03f), 0.32f + Jit(0.08f)),
                new Vector3(Jit(10f), Ang(), Jit(10f)), m);
        }
        BlocoR(g, "Manga_Solta", PrimitiveType.Cube, new Vector3(0.38f, 0.02f, 0.1f), new Vector3(0.4f, 0.025f, 0.13f),
            new Vector3(0f, 30f, -8f), cores[rngBagunca.Next(cores.Length)]);
    }

    // ----- Comida e embalagens -----

    static void CaixaPizza(string nome, Vector3 pos, float rotY, bool aberta, int pilha)
    {
        Transform g = Grupo(nome, pos, rotY, 1.5f).transform;
        if (!aberta)
        {
            for (int i = 0; i < pilha; i++)
            {
                float y = i * 0.046f;
                Transform caixa = Filho(g, "Caixa_" + i, new Vector3(Jit(0.03f), y, Jit(0.03f)), new Vector3(0f, Jit(12f), 0f));
                Bloco(caixa, "Corpo", PrimitiveType.Cube, new Vector3(0f, 0.0225f, 0f), new Vector3(0.42f, 0.045f, 0.42f), matKraft);
                Bloco(caixa, "Logo", PrimitiveType.Cylinder, new Vector3(0f, 0.0455f, 0f), new Vector3(0.16f, 0.001f, 0.16f), matVermelho);
            }
            return;
        }
        Bloco(g, "Fundo", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.42f, 0.03f, 0.42f), matKraft);
        Bloco(g, "Borda", PrimitiveType.Cylinder, new Vector3(0f, 0.033f, 0f), new Vector3(0.38f, 0.005f, 0.38f), matMassa);
        Bloco(g, "Queijo", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.34f, 0.004f, 0.34f), matQueijo);
        // Uma fatia já comida: um "buraco" cor de papelão por cima.
        BlocoR(g, "Fatia_Comida", PrimitiveType.Cube, new Vector3(0.07f, 0.0395f, -0.07f), new Vector3(0.14f, 0.001f, 0.14f), new Vector3(0f, 45f, 0f), matKraftEsc);
        for (int i = 0; i < 5; i++)
        {
            float a = (40f + i * 60f) * Mathf.Deg2Rad;
            Bloco(g, "Pepperoni_" + i, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.1f, 0.0395f, Mathf.Sin(a) * 0.1f),
                new Vector3(0.05f, 0.002f, 0.05f), matPepperoni);
        }
        Transform dobradica = Filho(g, "Dobradica", new Vector3(0f, 0.03f, 0.21f), new Vector3(100f, 0f, 0f));
        Bloco(dobradica, "Tampa", PrimitiveType.Cube, new Vector3(0f, 0f, -0.21f), new Vector3(0.42f, 0.02f, 0.42f), matKraft);
    }

    static void CopoMiojo(string nome, Vector3 pos, float rotY, bool caido)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Transform c = caido ? Filho(g, "Copo", new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0f, 90f))
                            : Filho(g, "Copo", new Vector3(0f, 0.06f, 0f), Vector3.zero);
        Bloco(c, "Corpo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.1f, 0.06f, 0.1f), matBranco);
        Bloco(c, "Faixa", PrimitiveType.Cylinder, new Vector3(0f, 0.025f, 0f), new Vector3(0.103f, 0.015f, 0.103f), matVermelho);
        BlocoR(c, "Tampa", PrimitiveType.Cube, new Vector3(0.02f, 0.064f, 0f), new Vector3(0.1f, 0.004f, 0.08f), new Vector3(0f, 0f, -18f), matAluminio);
        if (caido)
            Bloco(g, "Macarrao", PrimitiveType.Sphere, new Vector3(-0.13f, 0.005f, 0f), new Vector3(0.12f, 0.012f, 0.09f), matQueijo);
    }

    static void Lata(string nome, Vector3 pos, float rotY, Material cor, bool caida)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Transform c = caida ? Filho(g, "Lata", new Vector3(0f, 0.033f, 0f), new Vector3(0f, 0f, 90f))
                            : Filho(g, "Lata", new Vector3(0f, 0.06f, 0f), Vector3.zero);
        Bloco(c, "Corpo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.066f, 0.06f, 0.066f), cor);
        Bloco(c, "Topo", PrimitiveType.Cylinder, new Vector3(0f, 0.061f, 0f), new Vector3(0.06f, 0.002f, 0.06f), matAluminio);
    }

    static void Garrafa(string nome, Vector3 pos, float rotY, bool caida)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Transform c = caida ? Filho(g, "Garrafa", new Vector3(0f, 0.0375f, 0f), new Vector3(0f, 0f, 90f))
                            : Filho(g, "Garrafa", new Vector3(0f, 0.1f, 0f), Vector3.zero);
        Bloco(c, "Corpo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.075f, 0.1f, 0.075f), matVidro);
        Bloco(c, "Gargalo", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.03f, 0.04f, 0.03f), matVidro);
    }

    static void Caneca(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Bloco(g, "Corpo", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.085f, 0.05f, 0.085f), matCaneca);
        Bloco(g, "Cafe", PrimitiveType.Cylinder, new Vector3(0f, 0.098f, 0f), new Vector3(0.075f, 0.002f, 0.075f), matCafe);
        Bloco(g, "Alca", PrimitiveType.Cube, new Vector3(0.055f, 0.05f, 0f), new Vector3(0.02f, 0.06f, 0.04f), matCaneca);
    }

    static void Marmita(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Bloco(g, "Base", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.22f, 0.06f, 0.16f), matAluminio);
        Bloco(g, "Arroz", PrimitiveType.Cube, new Vector3(-0.05f, 0.058f, 0f), new Vector3(0.1f, 0.01f, 0.14f), matBranco);
        Bloco(g, "Feijao", PrimitiveType.Cube, new Vector3(0.05f, 0.058f, 0f), new Vector3(0.1f, 0.01f, 0.14f), matFeijao);
        BlocoR(g, "Tampa", PrimitiveType.Cube, new Vector3(0.2f, 0.005f, 0.15f), new Vector3(0.23f, 0.01f, 0.17f), new Vector3(0f, 25f, 0f), matPapel);
    }

    static void SacoDelivery(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 0f).transform;
        Bloco(g, "Saco", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.26f, 0.32f, 0.16f), matKraft);
        BlocoR(g, "Dobra", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.26f, 0.05f, 0.17f), new Vector3(10f, 0f, 0f), matKraftEsc);
        Bloco(g, "Etiqueta", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0.081f), new Vector3(0.12f, 0.08f, 0.002f), matVermelho);
    }

    static void Salgadinho(string nome, Vector3 pos, float rotY)
    {
        Transform g = Grupo(nome, pos, rotY, 2f).transform;
        Bloco(g, "Pacote", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.18f, 0.04f, 0.26f), matAmarelo);
        Bloco(g, "Faixa", PrimitiveType.Cube, new Vector3(0f, 0.0205f, 0.11f), new Vector3(0.182f, 0.042f, 0.04f), matVermelho);
    }

    static void PapelAmassado(string nome, Vector3 pos)
    {
        float s = 0.07f + Jit(0.015f);
        BlocoR(raizBagunca, nome, PrimitiveType.Sphere, pos + Vector3.up * s * 0.45f,
            new Vector3(s + Jit(0.015f), s * 0.9f, s + Jit(0.015f)), new Vector3(Ang(), Ang(), Ang()), matPapel);
    }

    // ======================= DIMINUIR O QUARTO =======================
    // Sala original: 20 x 20 m (paredes em ±10). Nova: 15 x 15 m (paredes em ±7,5).
    // O que está encostado numa parede (até 2,5 m dela) continua à mesma distância dela;
    // o miolo do quarto é aproximado do centro. Rodar de novo não encolhe mais.

    const float SALA_ORIGINAL = 10f;
    const float SALA_NOVA = 7.5f;
    const float MARGEM_PAREDE = 2.5f;
    static float salaBagunca = SALA_ORIGINAL;

    static float MeiaSalaAtual()
    {
        GameObject leste = GameObject.Find("Parede_Leste");
        return leste != null ? Mathf.Abs(leste.transform.position.x) : SALA_ORIGINAL;
    }

    static float MapearEixo(float v, float H, float h)
    {
        float s = Mathf.Sign(v);
        float a = Mathf.Abs(v);
        float d = H - a;
        if (d <= MARGEM_PAREDE) return s * (h - d);
        return s * a * (h - MARGEM_PAREDE) / (H - MARGEM_PAREDE);
    }

    // Posições fixas da bagunça foram pensadas para a sala original; isto as adapta à sala atual.
    static Vector3 M(float x, float y, float z)
    {
        return new Vector3(MapearEixo(x, SALA_ORIGINAL, salaBagunca), y, MapearEixo(z, SALA_ORIGINAL, salaBagunca));
    }

    [MenuItem("Bug In The Neural Network/Antigos/Diminuir o Quarto (layout antigo)")]
    public static void DiminuirQuarto()
    {
        float H = MeiaSalaAtual();
        float h = SALA_NOVA;
        if (Mathf.Abs(H - h) < 0.01f) { Debug.Log("O quarto já está no tamanho novo."); return; }

        ApagarSeExistir("Bagunca_Quarto");

        var pulados = new System.Collections.Generic.HashSet<string> {
            "Chao_Quarto", "Parede_Norte", "Parede_Sul", "Parede_Leste", "Parede_Oeste",
            "chairDesk", "Quadro_Decor", "Canvas_UI", "EventSystem", "Global Volume" };

        GameObject mesa = GameObject.Find("Mesa_PostIts");
        Vector3 deltaMesa = Vector3.zero;
        Vector3 deltaQuadro = Vector3.zero;

        foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (pulados.Contains(raiz.name)) continue;
            if (raiz.GetComponent<Camera>() != null || raiz.GetComponent<Canvas>() != null) continue;
            Light luz = raiz.GetComponent<Light>();
            if (luz != null && luz.type == LightType.Directional) continue;

            Bounds b;
            Vector3 ancora = TentarBounds(raiz, out b) ? b.center : raiz.transform.position;
            Vector3 delta = new Vector3(MapearEixo(ancora.x, H, h) - ancora.x, 0f, MapearEixo(ancora.z, H, h) - ancora.z);
            Undo.RecordObject(raiz.transform, "Diminuir o quarto");
            raiz.transform.position += delta;
            if (raiz == mesa) deltaMesa = delta;
            if (raiz.name == "Quadro_Loop") deltaQuadro = delta;
        }

        // A cadeira acompanha a mesa; a moldura acompanha o quadro.
        MoverJunto("chairDesk", deltaMesa);
        MoverJunto("Quadro_Decor", deltaQuadro);

        // Paredes e chão.
        AjustarParede("Parede_Norte", new Vector3(0f, 0f, h), true);
        AjustarParede("Parede_Sul", new Vector3(0f, 0f, -h), true);
        AjustarParede("Parede_Leste", new Vector3(h, 0f, 0f), false);
        AjustarParede("Parede_Oeste", new Vector3(-h, 0f, 0f), false);
        GameObject chao = GameObject.Find("Chao_Quarto");
        if (chao != null)
        {
            Undo.RecordObject(chao.transform, "Diminuir o quarto");
            chao.transform.localScale = new Vector3(2f * h / 10f, 1f, 2f * h / 10f);
        }

        // Com a parede mais curta, o quadro não pode invadir a janela: encosta ele à esquerda dela.
        GameObject janela = GameObject.Find("Janela_Cortinas");
        GameObject quadro = GameObject.Find("Quadro_Loop");
        GameObject quadroDecor = GameObject.Find("Quadro_Decor");
        Bounds bj, bq;
        if (janela != null && quadro != null && TentarBounds(janela, out bj) && TentarBounds(quadro, out bq))
        {
            Bounds bd;
            if (quadroDecor != null && TentarBounds(quadroDecor, out bd)) bq.Encapsulate(bd);
            float limite = bj.min.x - 0.2f;
            if (bq.max.x > limite)
            {
                Vector3 ajuste = new Vector3(limite - bq.max.x, 0f, 0f);
                MoverJunto("Quadro_Loop", ajuste);
                MoverJunto("Quadro_Decor", ajuste);
                if (bq.min.x + ajuste.x < -h + 0.15f)
                    Debug.LogWarning("O quadro ficou encostando na parede oeste; vale diminuir a largura dele um pouco.");
            }
        }

        // Câmera: limites acompanham as paredes novas.
        var cam = Object.FindFirstObjectByType<SimpleCameraFollow>();
        if (cam != null)
        {
            Undo.RecordObject(cam, "Diminuir o quarto");
            cam.limiteX = h - 1f;
            cam.limiteZ = h - 1f;
            EditorUtility.SetDirty(cam);
        }

        // Recria a bagunça já no tamanho novo.
        BaguncarQuarto();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Quarto reduzido para " + (2f * h) + " x " + (2f * h) + " m. Confira na Scene view e salve (Ctrl+S).");
    }

    static void MoverJunto(string nome, Vector3 delta)
    {
        GameObject g = GameObject.Find(nome);
        if (g == null || delta == Vector3.zero) return;
        Undo.RecordObject(g.transform, "Diminuir o quarto");
        g.transform.position += delta;
    }

    static void AjustarParede(string nome, Vector3 posXZ, bool horizontal)
    {
        GameObject p = GameObject.Find(nome);
        if (p == null) return;
        Undo.RecordObject(p.transform, "Diminuir o quarto");
        Vector3 pos = p.transform.position;
        Vector3 esc = p.transform.localScale;
        float comprimento = 2f * SALA_NOVA + 0.3f;
        if (horizontal) { pos.z = posXZ.z; esc.x = comprimento; }
        else { pos.x = posXZ.x; esc.z = comprimento; }
        p.transform.position = pos;
        p.transform.localScale = esc;
    }

    // ======================= QUARTO DA REFERÊNCIA =======================
    // Quarto pequeno e abarrotado (inspirado na foto de referência):
    //  - parede esquerda (oeste): escrivaninha com computador, varal de fotos, mural de cortiça (Puzzle 2), cadeira rosa;
    //  - fundo (norte): estante cheia com boneco em cima, balão de coração, casaco no cabideiro,
    //    janela com cortina turquesa (final) e arara de roupas;
    //  - parede direita (leste): cama com edredom lilás e o celular em cima (Puzzle 3);
    //  - piso de madeira, pôsteres e roupa/embalagem por todo lado.
    // A parede sul fica invisível (só colisão) para a câmera enxergar o quarto "de fora", como numa casa de bonecas.

    const float REF_X = 4f;            // paredes leste/oeste em ±4 (8 m de largura)
    const float REF_Z = 3.5f;          // paredes norte/sul em ±3,5 (7 m de profundidade)
    const float REF_ALTURA = 3.8f;
    const float REF_SUL_EXTRA = 3f;    // chão e paredes laterais continuam além da parede sul (a câmera fica ali)

    static Transform raizDecor;

    [MenuItem("Bug In The Neural Network/Montar Quarto da Referencia")]
    public static void MontarQuartoReferencia()
    {
        float xi = REF_X - 0.15f;   // face interna das paredes laterais
        float zi = REF_Z - 0.15f;   // face interna da parede norte
        float zSul = -REF_Z - REF_SUL_EXTRA;

        foreach (string n in new[] { "Bagunca_Quarto", "Bagunca_Depressao", "Quarto_Decor", "Janela_Cortinas", "Quadro_Decor",
                                     "lampRoundFloor", "pottedPlant", "rugRectangle", "wallWindow" })
            ApagarSeExistir(n);
        rngBagunca = new System.Random(11);
        raizDecor = new GameObject("Quarto_Decor").transform;
        InicializarMateriaisComida();

        // ---------- Materiais ----------
        Material parede = MatURP("Ref_Parede", new Color(0.93f, 0.91f, 0.95f), false);
        Material piso = MatURP("Ref_Piso_Madeira", new Color(0.66f, 0.47f, 0.30f), false);
        Material fresta = MatURP("Ref_Piso_Fresta", new Color(0.45f, 0.30f, 0.18f), false);
        Material madeira = MatURP("Ref_Madeira_Clara", new Color(0.80f, 0.66f, 0.48f), false);
        Material metal = MatURP("Ref_Metal", new Color(0.82f, 0.82f, 0.86f), false);
        Material metalEsc = MatURP("Metal_Cortineiro", new Color(0.25f, 0.22f, 0.30f), false);
        Material turqA = MatURP("Ref_Cortina_A", new Color(0.18f, 0.70f, 0.82f), false);
        Material turqB = MatURP("Ref_Cortina_B", new Color(0.32f, 0.82f, 0.90f), false);
        Material cortica = MatURP("Ref_Cortica", new Color(0.74f, 0.55f, 0.36f), false);
        Material rosaCadeira = MatURP("Ref_Rosa_Cadeira", new Color(0.92f, 0.36f, 0.55f), false);
        Material lilas = MatURP("Ref_Edredom_Lilas", new Color(0.74f, 0.64f, 0.88f), false);
        Material balaoMat = MatURP("Ref_Balao", new Color(0.90f, 0.10f, 0.15f), false);
        Material telaPc = MatURP("Ref_Tela", new Color(0.55f, 0.75f, 1f), true);
        Material telaCel = MatURP("Ref_Tela_Celular", new Color(0.45f, 0.85f, 0.75f), true);
        Material corpoCel = MatURP("Ref_Celular", new Color(0.06f, 0.06f, 0.08f), false);
        Material ceuAlto = MatURP("Ceu_Alto", new Color(0.08f, 0.11f, 0.26f), true);
        Material ceuBaixo = MatURP("Ceu_Baixo", new Color(0.20f, 0.26f, 0.45f), true);
        Material luaMat = MatURP("Ceu_Lua", new Color(0.90f, 0.93f, 1f), true);
        Material estrelaMat = MatURP("Ceu_Estrela", new Color(0.85f, 0.90f, 1f), true);

        Material marinho = MatURP("Bagunca_Roupa_Marinho", new Color(0.15f, 0.20f, 0.38f), false);
        Material cinza = MatURP("Bagunca_Roupa_Cinza", new Color(0.55f, 0.55f, 0.58f), false);
        Material mostarda = MatURP("Bagunca_Roupa_Mostarda", new Color(0.85f, 0.65f, 0.20f), false);
        Material vinho = MatURP("Bagunca_Roupa_Vinho", new Color(0.50f, 0.12f, 0.18f), false);
        Material teal = MatURP("Bagunca_Roupa_Teal", new Color(0.15f, 0.50f, 0.50f), false);
        Material preto = MatURP("Bagunca_Roupa_Preto", new Color(0.10f, 0.10f, 0.12f), false);
        Material rosa = MatURP("Bagunca_Roupa_Rosa", new Color(0.85f, 0.55f, 0.62f), false);
        Material brancoRoupa = MatURP("Bagunca_Roupa_Branco", new Color(0.88f, 0.88f, 0.85f), false);
        Material jeans = MatURP("Bagunca_Roupa_Jeans", new Color(0.25f, 0.35f, 0.55f), false);
        Material verde = MatURP("Ref_Roupa_Verde", new Color(0.45f, 0.75f, 0.30f), false);
        Material musgo = MatURP("Ref_Roupa_Musgo", new Color(0.33f, 0.38f, 0.22f), false);
        Material toalhaAzul = MatURP("Bagunca_Toalha", new Color(0.40f, 0.65f, 0.85f), false);
        Material[] roupas = { marinho, cinza, mostarda, vinho, teal, preto, rosa, brancoRoupa, jeans, verde, rosaCadeira };
        Material[] cores = {
            MatURP("Ref_Cor_Rosa", new Color(0.95f, 0.55f, 0.70f), false),
            MatURP("Ref_Cor_Azul", new Color(0.35f, 0.60f, 0.95f), false),
            MatURP("Ref_Cor_Amarela", new Color(0.98f, 0.88f, 0.35f), false),
            MatURP("Ref_Cor_Verde", new Color(0.50f, 0.85f, 0.55f), false),
            MatURP("Ref_Cor_Roxa", new Color(0.60f, 0.40f, 0.85f), false),
            MatURP("Ref_Cor_Laranja", new Color(0.98f, 0.60f, 0.25f), false) };

        // ---------- Casca: chão, paredes ----------
        float zMeio = (REF_Z + zSul) * 0.5f;
        GameObject chao = GameObject.Find("Chao_Quarto");
        if (chao != null)
        {
            chao.transform.position = new Vector3(0f, 0f, zMeio);
            chao.transform.localScale = new Vector3(2f * REF_X / 10f, 1f, (REF_Z - zSul) / 10f);
            chao.GetComponent<Renderer>().sharedMaterial = piso;
        }
        Transform tabuas = Filho(raizDecor, "Piso_Tabuas", Vector3.zero, Vector3.zero);
        for (float x = -REF_X + 0.32f; x < REF_X; x += 0.32f)
            Bloco(tabuas, "Fresta", PrimitiveType.Cube, new Vector3(x, 0.002f, zMeio), new Vector3(0.012f, 0.002f, REF_Z - zSul), fresta);

        float hp = REF_ALTURA;
        ParedeRef("Parede_Norte", new Vector3(0f, hp / 2f, REF_Z), new Vector3(2f * REF_X + 0.3f, hp, 0.3f), parede, true);
        ParedeRef("Parede_Sul", new Vector3(0f, hp / 2f, -REF_Z), new Vector3(2f * REF_X + 0.3f, hp, 0.3f), parede, false);
        ParedeRef("Parede_Leste", new Vector3(REF_X, hp / 2f, zMeio), new Vector3(0.3f, hp, REF_Z - zSul + 0.3f), parede, true);
        ParedeRef("Parede_Oeste", new Vector3(-REF_X, hp / 2f, zMeio), new Vector3(0.3f, hp, REF_Z - zSul + 0.3f), parede, true);

        // ---------- Móveis grandes ----------
        // Escrivaninha (Puzzle 1) na parede esquerda, de frente para o quarto.
        GameObject mesa = GameObject.Find("Mesa_PostIts");
        GameObject mesaVisual = mesa;
        if (mesa != null)
        {
            Transform vm = mesa.transform.Find("VisualModelo");
            if (vm != null) mesaVisual = vm.gameObject;
            mesa.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            mesa.transform.position = new Vector3(-2.5f, mesa.transform.position.y, -0.9f);
            EncostarX(mesa, -xi + 0.01f, true);
        }
        Bounds bMesa;
        if (!TentarBounds(mesaVisual, out bMesa)) bMesa = new Bounds(new Vector3(-xi + 0.45f, 0.39f, -0.9f), new Vector3(0.9f, 0.78f, 1.6f));

        // Cadeira rosa, virada para a mesa.
        GameObject cadeira = GameObject.Find("chairDesk");
        if (cadeira != null)
        {
            cadeira.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            cadeira.transform.position = new Vector3(bMesa.max.x + 0.4f, cadeira.transform.position.y, bMesa.center.z + 0.15f);
            Pintar(cadeira, rosaCadeira);
        }

        // Estante no canto do fundo à esquerda.
        GameObject estante = GameObject.Find("bookcaseOpen");
        if (estante != null)
        {
            estante.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            estante.transform.position = new Vector3(-2.8f, estante.transform.position.y, 2.8f);
            EncostarX(estante, -xi + 0.02f, true);
            EncostarZ(estante, zi - 0.01f, false);
            ColisorPorBounds(estante);
        }

        // Cama na parede direita, cabeceira no fundo.
        GameObject cama = GameObject.Find("bedSingle");
        if (cama != null)
        {
            cama.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            cama.transform.position = new Vector3(3f, cama.transform.position.y, 2f);
            EncostarX(cama, xi - 0.01f, false);
            EncostarZ(cama, zi - 0.01f, false);
            ColisorPorBounds(cama);
        }
        Bounds bCama;
        if (!TentarBounds(cama, out bCama)) bCama = new Bounds(new Vector3(xi - 0.5f, 0.3f, zi - 1.05f), new Vector3(1f, 0.6f, 2.1f));

        // ---------- Janela com cortina turquesa (Puzzle final) ----------
        float jx = 0.2f, jl = 2.0f, jy0 = 1.0f, jh = 1.9f;
        MontarJanelaRef(jx, jl, jy0, jh, zi, madeira, metalEsc, turqA, turqB, ceuAlto, ceuBaixo, luaMat, estrelaMat);

        // ---------- Mural de cortiça (Puzzle 2) ao lado da mesa ----------
        MontarMuralRef(new Vector3(-xi + 0.03f, 1.4f, Mathf.Max(bMesa.max.z + 0.9f, 1.2f)), cortica, madeira, cores);

        // Varal de fotos sobre a escrivaninha.
        Transform varal = Filho(raizDecor, "Varal_Fotos", Vector3.zero, Vector3.zero);
        Bloco(varal, "Fio", PrimitiveType.Cube, new Vector3(-xi + 0.01f, 1.72f, bMesa.center.z), new Vector3(0.005f, 0.005f, bMesa.size.z + 0.2f), matSombra);
        int nFotos = 6;
        for (int i = 0; i < nFotos; i++)
        {
            float z = Mathf.Lerp(bMesa.min.z + 0.1f, bMesa.max.z - 0.1f, i / (float)(nFotos - 1));
            Transform f = Filho(varal, "Foto_" + i, new Vector3(-xi + 0.012f, 1.58f, z), new Vector3(Jit(8f), 0f, 0f));
            Bloco(f, "Moldura", PrimitiveType.Cube, Vector3.zero, new Vector3(0.006f, 0.2f, 0.17f), matBranco);
            Bloco(f, "Imagem", PrimitiveType.Cube, new Vector3(0.002f, 0.02f, 0f), new Vector3(0.006f, 0.13f, 0.13f), cores[i % cores.Length]);
        }

        // Pôsteres.
        Poster("Poster_Estante", new Vector3(-2.9f, 2.85f, zi), 'N', new Vector2(0.6f, 0.8f), cores[1], cores[2], cores[0]);
        Poster("Poster_Cama_Fundo", new Vector3(Mathf.Min(bCama.center.x, xi - 0.4f), 2.5f, zi), 'N', new Vector2(0.55f, 0.75f), cores[4], cores[5], cores[3]);
        Poster("Poster_Cama_1", new Vector3(xi, 1.8f, bCama.center.z + 0.3f), 'E', new Vector2(0.75f, 0.5f), cores[3], cores[0], cores[2]);
        Poster("Poster_Cama_2", new Vector3(xi, 1.75f, bCama.min.z - 0.6f), 'E', new Vector2(0.5f, 0.7f), cores[2], cores[4], cores[1]);
        Poster("Poster_Mesa", new Vector3(-xi, 1.9f, bMesa.min.z - 0.7f), 'W', new Vector2(0.55f, 0.75f), cores[5], cores[1], cores[3]);

        // Cabideiro com casaco, entre a estante e a janela.
        GameObject cabideiro = KenneyEm(raizDecor, "coatRackStanding", new Vector3(-1.9f, 0f, zi - 0.4f), 0f, 0f, 1.75f, "y", 0f);
        Bounds bCab;
        if (cabideiro != null && TentarBounds(cabideiro, out bCab))
        {
            RoupaPendurada(raizDecor, "Casaco_Cabideiro", new Vector3(bCab.center.x, bCab.max.y - 0.12f, bCab.center.z - 0.12f), 0f, musgo, 0.85f, true, true);
            RoupaPendurada(raizDecor, "Bolsa_Cabideiro", new Vector3(bCab.center.x + 0.1f, bCab.max.y - 0.2f, bCab.center.z + 0.1f), 30f, marinho, 0.35f, false, false);
        }

        // Balão de coração vermelho.
        Transform balao = Filho(raizDecor, "Balao_Coracao", new Vector3(-2.45f, 2.4f, zi - 0.6f), new Vector3(0f, 0f, 8f));
        Bloco(balao, "Lobo_E", PrimitiveType.Sphere, new Vector3(-0.085f, 0.05f, 0f), new Vector3(0.22f, 0.22f, 0.16f), balaoMat);
        Bloco(balao, "Lobo_D", PrimitiveType.Sphere, new Vector3(0.085f, 0.05f, 0f), new Vector3(0.22f, 0.22f, 0.16f), balaoMat);
        BlocoR(balao, "Ponta", PrimitiveType.Cube, new Vector3(0f, -0.06f, 0f), new Vector3(0.2f, 0.2f, 0.15f), new Vector3(0f, 0f, 45f), balaoMat);
        Bloco(balao, "Fio", PrimitiveType.Cylinder, new Vector3(0f, -0.75f, 0f), new Vector3(0.006f, 0.6f, 0.006f), matSombra);

        // Arara de roupas à direita da janela, perto da cabeceira.
        float ra = Mathf.Min(jx + jl / 2f + 0.75f, bCama.min.x - 0.7f);
        float rz = zi - 0.85f;
        Transform arara = Filho(raizDecor, "Arara_Roupas", Vector3.zero, Vector3.zero);
        for (int s = -1; s <= 1; s += 2)
        {
            Bloco(arara, "Poste", PrimitiveType.Cylinder, new Vector3(ra + s * 0.6f, 0.8f, rz), new Vector3(0.035f, 0.8f, 0.035f), metal);
            Bloco(arara, "Pe", PrimitiveType.Cube, new Vector3(ra + s * 0.6f, 0.015f, rz), new Vector3(0.06f, 0.03f, 0.45f), metal);
        }
        BlocoR(arara, "Barra", PrimitiveType.Cylinder, new Vector3(ra, 1.6f, rz), new Vector3(0.03f, 0.62f, 0.03f), new Vector3(0f, 0f, 90f), metal);
        RoupaPendurada(arara, "Jaqueta_Preta", new Vector3(ra - 0.32f, 1.62f, rz), 0f, preto, 0.62f, true, true);
        RoupaPendurada(arara, "Vestido_Verde", new Vector3(ra + 0.05f, 1.62f, rz), 90f, verde, 0.95f, false, false);
        RoupaPendurada(arara, "Blusa_Rosa", new Vector3(ra + 0.22f, 1.62f, rz), 90f, rosa, 0.5f, true, false);
        RoupaPendurada(arara, "Camisa_Branca", new Vector3(ra + 0.4f, 1.62f, rz), 90f, brancoRoupa, 0.6f, true, false);

        // ---------- Coisas em cima dos móveis ----------
        raizBagunca = new GameObject("Bagunca_Quarto").transform;
        try
        {
            PrepararSuperficie(mesaVisual);
            PrepararSuperficie(cadeira);
            PrepararSuperficie(cama);
            PrepararSuperficie(estante);

            // Escrivaninha: computador, teclado, mouse, luminária + bagunça.
            Vector3 p = NaSuperficie(bMesa, 0.32f, 0.5f);
            GameObject monitor = KenneyEm(raizDecor, "computerScreen", p, 90f, 0f, 0.42f, "y", p.y);
            Bounds bMon;
            if (monitor != null && TentarBounds(monitor, out bMon))
            {
                GameObject brilho = new GameObject("Luz_Monitor");
                brilho.transform.SetParent(raizDecor, false);
                brilho.transform.position = new Vector3(bMon.max.x + 0.3f, bMon.center.y, bMon.center.z);
                Light lm = brilho.AddComponent<Light>();
                lm.type = LightType.Point; lm.color = new Color(0.55f, 0.75f, 1f); lm.intensity = 0.8f; lm.range = 2.2f;
            }
            p = NaSuperficie(bMesa, 0.66f, 0.5f);
            KenneyEm(raizDecor, "computerKeyboard", p, 90f, 0f, 0.45f, "z", p.y);
            p = NaSuperficie(bMesa, 0.66f, 0.76f);
            KenneyEm(raizDecor, "computerMouse", p, 90f, 0f, 0.035f, "y", p.y);
            p = NaSuperficie(bMesa, 0.22f, 0.9f);
            GameObject luminaria = KenneyEm(raizDecor, "lampRoundTable", p, 0f, 0f, 0.4f, "y", p.y);
            Bounds bLum;
            if (luminaria != null && TentarBounds(luminaria, out bLum))
            {
                GameObject luzLum = new GameObject("Luz_Luminaria");
                luzLum.transform.SetParent(raizDecor, false);
                luzLum.transform.position = new Vector3(bLum.center.x + 0.1f, bLum.max.y - 0.08f, bLum.center.z);
                Light ll = luzLum.AddComponent<Light>();
                ll.type = LightType.Point; ll.color = new Color(1f, 0.8f, 0.55f); ll.intensity = 1.4f; ll.range = 3.2f;
            }
            MonteDeRoupa("Mesa_MonteRoupa", NaSuperficie(bMesa, 0.5f, 0.14f), roupas);
            CopoMiojo("Mesa_Miojo", NaSuperficie(bMesa, 0.82f, 0.9f), 20f, false);
            Caneca("Mesa_Caneca", NaSuperficie(bMesa, 0.82f, 0.32f), 120f);
            Lata("Mesa_Lata", NaSuperficie(bMesa, 0.55f, 0.88f), 0f, matVermelho, false);
            PapelAmassado("Mesa_Papel_1", NaSuperficie(bMesa, 0.45f, 0.3f));
            PapelAmassado("Mesa_Papel_2", NaSuperficie(bMesa, 0.88f, 0.62f));

            // Cadeira: moletom largado no assento, manga caindo.
            Bounds bCad;
            if (TentarBounds(cadeira, out bCad))
            {
                float ySeat = AlturaEm(bCad.center.x, bCad.center.z, bCad.center.y);
                Transform g = Grupo("Cadeira_Moletom", new Vector3(bCad.center.x, ySeat, bCad.center.z), Ang(), 4f).transform;
                Bloco(g, "Volume_1", PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.38f, 0.1f, 0.30f), teal);
                Bloco(g, "Volume_2", PrimitiveType.Sphere, new Vector3(0.06f, 0.08f, -0.04f), new Vector3(0.26f, 0.08f, 0.22f), rosa);
                Bloco(raizBagunca, "Cadeira_Manga", PrimitiveType.Cube,
                    new Vector3(bCad.center.x, ySeat - 0.13f, bCad.min.z + 0.03f), new Vector3(0.12f, 0.3f, 0.08f), teal);
            }

            // Cama: edredom lilás, travesseiros, roupas e o celular (Puzzle 3).
            float zCama(float f) { return Mathf.Lerp(bCama.min.z, bCama.max.z, f); }
            float yColchao = AlturaEm(bCama.center.x, zCama(0.33f), bCama.center.y);
            float compEdredom = bCama.size.z * 0.62f;
            GameObject edredom = Bloco(raizDecor, "Edredom", PrimitiveType.Cube,
                new Vector3(bCama.center.x, yColchao + 0.03f, zCama(0.33f)), new Vector3(bCama.size.x * 0.96f, 0.06f, compEdredom), lilas);
            Bloco(raizDecor, "Edredom_Aba", PrimitiveType.Cube,
                new Vector3(bCama.min.x - 0.005f, yColchao - 0.12f, zCama(0.33f)), new Vector3(0.03f, 0.3f, compEdredom), lilas);
            float yTravesseiro = AlturaEm(bCama.center.x, zCama(0.85f), yColchao);
            KenneyEm(raizDecor, "pillowBlue", new Vector3(bCama.center.x, 0f, zCama(0.86f)), 0f, 0f, bCama.size.x * 0.7f, "x", yTravesseiro);
            KenneyEm(raizDecor, "pillow", new Vector3(bCama.center.x - 0.05f, 0f, zCama(0.76f)), 12f, 0f, bCama.size.x * 0.55f, "x", yTravesseiro + 0.06f);
            PrepararSuperficie(edredom);

            GameObject celular = GameObject.Find("Celular_Firewall");
            if (celular != null)
            {
                foreach (string filho in new[] { "VisualModelo", "Tela" })
                {
                    Transform t = celular.transform.Find(filho);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                Vector3 pc = NaSuperficie(bCama, 0.35f, 0.42f);
                celular.transform.position = pc + Vector3.up * 0.012f;
                celular.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                celular.transform.localScale = new Vector3(0.14f, 0.02f, 0.28f);
                var mrCel = celular.GetComponent<MeshRenderer>();
                if (mrCel != null) { mrCel.enabled = true; mrCel.sharedMaterial = corpoCel; }
                Bloco(celular.transform, "Tela", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(0.86f, 0.1f, 0.9f), telaCel);
            }

            Camiseta("Cama_Camiseta", NaSuperficie(bCama, 0.6f, 0.15f), 35f, mostarda, false);
            Camiseta("Cama_Moletom", NaSuperficie(bCama, 0.55f, 0.62f), -70f, vinho, true);
            Calca("Cama_Calca", NaSuperficie(bCama, 0.3f, 0.2f), 160f, jeans);
            Lata("Cama_Lata", NaSuperficie(bCama, 0.8f, 0.42f), 50f, matVermelho, true);
            Meia("Cama_Meia", NaSuperficie(bCama, 0.2f, 0.55f), 120f, brancoRoupa);

            // Estante cheia: livros em cada prateleira + o boneco em cima.
            Bounds bEst;
            if (TentarBounds(estante, out bEst))
            {
                EncherEstante(bEst);
                KenneyEm(raizDecor, "bear", new Vector3(bEst.center.x, 0f, bEst.center.z), 180f, 0f, 0.35f, "y", bEst.max.y);
            }
        }
        finally
        {
            LimparSuperficies();
        }

        // ---------- Bagunça no chão ----------
        // Perto da mesa e da cadeira
        MonteDeRoupa("Chao_Monte_Mesa_1", new Vector3(bMesa.max.x + 0.35f, 0f, bMesa.min.z - 0.45f), roupas);
        MonteDeRoupa("Chao_Monte_Mesa_2", new Vector3(bMesa.max.x + 0.25f, 0f, bMesa.max.z + 0.5f), roupas);
        CaixaPizza("Chao_Pizzas_Empilhadas", new Vector3(-xi + 0.35f, 0f, bMesa.min.z - 1.1f), -15f, false, 3);
        GameObject lixo = Kenney("trashcan", new Vector3(-xi + 0.35f, 0f, bMesa.min.z - 0.35f), 0f, 0f, 0.5f, "y", 0f);
        Bounds bl;
        if (lixo != null && TentarBounds(lixo, out bl))
        {
            for (int i = 0; i < 3; i++)
                PapelAmassado("Lixo_Papel_Topo_" + i, new Vector3(bl.center.x + Jit(0.08f), bl.max.y - 0.01f + i * 0.03f, bl.center.z + Jit(0.08f)));
            for (int i = 0; i < 4; i++)
            {
                float a = (40f + i * 70f) * Mathf.Deg2Rad;
                PapelAmassado("Lixo_Papel_Chao_" + i, new Vector3(bl.center.x + 0.3f + Mathf.Cos(a) * 0.35f, 0f, bl.center.z + Mathf.Sin(a) * 0.35f));
            }
        }
        Garrafa("Chao_Garrafa", new Vector3(-1.9f, 0f, -2.7f), 20f, true);

        // Meio do quarto
        CaixaPizza("Chao_Pizza_Aberta", new Vector3(-0.8f, 0f, 0.6f), 20f, true, 1);
        Camiseta("Chao_Camiseta_1", new Vector3(-1.3f, 0f, 1.9f), -30f, cinza, false);
        Camiseta("Chao_Camiseta_2", new Vector3(1.3f, 0f, -0.2f), 60f, teal, false);
        Calca("Chao_Calca", new Vector3(0.9f, 0f, -1.5f), -20f, preto);
        Meia("Chao_Meia_1", new Vector3(0.3f, 0f, -0.6f), 75f, brancoRoupa);
        Meia("Chao_Meia_2", new Vector3(1.6f, 0f, 0.9f), -40f, rosa);
        Meia("Chao_Meia_3", new Vector3(-0.4f, 0f, -2.6f), 10f, cinza);
        Lata("Chao_Lata_1", new Vector3(-1.6f, 0f, -1.3f), 30f, matVermelho, true);
        Lata("Chao_Lata_2", new Vector3(2.2f, 0f, -1.2f), 0f, matVidro, false);
        CopoMiojo("Chao_Miojo", new Vector3(0.8f, 0f, 1.5f), 80f, true);
        Marmita("Chao_Marmita", new Vector3(-0.2f, 0f, 2.1f), -25f);
        Salgadinho("Chao_Salgadinho", new Vector3(0.4f, 0f, 0.7f), 55f);
        Toalha("Chao_Toalha", new Vector3(1.8f, 0f, -2.6f), 15f, toalhaAzul);

        // Lado da cama e fundo
        MonteDeRoupa("Chao_Monte_Cama", new Vector3(bCama.min.x - 0.1f, 0f, bCama.min.z - 0.5f), roupas);
        MonteDeRoupa("Chao_Monte_Arara", new Vector3(ra - 0.1f, 0f, rz - 0.55f), roupas);
        SacoDelivery("Chao_Saco_Delivery", new Vector3(xi - 0.35f, 0f, bCama.min.z - 1.4f), 35f);
        GameObject caixa = Kenney("cardboardBoxClosed", new Vector3(xi - 0.45f, 0f, -2.6f), -10f, 0f, 0.4f, "y", 0f);
        Bounds bcx;
        if (caixa != null && TentarBounds(caixa, out bcx))
            Kenney("cardboardBoxClosed", new Vector3(xi - 0.42f, 0f, -2.55f), 20f, 0f, 0.32f, "y", bcx.max.y);
        Kenney("cardboardBoxOpen", new Vector3(xi - 1.3f, 0f, -2.9f), 25f, 0f, 0.42f, "y", 0f);
        Kenney("books", new Vector3(-2.3f, 0f, 2.0f), 70f, 90f, 0.2f, "y", 0f);
        Kenney("cardboardBoxOpen", new Vector3(-1.05f, 0f, zi - 0.45f), -20f, 0f, 0.4f, "y", 0f);

        // ---------- Ana e câmera ----------
        GameObject ana = GameObject.Find("Faisca");
        if (ana != null) ana.transform.position = new Vector3(0.3f, ana.transform.position.y, -2.0f);

        var cam = Object.FindFirstObjectByType<SimpleCameraFollow>();
        if (cam != null)
        {
            cam.deslocamento = new Vector3(0f, 3.3f, -3.6f);
            cam.limiteX = xi - 0.7f;
            cam.limiteZ = zi - 1f;
            cam.limiteZSul = zSul + 0.5f;
            if (ana != null)
            {
                cam.transform.position = ana.transform.position + cam.deslocamento;
                cam.transform.LookAt(ana.transform.position + Vector3.up);
            }
            EditorUtility.SetDirty(cam);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Quarto da referência montado. Confira na Scene/Game view e salve a cena (Ctrl+S).");
    }

    // ----- Utilitários do quarto da referência -----

    static void InicializarMateriaisComida()
    {
        matKraft = MatURP("Bagunca_Kraft", new Color(0.72f, 0.55f, 0.36f), false);
        matKraftEsc = MatURP("Bagunca_KraftEscuro", new Color(0.55f, 0.40f, 0.25f), false);
        matQueijo = MatURP("Bagunca_Queijo", new Color(0.98f, 0.78f, 0.35f), false);
        matMassa = MatURP("Bagunca_Massa", new Color(0.80f, 0.55f, 0.25f), false);
        matPepperoni = MatURP("Bagunca_Pepperoni", new Color(0.70f, 0.15f, 0.10f), false);
        matBranco = MatURP("Bagunca_Branco", new Color(0.93f, 0.92f, 0.88f), false);
        matVermelho = MatURP("Bagunca_Vermelho", new Color(0.80f, 0.15f, 0.15f), false);
        matAluminio = MatURP("Bagunca_Aluminio", new Color(0.75f, 0.76f, 0.78f), false);
        matPapel = MatURP("Bagunca_Papel", new Color(0.95f, 0.95f, 0.92f), false);
        matCaneca = MatURP("Bagunca_Caneca", new Color(0.85f, 0.82f, 0.75f), false);
        matCafe = MatURP("Bagunca_Cafe", new Color(0.25f, 0.15f, 0.08f), false);
        matSombra = MatURP("Bagunca_Sombra", new Color(0.08f, 0.08f, 0.10f), false);
        matFeijao = MatURP("Bagunca_Feijao", new Color(0.40f, 0.22f, 0.14f), false);
        matVidro = MatURP("Bagunca_Vidro", new Color(0.20f, 0.45f, 0.25f), false);
        matAmarelo = MatURP("Bagunca_Amarelo", new Color(0.95f, 0.75f, 0.15f), false);
    }

    static void ParedeRef(string nome, Vector3 pos, Vector3 esc, Material mat, bool visivel)
    {
        GameObject p = GameObject.Find(nome);
        if (p == null) { p = GameObject.CreatePrimitive(PrimitiveType.Cube); p.name = nome; }
        p.transform.position = pos;
        p.transform.rotation = Quaternion.identity;
        p.transform.localScale = esc;
        var r = p.GetComponent<Renderer>();
        if (r != null) { r.sharedMaterial = mat; r.enabled = visivel; }
    }

    // Encosta o objeto numa parede: em X (lado mínimo = parede oeste, máximo = leste).
    static void EncostarX(GameObject g, float parede, bool ladoMinimo)
    {
        Bounds b;
        if (!TentarBounds(g, out b)) return;
        float dx = ladoMinimo ? parede - b.min.x : parede - b.max.x;
        g.transform.position += new Vector3(dx, 0f, 0f);
    }

    static void EncostarZ(GameObject g, float parede, bool ladoMinimo)
    {
        Bounds b;
        if (!TentarBounds(g, out b)) return;
        float dz = ladoMinimo ? parede - b.min.z : parede - b.max.z;
        g.transform.position += new Vector3(0f, 0f, dz);
    }

    static void ColisorPorBounds(GameObject g)
    {
        Bounds b;
        if (!TentarBounds(g, out b)) return;
        var antigo = g.GetComponent<BoxCollider>();
        if (antigo != null) Object.DestroyImmediate(antigo);
        var bc = g.AddComponent<BoxCollider>();
        bc.center = g.transform.InverseTransformPoint(b.center);
        Vector3 s = g.transform.InverseTransformVector(b.size);
        bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }

    static void Pintar(GameObject g, Material mat)
    {
        foreach (var r in g.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static GameObject KenneyEm(Transform pai, string arquivo, Vector3 pos, float rotY, float rotZ, float alvo, string eixo, float yBase)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FBX format/" + arquivo + ".fbx");
        if (prefab == null) { Debug.LogWarning("Modelo não encontrado: " + arquivo); return null; }
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        inst.name = "Kenney_" + arquivo;
        inst.transform.position = new Vector3(pos.x, 0f, pos.z);
        inst.transform.rotation = Quaternion.Euler(0f, rotY, rotZ);
        inst.transform.localScale = Vector3.one;
        AjustarEscala(inst, alvo, eixo, true);
        inst.transform.position += Vector3.up * yBase;
        foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        return inst;
    }

    // Pôster numa parede: 'N' (fundo), 'E' (direita) ou 'W' (esquerda). "pos" fica na face da parede.
    static void Poster(string nome, Vector3 pos, char parede, Vector2 tam, Material fundo, Material det1, Material det2)
    {
        float rotY = parede == 'N' ? 180f : (parede == 'E' ? -90f : 90f);
        Transform g = Filho(raizDecor, nome, pos, new Vector3(0f, rotY, Jit(3f)));
        Bloco(g, "Fundo", PrimitiveType.Cube, new Vector3(0f, 0f, 0.004f), new Vector3(tam.x, tam.y, 0.008f), fundo);
        float d = Mathf.Min(tam.x, tam.y) * 0.55f;
        BlocoR(g, "Circulo", PrimitiveType.Cylinder, new Vector3(0f, tam.y * 0.12f, 0.009f), new Vector3(d, 0.002f, d), new Vector3(90f, 0f, 0f), det1);
        Bloco(g, "Faixa", PrimitiveType.Cube, new Vector3(0f, -tam.y * 0.32f, 0.009f), new Vector3(tam.x * 0.8f, tam.y * 0.08f, 0.004f), det2);
        Bloco(g, "Faixa_2", PrimitiveType.Cube, new Vector3(-tam.x * 0.1f, -tam.y * 0.42f, 0.009f), new Vector3(tam.x * 0.6f, tam.y * 0.04f, 0.004f), det2);
    }

    static void RoupaPendurada(Transform pai, string nome, Vector3 gancho, float rotY, Material cor, float comprimento, bool mangas, bool capuz)
    {
        Transform g = new GameObject(nome).transform;
        g.SetParent(pai, false);
        g.position = gancho;
        g.rotation = Quaternion.Euler(Jit(3f), rotY, Jit(4f));
        Bloco(g, "Gancho", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.01f, 0.03f, 0.01f), matSombra);
        Bloco(g, "Cabide", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), new Vector3(0.4f, 0.015f, 0.015f), matSombra);
        Bloco(g, "Corpo", PrimitiveType.Cube, new Vector3(0f, -0.05f - comprimento * 0.5f, 0f), new Vector3(0.42f, comprimento, 0.1f), cor);
        if (mangas)
        {
            float lm = Mathf.Min(0.55f, comprimento * 0.9f);
            Bloco(g, "Manga_E", PrimitiveType.Cube, new Vector3(-0.25f, -0.07f - lm * 0.5f, 0f), new Vector3(0.1f, lm, 0.1f), cor);
            Bloco(g, "Manga_D", PrimitiveType.Cube, new Vector3(0.25f, -0.07f - lm * 0.5f, 0f), new Vector3(0.1f, lm, 0.1f), cor);
        }
        if (capuz)
            Bloco(g, "Capuz", PrimitiveType.Sphere, new Vector3(0f, -0.08f, -0.05f), new Vector3(0.26f, 0.2f, 0.12f), cor);
    }

    // Procura as prateleiras de cima para baixo (raycast nos colisores temporários) e coloca livros nelas.
    static void EncherEstante(Bounds b)
    {
        float[] colunas = { 0.28f, 0.72f };
        foreach (float fx in colunas)
        {
            float x = Mathf.Lerp(b.min.x, b.max.x, fx);
            float y = b.max.y - 0.04f;
            for (int prateleira = 0; prateleira < 6; prateleira++)
            {
                float yHit = AlturaAbaixo(x, y, b.center.z);
                if (float.IsNegativeInfinity(yHit) || yHit < 0.05f) break;
                float vao = y - yHit;
                if (vao > 0.12f)
                {
                    float altura = Mathf.Min(0.26f, vao * 0.75f);
                    if (rngBagunca.NextDouble() < 0.8)
                        KenneyEm(raizDecor, "books", new Vector3(x, 0f, b.center.z), 180f + Jit(10f), 0f, altura, "y", yHit);
                    else
                        PapelAmassado("Estante_Papel_" + prateleira, new Vector3(x, yHit, b.center.z));
                }
                y = yHit - 0.03f;
            }
        }
    }

    static float AlturaAbaixo(float x, float yInicio, float z)
    {
        float melhor = float.NegativeInfinity;
        foreach (var hit in Physics.RaycastAll(new Vector3(x, yInicio, z), Vector3.down, yInicio + 1f, ~0, QueryTriggerInteraction.Collide))
            if (collidersTemp.Contains(hit.collider) && hit.point.y > melhor) melhor = hit.point.y;
        return melhor;
    }

    static void MontarJanelaRef(float jx, float jl, float jy0, float jh, float zi, Material madeira, Material metal,
                                Material cortA, Material cortB, Material ceuAlto, Material ceuBaixo, Material lua, Material estrela)
    {
        GameObject raiz = new GameObject("Janela_Cortinas");
        Transform r = raiz.transform;
        float jyc = jy0 + jh * 0.5f;

        var rAlto = Bloco(r, "Ceu_Alto", PrimitiveType.Cube, new Vector3(jx, jy0 + jh * 0.75f, zi - 0.02f), new Vector3(jl, jh * 0.5f, 0.02f), ceuAlto);
        var rBaixo = Bloco(r, "Ceu_Baixo", PrimitiveType.Cube, new Vector3(jx, jy0 + jh * 0.25f, zi - 0.02f), new Vector3(jl, jh * 0.5f, 0.02f), ceuBaixo);
        var rLua = Bloco(r, "Lua", PrimitiveType.Sphere, new Vector3(jx - jl * 0.25f, jy0 + jh * 0.8f, zi - 0.04f), new Vector3(0.32f, 0.32f, 0.03f), lua);
        var estrelas = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < 7; i++)
        {
            Vector3 pe = new Vector3(jx + Jit(jl * 0.45f), jy0 + jh * 0.5f + (float)rngBagunca.NextDouble() * jh * 0.45f, zi - 0.04f);
            estrelas.Add(Bloco(r, "Estrela_" + i, PrimitiveType.Cube, pe, new Vector3(0.05f, 0.05f, 0.02f), estrela).GetComponent<Renderer>());
        }

        Bloco(r, "Moldura_Topo", PrimitiveType.Cube, new Vector3(jx, jy0 + jh + 0.07f, zi - 0.1f), new Vector3(jl + 0.28f, 0.14f, 0.2f), madeira);
        Bloco(r, "Moldura_Esq", PrimitiveType.Cube, new Vector3(jx - jl * 0.5f - 0.07f, jyc, zi - 0.1f), new Vector3(0.14f, jh + 0.28f, 0.2f), madeira);
        Bloco(r, "Moldura_Dir", PrimitiveType.Cube, new Vector3(jx + jl * 0.5f + 0.07f, jyc, zi - 0.1f), new Vector3(0.14f, jh + 0.28f, 0.2f), madeira);
        Bloco(r, "Divisoria_V", PrimitiveType.Cube, new Vector3(jx, jyc, zi - 0.06f), new Vector3(0.06f, jh, 0.12f), madeira);
        Bloco(r, "Divisoria_H", PrimitiveType.Cube, new Vector3(jx, jyc, zi - 0.06f), new Vector3(jl, 0.06f, 0.12f), madeira);
        Bloco(r, "Peitoril", PrimitiveType.Cube, new Vector3(jx, jy0 - 0.05f, zi - 0.2f), new Vector3(jl + 0.5f, 0.08f, 0.45f), madeira);

        float yVarao = jy0 + jh + 0.35f;
        float meioVarao = jl * 0.5f + 0.6f;
        Bloco(r, "Varao", PrimitiveType.Cube, new Vector3(jx, yVarao, zi - 0.32f), new Vector3(meioVarao * 2f, 0.06f, 0.06f), metal);
        Bloco(r, "Ponteira_Esq", PrimitiveType.Sphere, new Vector3(jx - meioVarao, yVarao, zi - 0.32f), Vector3.one * 0.15f, metal);
        Bloco(r, "Ponteira_Dir", PrimitiveType.Sphere, new Vector3(jx + meioVarao, yVarao, zi - 0.32f), Vector3.one * 0.15f, metal);

        // Cortinas compridas, do varão até quase o chão.
        float largura = meioVarao + 0.05f;
        float alturaCortina = yVarao - 0.15f;
        Transform esq = CriarCortinaRef(r, "Cortina_Esq", new Vector3(jx - meioVarao + 0.1f, 0f, zi - 0.32f), 1f, largura, alturaCortina, cortA, cortB);
        Transform dir = CriarCortinaRef(r, "Cortina_Dir", new Vector3(jx + meioVarao - 0.1f, 0f, zi - 0.32f), -1f, largura, alturaCortina, cortA, cortB);

        GameObject luzObj = new GameObject("Luz_Janela");
        luzObj.transform.SetParent(r, false);
        luzObj.transform.localPosition = new Vector3(jx, jyc, zi - 0.9f);
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.55f, 0.65f, 1f);
        luz.intensity = 2.5f;
        luz.range = 6f;

        GameObject cortinaObj = GameObject.Find("Cortina");
        if (cortinaObj == null) { Debug.LogWarning("Objeto 'Cortina' (Puzzle final) não encontrado."); return; }
        var mr = cortinaObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        cortinaObj.transform.position = new Vector3(jx, 1.5f, zi - 0.32f);
        cortinaObj.transform.rotation = Quaternion.identity;
        cortinaObj.transform.localScale = new Vector3(meioVarao * 2f, 3f, 0.5f);

        var final = cortinaObj.GetComponent<PuzzleFinal>();
        if (final != null)
        {
            final.cortinaEsquerda = esq;
            final.cortinaDireita = dir;
            final.ceu = new Renderer[] { rAlto.GetComponent<Renderer>(), rBaixo.GetComponent<Renderer>(), rLua.GetComponent<Renderer>() };
            final.coresCeuFinal = new Color[] { new Color(0.95f, 0.71f, 0.36f), new Color(0.99f, 0.89f, 0.65f), new Color(1f, 0.95f, 0.78f) };
            final.estrelas = estrelas.ToArray();
            final.luzJanela = luz;
            EditorUtility.SetDirty(final);
        }
    }

    static Transform CriarCortinaRef(Transform pai, string nome, Vector3 pivo, float sentido, float largura, float altura, Material a, Material b)
    {
        GameObject g = new GameObject(nome);
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pivo;
        const int pregas = 8;
        float w = largura / pregas;
        for (int i = 0; i < pregas; i++)
        {
            float zOff = (i % 2 == 0) ? 0.035f : -0.035f;
            Bloco(g.transform, "Prega_" + i, PrimitiveType.Cube,
                new Vector3(sentido * (i + 0.5f) * w, 0.1f + altura * 0.5f, zOff), new Vector3(w, altura, 0.06f), (i % 2 == 0) ? a : b);
        }
        return g.transform;
    }

    // Mural de cortiça na parede esquerda (é o "Quadro" do Puzzle 2).
    static void MontarMuralRef(Vector3 pos, Material cortica, Material madeira, Material[] cores)
    {
        GameObject quadro = GameObject.Find("Quadro_Loop");
        if (quadro != null)
        {
            Transform vis = quadro.transform.Find("VisualModelo");
            if (vis != null) Object.DestroyImmediate(vis.gameObject);
            quadro.transform.position = pos;
            quadro.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            quadro.transform.localScale = new Vector3(1.6f, 1.05f, 0.05f);
            var mr = quadro.GetComponent<MeshRenderer>();
            if (mr != null) { mr.enabled = true; mr.sharedMaterial = cortica; }
        }
        else Debug.LogWarning("Quadro_Loop (Puzzle 2) não encontrado.");

        // Filhos em coordenadas locais do mural: x = largura, y = altura, +z = para dentro do quarto.
        GameObject deco = new GameObject("Quadro_Decor");
        deco.transform.position = pos;
        deco.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Transform d = deco.transform;

        Bloco(d, "Moldura_Topo", PrimitiveType.Cube, new Vector3(0f, 0.56f, 0f), new Vector3(1.7f, 0.07f, 0.08f), madeira);
        Bloco(d, "Moldura_Base", PrimitiveType.Cube, new Vector3(0f, -0.56f, 0f), new Vector3(1.7f, 0.07f, 0.08f), madeira);
        Bloco(d, "Moldura_Esq", PrimitiveType.Cube, new Vector3(-0.83f, 0f, 0f), new Vector3(0.07f, 1.19f, 0.08f), madeira);
        Bloco(d, "Moldura_Dir", PrimitiveType.Cube, new Vector3(0.83f, 0f, 0f), new Vector3(0.07f, 1.19f, 0.08f), madeira);

        // Folha com o "código" do laço (o pensamento que se repete).
        Transform folha = Filho(d, "Folha_Codigo", new Vector3(-0.35f, 0.02f, 0.03f), new Vector3(0f, 0f, 3f));
        Bloco(folha, "Papel", PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.62f, 0.004f), matPapel);
        float[] larg = { 0.36f, 0.3f, 0.3f, 0.12f, 0.22f };
        float[] indent = { 0f, 0.05f, 0.05f, 0f, 0f };
        for (int i = 0; i < larg.Length; i++)
            Bloco(folha, "Linha_" + i, PrimitiveType.Cube, new Vector3(-0.19f + indent[i] + larg[i] * 0.5f, 0.2f - i * 0.1f, 0.003f), new Vector3(larg[i], 0.018f, 0.002f), matSombra);
        Bloco(folha, "Alfinete", PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0.01f), Vector3.one * 0.03f, matVermelho);

        // Fotos e post-its.
        (Vector2 p, Vector2 t, float rot, bool foto)[] itens = {
            (new Vector2(0.18f, 0.3f), new Vector2(0.24f, 0.18f), -4f, true),
            (new Vector2(0.52f, 0.26f), new Vector2(0.2f, 0.26f), 6f, true),
            (new Vector2(0.3f, -0.1f), new Vector2(0.26f, 0.2f), -2f, true),
            (new Vector2(0.62f, -0.25f), new Vector2(0.16f, 0.16f), 8f, false),
            (new Vector2(0.05f, -0.34f), new Vector2(0.15f, 0.15f), -7f, false),
            (new Vector2(-0.68f, 0.38f), new Vector2(0.13f, 0.13f), 5f, false),
        };
        for (int i = 0; i < itens.Length; i++)
        {
            var it = itens[i];
            Transform t = Filho(d, "Item_" + i, new Vector3(it.p.x, it.p.y, 0.03f), new Vector3(0f, 0f, it.rot));
            Material cor = cores[i % cores.Length];
            if (it.foto)
            {
                Bloco(t, "Moldura", PrimitiveType.Cube, Vector3.zero, new Vector3(it.t.x, it.t.y, 0.004f), matBranco);
                Bloco(t, "Imagem", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0.003f), new Vector3(it.t.x * 0.82f, it.t.y * 0.7f, 0.002f), cor);
            }
            else Bloco(t, "PostIt", PrimitiveType.Cube, Vector3.zero, new Vector3(it.t.x, it.t.y, 0.004f), cor);
            Bloco(t, "Alfinete", PrimitiveType.Sphere, new Vector3(0f, it.t.y * 0.4f, 0.01f), Vector3.one * 0.03f, matVermelho);
        }
    }
}
