#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Roteiro de teste do jogo em Play mode (só existe no Editor, não entra na build).
/// É criado pela ferramenta TestePlayMode (Assets/Editor) e faz o que um jogador faria:
/// verifica se Cris alcança cada puzzle andando, se o E pegaria o objeto certo,
/// resolve os 3 puzzles, força um apagão e abre a cortina. Grava um relatório e prints.
/// </summary>
public class TesteAutomaticoCris : MonoBehaviour
{
    public string pastaSaida;

    readonly StringBuilder relatorio = new StringBuilder();
    int falhas;
    GameObject ana;
    CharacterController cc;
    ProximityInteractor interactor;

    void Start() { StartCoroutine(Rodar()); }

    void Ok(string msg) { relatorio.AppendLine("[OK]    " + msg); }
    void Falha(string msg) { falhas++; relatorio.AppendLine("[FALHA] " + msg); }
    void Info(string msg) { relatorio.AppendLine("        " + msg); }
    void Checar(bool cond, string msg) { if (cond) Ok(msg); else Falha(msg); }

    IEnumerator Rodar()
    {
        yield return null;
        yield return null;

        // ---------- Menu inicial ----------
        var tela = FindFirstObjectByType<TelaInicial>();
        Checar(tela != null && Time.timeScale == 0f, "Menu inicial aberto e jogo pausado");
        var menu = FindFirstObjectByType<MenuNeural>();
        if (menu != null)
        {
            yield return new WaitForSecondsRealtime(3f);   // espera a entrada em sequência (jogo pausado)
            int neuronios = menu.areaRede != null ? menu.areaRede.GetComponentsInChildren<UnityEngine.UI.Image>().Length : 0;
            Checar(neuronios > 20, "Menu: rede neural animada criada (" + neuronios + " elementos)");
            bool todosVisiveis = true;
            foreach (var g in menu.entrada) if (g != null && g.alpha < 0.99f) todosVisiveis = false;
            Checar(todosVisiveis, "Menu: todos os elementos terminaram a animação de entrada");
            SalvarComUI("play_0_menu.png");
        }
        if (tela != null && tela.botaoComecar != null) tela.botaoComecar.onClick.Invoke();
        else Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(1.2f);   // fade de saída do menu
        Checar(Time.timeScale == 1f, "Botão Jogar despausa o jogo");
        Checar(tela == null || tela.painel == null || !tela.painel.activeInHierarchy, "Menu some depois do fade");

        var gm = GameManager.Instance;
        ana = gm != null && gm.faiscaPlayer != null ? gm.faiscaPlayer : GameObject.Find("Faisca");
        if (ana == null) { Falha("Cris (Faisca) não encontrada"); Terminar(); yield break; }
        cc = ana.GetComponent<CharacterController>();
        interactor = ana.GetComponent<ProximityInteractor>();
        var mov = ana.GetComponent<PlayerMovement>();
        Checar(mov != null && mov.podeMover, "Cris pode se mover depois de Jogar");
        Vector3 inicio = ana.transform.position;
        Info("Cris começa em " + inicio.ToString("F2"));
        Salvar("play_1_inicio.png");

        // ---------- Andar de verdade, com teclado virtual ----------
        // Em batch a janela não tem foco: sem isto o Input System descarta as teclas. Restaurado no fim.
        var ajustes = UnityEngine.InputSystem.InputSystem.settings;
        var fundoAntes = ajustes.backgroundBehavior;
        var editorAntes = ajustes.editorInputBehaviorInPlayMode;
        ajustes.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        ajustes.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var teclado = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        teclado.MakeCurrent();
        yield return null;
        var anim = ana.GetComponent<CrisMiniatura>();
        float maiorPerna = 0f;
        Segurar(teclado, UnityEngine.InputSystem.Key.W);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            if (anim != null && anim.pernaEsquerda != null)
                maiorPerna = Mathf.Max(maiorPerna, Mathf.Abs(Mathf.DeltaAngle(0f, anim.pernaEsquerda.localEulerAngles.x)));
            yield return null;
        }
        float andou = Vector3.Distance(new Vector3(inicio.x, 0f, inicio.z), new Vector3(ana.transform.position.x, 0f, ana.transform.position.z));
        Checar(andou > 1.2f, "Segurando W Cris anda (" + andou.ToString("F2") + " m em 1,2 s)");
        Checar(maiorPerna > 12f, "Andando, as pernas balançam (até " + maiorPerna.ToString("F0") + "°)");
        Salvar("play_1b_andando.png");
        Soltar(teclado);
        yield return new WaitForSeconds(0.6f);
        Vector3 aposSoltar = ana.transform.position;
        yield return new WaitForSeconds(0.4f);
        Checar(Vector3.Distance(aposSoltar, ana.transform.position) < 0.02f, "Soltando a tecla, Cris freia e para");

