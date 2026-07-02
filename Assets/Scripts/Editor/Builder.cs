using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class Builder
{
    public static void BuildWebGL()
    {
        Debug.Log("[Builder] Starting WebGL Build...");
        
        // Disable compression to avoid localhost server header issues
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        // Gerçekçi görünüm: Linear renk uzayı (WebGL2) + kaliteli gölge/AA.
        PlayerSettings.colorSpace        = ColorSpace.Linear;
        QualitySettings.antiAliasing     = 4;
        QualitySettings.shadowDistance   = 150f;
        QualitySettings.shadows          = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;


        string[] scenes = { "Assets/Scenes/Main.unity" };
        string buildPath = "Builds/WebGL";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Builder] Build succeeded: " + summary.totalSize + " bytes");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("[Builder] Build failed");
        }
    }
}
