using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tira prints da cena (para revisar o quarto sem abrir o Play).
// Linha de comando: -executeMethod CapturaQuarto.Capturar -shots <pasta>
public static class CapturaQuarto
{
    [MenuItem("Bug In The Neural Network/Ferramentas/Capturar Prints do Quarto")]
    public static void CapturarMenu()
    {
        Capturar(Path.Combine(Application.dataPath, "../Prints"));
    }

    public static void Capturar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Capturar(PastaDaLinhaDeComando());
    }

    // Linha de comando: -executeMethod CapturaQuarto.PovoarESalvar -shots <pasta>
    public static void PovoarESalvar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SceneSetup.PovoarQuartoAcumulo();
        EditorSceneManager.SaveOpenScenes();
        Capturar(PastaDaLinhaDeComando());
    }

    // Linha de comando: -executeMethod CapturaQuarto.MadrugadaESalvar -shots <pasta>
    public static void MadrugadaESalvar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SceneSetup.AplicarMadrugada();
        EditorSceneManager.SaveOpenScenes();
        Capturar(PastaDaLinhaDeComando());
    }

    // Linha de comando: -executeMethod CapturaQuarto.NinhoESalvar -shots <pasta>
    public static void NinhoESalvar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SceneSetup.MontarCamaNinho();
        EditorSceneManager.SaveOpenScenes();
        Capturar(PastaDaLinhaDeComando());
    }

    // Linha de comando: -executeMethod CapturaQuarto.HistoriaESalvar -shots <pasta>
    public static void HistoriaESalvar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SceneSetup.MontarDetalhesHistoria();
        EditorSceneManager.SaveOpenScenes();
        Capturar(PastaDaLinhaDeComando());
    }

    static string PastaDaLinhaDeComando()
    {
        string pasta = Path.Combine(Application.dataPath, "../Prints");
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-shots") pasta = args[i + 1];
        return pasta;
    }

    public static void Capturar(string pasta)
    {
        Directory.CreateDirectory(pasta);

        // Esconde a UI (menus/painéis) para o print mostrar só o quarto.
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            c.enabled = false;

        Camera jogo = Camera.main;
        if (jogo != null) Salvar(jogo, Path.Combine(pasta, "1_camera_jogo.png"));

        GameObject temp = new GameObject("Camera_Print");
        Camera cam = temp.AddComponent<Camera>();
        if (jogo != null) cam.CopyFrom(jogo);
        cam.fieldOfView = 60f;

        cam.transform.position = new Vector3(0f, 7.5f, -6.5f);
        cam.transform.LookAt(new Vector3(0f, 0f, 0.3f));
        Salvar(cam, Path.Combine(pasta, "2_visao_geral.png"));

        cam.transform.position = new Vector3(0f, 9.5f, 0f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Salvar(cam, Path.Combine(pasta, "3_de_cima.png"));

        cam.transform.position = new Vector3(2.5f, 2.2f, -3.5f);
        cam.transform.LookAt(new Vector3(-2.8f, 0.8f, 0.5f));
        Salvar(cam, Path.Combine(pasta, "4_escrivaninha.png"));

        cam.transform.position = new Vector3(-2.5f, 2.2f, -3.5f);
        cam.transform.LookAt(new Vector3(2.8f, 0.6f, 1.2f));
        Salvar(cam, Path.Combine(pasta, "5_cama.png"));

        cam.transform.position = new Vector3(0.9f, 2.4f, -0.4f);
        cam.transform.LookAt(new Vector3(3.0f, 0.4f, 2.2f));
        Salvar(cam, Path.Combine(pasta, "6_ninho.png"));

        // Closes dos detalhes: monitor com os slides e o despertador ao lado da cama.
        GameObject slide = GameObject.Find("Slide_No_Monitor");
        if (slide != null)
        {
            cam.transform.position = slide.transform.position + new Vector3(1.1f, 0.25f, -0.35f);
            cam.transform.LookAt(slide.transform.position);
            Salvar(cam, Path.Combine(pasta, "7_monitor.png"));
        }
        GameObject relogio = GameObject.Find("Despertador");
        if (relogio != null)
        {
            cam.transform.position = relogio.transform.position + new Vector3(-0.5f, 0.45f, -0.9f);
            cam.transform.LookAt(relogio.transform.position + Vector3.up * 0.05f);
            Salvar(cam, Path.Combine(pasta, "8_despertador.png"));
        }

        Object.DestroyImmediate(temp);
        Debug.Log("Prints salvos em " + pasta);
    }

    static void Salvar(Camera cam, string arquivo)
    {
        const int w = 1280, h = 720;
        RenderTexture rt = new RenderTexture(w, h, 24);
        RenderTexture antes = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.Render(); // o primeiro render no batchmode às vezes sai sem os shaders prontos
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(arquivo, tex.EncodeToPNG());
        cam.targetTexture = antes;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
