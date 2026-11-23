using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Roda o roteiro TesteAutomaticoCris em Play mode.
// Linha de comando (sem -quit): -executeMethod TestePlayMode.Rodar -shots <pasta>
// O Editor fecha sozinho no fim, com código 0 (tudo ok) ou 1 (alguma falha).
[InitializeOnLoad]
public static class TestePlayMode
{
    const string CHAVE = "TesteAna_Pasta";
    const double LIMITE_SEGUNDOS = 240;

    static TestePlayMode()
    {
        // Depois do recarregamento ao entrar no Play, o Unity roda este construtor de novo.
        if (string.IsNullOrEmpty(SessionState.GetString(CHAVE, ""))) return;
        EditorApplication.delayCall += CriarRoteiroSeEmPlay;
        EditorApplication.update += VigiarTempo;
    }

    [MenuItem("Bug In The Neural Network/Ferramentas/Testar em Play Mode")]
    public static void RodarMenu()
    {
        Iniciar(Path.GetFullPath(Path.Combine(Application.dataPath, "../Prints/TestePlay")));
    }

    public static void Rodar()
    {
        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "../Prints/TestePlay"));
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-shots") pasta = args[i + 1];
        Iniciar(pasta);
    }

    static void Iniciar(string pasta)
    {
        Directory.CreateDirectory(pasta);
        SessionState.SetString(CHAVE, pasta);
        SessionState.SetFloat("TesteAna_Inicio", (float)EditorApplication.timeSinceStartup);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        // Também registra aqui, caso o projeto entre no Play sem recarregar o domínio.
        EditorApplication.delayCall += CriarRoteiroSeEmPlay;
        EditorApplication.update += VigiarTempo;
        EditorApplication.EnterPlaymode();
    }

    static void CriarRoteiroSeEmPlay()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.delayCall += CriarRoteiroSeEmPlay;
            return;
        }
        if (Object.FindFirstObjectByType<TesteAutomaticoCris>() != null) return;
        var g = new GameObject("TesteAutomaticoCris");
        g.AddComponent<TesteAutomaticoCris>().pastaSaida = SessionState.GetString(CHAVE, "");
        SessionState.EraseString(CHAVE);
    }

    static void VigiarTempo()
    {
        float inicio = SessionState.GetFloat("TesteAna_Inicio", 0f);
        if (inicio > 0f && EditorApplication.timeSinceStartup - inicio > LIMITE_SEGUNDOS)
        {
            Debug.LogError("[TESTE ANA] Tempo esgotado sem terminar o roteiro.");
            SessionState.EraseFloat("TesteAna_Inicio");
            if (Application.isBatchMode) EditorApplication.Exit(2);
            else EditorApplication.isPlaying = false;
        }
    }
}