        // Ao sul: anda até a borda do quarto; o chão visível tem que acabar ali também.
        Segurar(teclado, UnityEngine.InputSystem.Key.S);
        yield return new WaitForSeconds(4f);
        Soltar(teclado);
        float zSul = ana.transform.position.z;
        var chao = GameObject.Find("Chao_Quarto");
        float fimChao = chao != null ? chao.GetComponent<Renderer>().bounds.min.z : zSul;
        Checar(zSul - fimChao < 0.6f, "Indo para o sul, Cris chega à borda do chão visível (Cris em z " + zSul.ToString("F2") + ", chão acaba em " + fimChao.ToString("F2") + ")");
        UnityEngine.InputSystem.InputSystem.RemoveDevice(teclado);
        ajustes.backgroundBehavior = fundoAntes;
        ajustes.editorInputBehaviorInPlayMode = editorAntes;
        Teleportar(inicio);
        yield return new WaitForSeconds(0.5f);

        // ---------- Mapa de onde Cris consegue andar ----------
        var alcancaveis = MapearAlcance(inicio, out int livres, out bool inicioLivre);
        Checar(inicioLivre, "Cris não começa dentro de nenhum objeto");
        Info("Células alcançáveis a pé: " + alcancaveis.Count + " de " + livres + " livres (grade de 10 cm)");

        var puzzles = new List<Interactable>(FindObjectsByType<Interactable>(FindObjectsSortMode.None));
        puzzles.Sort((a, b) => string.Compare(a.GetType().Name, b.GetType().Name, System.StringComparison.Ordinal));
        int esperados = (StoryState.Instance != null ? StoryState.Instance.totalPuzzles : 3) + 1;
        Checar(puzzles.Count == esperados, esperados + " objetos interativos na cena (encontrados: " + puzzles.Count + ")");

        var pontos = new Dictionary<Interactable, Vector3>();
        foreach (var p in puzzles)
        {
            Vector3 ponto;
            bool ok = MelhorPonto(alcancaveis, p, out ponto);
            Checar(ok, p.GetType().Name + " (" + p.name + "): dá para chegar andando perto o bastante para o E escolher ele");
            if (ok) { pontos[p] = ponto; Info("ponto de interação: " + ponto.ToString("F2") + ", distância " + Vector3.Distance(ponto, p.transform.position).ToString("F2") + " m"); }
        }

        // ---------- Puzzles 1, 2 e 3 ----------
        var story = StoryState.Instance;
        var p1 = FindFirstObjectByType<PuzzlePriorityQueue>();
        var p2 = FindFirstObjectByType<PuzzleLoop>();
        var p3 = FindFirstObjectByType<PuzzleFirewall>();
        var pf = FindFirstObjectByType<PuzzleFinal>();

        if (p1 != null)
        {
            yield return Interagir(p1, pontos, "play_2_puzzle1.png");
            Checar(p1.painel != null && p1.painel.activeInHierarchy, "Puzzle 1: painel abre ao interagir");
            float v0 = gm != null ? gm.vontade : 0f;
            p1.Clicar(3);   // "Ensaiar" vence antes, mas depende dos slides
            Checar(gm == null || gm.vontade < v0, "Puzzle 1: tarefa bloqueada tira Vontade (" + v0 + " → " + (gm != null ? gm.vontade : 0f) + ")");
            var ordem = p1.OrdemCerta();
            Checar(!ordem.Contains(5), "Puzzle 1: a prova de Redes não entra na fila de hoje");
            foreach (int t in ordem) p1.Clicar(t);
            yield return new WaitForSeconds(2.8f);
            Checar(story != null && story.puzzle1Concluido, "Puzzle 1: concluído seguindo a fila (" + string.Join(", ", ordem) + ")");
            Checar(!p1.painel.activeInHierarchy, "Puzzle 1: painel fecha sozinho");
        }

