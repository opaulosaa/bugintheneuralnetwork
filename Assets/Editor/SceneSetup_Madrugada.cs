using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

// ======================= MADRUGADA =======================
// Véspera da apresentação, 3 da manhã: o quarto fica no escuro, frio e abafado.
// Só o monitor (azul) e a luminária (âmbar, fraca) iluminam de verdade; a luz da lua
// mal passa pela cortina fechada. O brilho da própria Ana (a Vontade) passa a se destacar.
// Rodar de novo só reaplica os mesmos valores.
public static partial class SceneSetup
{
    const string PERFIL_MADRUGADA = "Assets/Settings/MadrugadaProfile.asset";

    [MenuItem("Bug In The Neural Network/Antigos/Povoar Quarto - 2. Madrugada (substituído por Versão Cris > Fim de tarde)")]
    public static void AplicarMadrugada()
    {
        // ---------- Luz principal: lua fraca, vinda do alto da janela ----------
        Light sol = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            sol.intensity = 0.15f;
            sol.color = new Color(0.45f, 0.52f, 0.78f);
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.6f;
            sol.transform.rotation = Quaternion.Euler(55f, 160f, 0f);
            EditorUtility.SetDirty(sol);
        }

        // O GameManager pinta essa luz conforme a Vontade: as duas pontas agora são de madrugada.
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.corFria = new Color(0.32f, 0.38f, 0.62f);
            gm.corQuente = new Color(0.62f, 0.58f, 0.72f);
            EditorUtility.SetDirty(gm);
        }

        // O final (abrir a cortina) traz o amanhecer de volta.
        var final = Object.FindFirstObjectByType<PuzzleFinal>();
        if (final != null)
        {
            final.intensidadeLuzFinal = 1.1f;
            final.intensidadeJanelaFinal = 3f;
            EditorUtility.SetDirty(final);
        }

        // ---------- Ambiente: quase nada de luz indireta ----------
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.10f, 0.12f, 0.20f);
        RenderSettings.ambientEquatorColor = new Color(0.07f, 0.08f, 0.13f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.06f);
        RenderSettings.reflectionIntensity = 0.25f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            EditorUtility.SetDirty(cam);
        }

        // ---------- Luzes do quarto ----------
        AjustarLuz("Luz_Monitor", new Color(0.50f, 0.66f, 1f), 2.2f, 3.6f);     // a tela acesa com o trabalho por fazer
        AjustarLuz("Luz_Luminaria", new Color(1f, 0.70f, 0.42f), 1.1f, 2.6f);   // lâmpada amarela, fraca
        AjustarLuz("Luz_Janela", new Color(0.40f, 0.50f, 0.90f), 1.1f, 4f);     // lua atrás da cortina fechada

        // ---------- Pós-processamento: vinheta e cores lavadas ----------
        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume != null)
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PERFIL_MADRUGADA);
            if (perfil == null)
            {
                if (volume.sharedProfile != null)
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), PERFIL_MADRUGADA);
                perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PERFIL_MADRUGADA);
                if (perfil == null)
                {
                    perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(perfil, PERFIL_MADRUGADA);
                }
            }

            Vignette vinheta;
            if (!perfil.TryGet(out vinheta)) vinheta = perfil.Add<Vignette>(true);
            vinheta.active = true;
            vinheta.color.Override(new Color(0.01f, 0.01f, 0.03f));
            vinheta.intensity.Override(0.42f);
            vinheta.smoothness.Override(0.5f);

            ColorAdjustments cores;
            if (!perfil.TryGet(out cores)) cores = perfil.Add<ColorAdjustments>(true);
            cores.active = true;
            cores.saturation.Override(-30f);
            cores.contrast.Override(12f);
            cores.colorFilter.Override(new Color(0.86f, 0.9f, 1f));

            Bloom bloom;
            if (!perfil.TryGet(out bloom)) bloom = perfil.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.6f);

            foreach (var c in perfil.components) EditorUtility.SetDirty(c);
            EditorUtility.SetDirty(perfil);
            volume.sharedProfile = perfil;
            EditorUtility.SetDirty(volume);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Madrugada aplicada. Salve a cena (Ctrl+S).");
    }

    static void AjustarLuz(string nome, Color cor, float intensidade, float alcance)
    {
        GameObject g = GameObject.Find(nome);
        Light l = g != null ? g.GetComponent<Light>() : null;
        if (l == null) { Debug.LogWarning("Luz '" + nome + "' não encontrada."); return; }
        l.color = cor;
        l.intensity = intensidade;
        l.range = alcance;
        l.shadows = LightShadows.Soft;
        EditorUtility.SetDirty(l);
    }
}