        if (p2 != null)
        {
            yield return Interagir(p2, pontos, "play_3_puzzle2.png");
            Checar(p2.painel != null && p2.painel.activeInHierarchy, "Puzzle 2: painel abre ao interagir");
            p2.Escolher(p2.condicaoCerta, 0);   // condição certa, corpo errado: ainda é laço infinito
            p2.Executar();
            yield return new WaitForSeconds(2f);
            Checar(story == null || !story.puzzle2Concluido, "Puzzle 2: condição certa sem mudar a variável não resolve");
            p2.Escolher(p2.condicaoCerta, p2.corpoCerto);
            p2.Executar();
            yield return new WaitForSeconds(3.4f);
            Checar(story != null && story.puzzle2Concluido, "Puzzle 2: concluído com condição e corpo certos");
        }

        if (p3 != null)
        {
            yield return Interagir(p3, pontos, "play_4_puzzle3.png");
            Checar(p3.painel != null && p3.painel.activeInHierarchy, "Puzzle 3: painel abre ao interagir");
            p3.AlternarAcao(p3.PosicaoDe("contatos"));        // contatos: PERMITIR
            p3.Aplicar();
            Checar(story == null || !story.puzzle3Concluido, "Puzzle 3: só liberar os contatos deixa passar a corrente e o golpe");
            while (p3.PosicaoDe("correntes") > p3.PosicaoDe("contatos")) p3.Mover(p3.PosicaoDe("correntes"), -1);
            p3.AlternarAcao(p3.PosicaoDe("desconhecidos"));   // desconhecidos: BARRAR
            p3.Aplicar();
            yield return new WaitForSeconds(3f);
            Checar(story != null && story.puzzle3Concluido, "Puzzle 3: concluído com as regras na ordem certa");
        }

        var p4 = FindFirstObjectByType<PuzzleDeadlock>();
        if (p4 != null)
        {
            yield return Interagir(p4, pontos, "play_4b_puzzle4.png");
            Checar(p4.painel != null && p4.painel.activeInHierarchy, "Puzzle 4: painel abre ao interagir");
            p4.Cortar(4);   // espera irreal, mas só de um dos ciclos
            p4.Executar();
            yield return new WaitForSeconds(1.6f);
            Checar(story == null || !story.Concluido(4), "Puzzle 4: cortar só a espera do descanso ainda deixa um ciclo");
            p4.Cortar(4); p4.Cortar(0);   // devolve e corta a espera que está nos dois ciclos
            p4.Executar();
            yield return new WaitForSeconds(5f);
            Checar(story != null && story.Concluido(4), "Puzzle 4: deadlock desfeito");
        }

        var p5 = FindFirstObjectByType<PuzzleColetorLixo>();
        if (p5 != null)
        {
            yield return Interagir(p5, pontos, "play_4c_puzzle5.png");
            Checar(p5.painel != null && p5.painel.activeInHierarchy, "Puzzle 5: painel abre ao interagir");
            p5.Varrer();   // nada marcado: apagaria coisas vivas
            Checar(story == null || !story.Concluido(5), "Puzzle 5: varrer sem marcar é recusado");
            var vivos = p5.Alcancaveis();
            for (int volta = 0; volta < 4; volta++)
                for (int i = 1; i < vivos.Length; i++) if (vivos[i]) p5.Marcar(i);
            p5.Varrer();
            yield return new WaitForSeconds(5f);
            Checar(story != null && story.Concluido(5), "Puzzle 5: coletor rodou e liberou só o lixo");
        }

        var hud = FindFirstObjectByType<PuzzleProgressHUD>();
        yield return null;
        Checar(hud == null || (hud.texto != null && !hud.texto.text.Contains("pendente")), "HUD mostra os 3 puzzles como feitos");

        // ---------- Vontade em 0: sem apagão, sem tela escura ----------
        if (gm != null)
        {
            gm.DrenarVontade(1000f);
            yield return new WaitForSeconds(0.3f);
            Checar(mov != null && mov.podeMover, "Vontade em 0 não trava o movimento (não há apagão)");
            Checar(mov != null && mov.multiplicadorVelocidade < 0.95f, "Vontade em 0 deixa o passo de Cris mais lento (x" + (mov != null ? mov.multiplicadorVelocidade.ToString("F2") : "?") + ")");
            gm.RecuperarVontade(50f);
        }

        // ---------- Final: abrir a cortina ----------
        if (pf != null)
        {
            Checar(pf.interagivel, "Cortina liberada depois dos 3 puzzles");
            float luzAntes = pf.luzAmbiente != null ? pf.luzAmbiente.intensity : 0f;
            yield return Interagir(pf, pontos, null);
            yield return new WaitForSeconds(pf.duracaoAbertura + 1.6f);
            float luzDepois = pf.luzAmbiente != null ? pf.luzAmbiente.intensity : 0f;
            Checar(luzDepois > luzAntes + 0.3f, "Final: o quarto clareia ao abrir a cortina (luz " + luzAntes.ToString("F2") + " → " + luzDepois.ToString("F2") + ")");
            Checar(pf.painelFinal != null && pf.painelFinal.activeInHierarchy, "Final: painel com a mensagem final aparece");
            if (pf.painelFinal != null) pf.painelFinal.SetActive(false);   // para o print mostrar o quarto
            Salvar("play_5_final.png");
        }

        Terminar();
    }

    // Leva Cris até o ponto de interação, confere se o E escolheria o objeto e interage.
    IEnumerator Interagir(Interactable alvo, Dictionary<Interactable, Vector3> pontos, string print)
    {
        Vector3 ponto;
        if (!pontos.TryGetValue(alvo, out ponto))
        {
            Info(alvo.GetType().Name + ": sem ponto alcançável, interagindo direto");
            alvo.Interagir();
            yield break;
        }
        Teleportar(ponto);
        Vector3 olhar = alvo.transform.position - ponto; olhar.y = 0f;
        if (olhar.sqrMagnitude > 0.01f) ana.transform.forward = olhar.normalized;
        yield return new WaitForSeconds(1.2f);   // câmera alcança Cris
        var atual = typeof(ProximityInteractor).GetField("atual", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(interactor) as Interactable;
        Checar(atual == alvo, alvo.GetType().Name + ": ao lado dele, o E escolhe o objeto certo (escolhido: " + (atual != null ? atual.GetType().Name : "nenhum") + ")");
        if (print != null) Salvar(print);
        alvo.Interagir();   // o mesmo que apertar E
        yield return null;
    }

    static void Segurar(UnityEngine.InputSystem.Keyboard teclado, UnityEngine.InputSystem.Key tecla)
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado, new UnityEngine.InputSystem.LowLevel.KeyboardState(tecla));
        UnityEngine.InputSystem.InputSystem.Update();
    }

    static void Soltar(UnityEngine.InputSystem.Keyboard teclado)
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        UnityEngine.InputSystem.InputSystem.Update();
    }

    // Print com a UI: o Canvas é Overlay (não aparece no Camera.Render), então troca por um instante.
    void SalvarComUI(string nome)
    {
        var canvas = GameObject.Find("Canvas_UI")?.GetComponent<Canvas>();
        if (canvas == null) { Salvar(nome); return; }
        var modo = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        Canvas.ForceUpdateCanvases();
        Salvar(nome);
        canvas.renderMode = modo;
    }

    void Teleportar(Vector3 p)
    {
        cc.enabled = false;
        ana.transform.position = p;
        Physics.SyncTransforms();
        cc.enabled = true;
    }

    // ---------- Alcance a pé: busca em largura numa grade, testando a cápsula de Cris ----------
    const float PASSO = 0.1f;

    bool CapsulaLivre(Vector3 pos)
    {
        float s = ana.transform.lossyScale.y;
        float r = cc.radius * s, h = cc.height * s;
        Vector3 c = pos + cc.center * s;
        Vector3 baixo = c + Vector3.up * (-h * 0.5f + r + 0.04f);   // um pouco acima do chão (degrau)
        Vector3 cima = c + Vector3.up * (h * 0.5f - r);
        foreach (var col in Physics.OverlapCapsule(baixo, cima, r * 0.98f, ~0, QueryTriggerInteraction.Ignore))
            if (col.gameObject != ana && !col.transform.IsChildOf(ana.transform)) return false;
        return true;
    }

    List<Vector3> MapearAlcance(Vector3 inicio, out int livres, out bool inicioLivre)
    {
        var lista = new List<Vector3>();
        var visto = new HashSet<Vector2Int>();
        var fila = new Queue<Vector2Int>();
        Vector2Int Celula(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x / PASSO), Mathf.RoundToInt(p.z / PASSO));
        Vector3 Pos(Vector2Int c) => new Vector3(c.x * PASSO, inicio.y, c.y * PASSO);

        inicioLivre = CapsulaLivre(inicio);
        livres = 0;
        for (float x = -4f; x <= 4f; x += PASSO)
            for (float z = -3.5f; z <= 3.5f; z += PASSO)
                if (CapsulaLivre(new Vector3(x, inicio.y, z))) livres++;

        Vector2Int c0 = Celula(inicio);
        fila.Enqueue(c0); visto.Add(c0);
        Vector2Int[] viz = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (fila.Count > 0 && lista.Count < 20000)
        {
            Vector2Int c = fila.Dequeue();
            Vector3 p = Pos(c);
            if (!CapsulaLivre(p) && c != c0) continue;
            lista.Add(p);
            foreach (var d in viz)
            {
                Vector2Int n = c + d;
                if (visto.Add(n)) fila.Enqueue(n);
            }
        }
        return lista;
    }

    // Mesma regra do ProximityInteractor: entre os interativos dentro do raio, o de transform mais perto.
    Interactable EscolhidoEm(Vector3 pos)
    {
        Interactable melhor = null;
        float menor = float.MaxValue;
        foreach (var col in Physics.OverlapSphere(pos, interactor.raioInteracao, interactor.camadaInteragiveis))
        {
            var it = col.GetComponent<Interactable>();
            if (it == null) continue;
            float d = Vector3.Distance(pos, col.transform.position);
            if (d < menor) { menor = d; melhor = it; }
        }
        return melhor;
    }

    bool MelhorPonto(List<Vector3> alcancaveis, Interactable alvo, out Vector3 ponto)
    {
        ponto = Vector3.zero;
        float menor = float.MaxValue;
        foreach (var p in alcancaveis)
        {
            float d = Vector3.Distance(p, alvo.transform.position);
            if (d >= menor || d > interactor.raioInteracao + 1f) continue;
            if (EscolhidoEm(p) != alvo) continue;
            menor = d; ponto = p;
        }
        return menor < float.MaxValue;
    }

    // ---------- Saída ----------
    void Salvar(string nome)
    {
        Camera cam = Camera.main;
        if (cam == null || string.IsNullOrEmpty(pastaSaida)) return;
        const int w = 1280, h = 720;
        var rt = new RenderTexture(w, h, 24);
        var antes = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(pastaSaida, nome), tex.EncodeToPNG());
        cam.targetTexture = antes;
        RenderTexture.active = null;
        Destroy(rt); Destroy(tex);
    }

    void Terminar()
    {
        relatorio.Insert(0, (falhas == 0 ? "TUDO OK" : falhas + " FALHA(S)") + "\n\n");
        string texto = relatorio.ToString();
        Debug.Log("[TESTE ANA]\n" + texto);
        if (!string.IsNullOrEmpty(pastaSaida)) File.WriteAllText(Path.Combine(pastaSaida, "relatorio.txt"), texto, Encoding.UTF8);
        UnityEditor.SessionState.EraseFloat("TesteAna_Inicio");   // desliga o limite de tempo do TestePlayMode
        // Em batch (linha de comando) fecha o Unity; pelo menu, só sai do Play.
        if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(falhas == 0 ? 0 : 1);
        else UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
