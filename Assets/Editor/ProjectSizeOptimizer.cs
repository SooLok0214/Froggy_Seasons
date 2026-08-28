#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ProjectSizeOptimizer
{
    [MenuItem("Tools/Froggy/Optimize Build Size")]
    public static void OptimizeBuildSize()
    {
        OptimizeWorldMap();
        OptimizeBgm();
        OptimizeUiTextures();
        PlayerSettings.stripUnusedMeshComponents = true;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Froggy Optimizer] Finished: map mesh, BGM, UI textures and mesh stripping optimized.");
    }

    [MenuItem("Tools/Froggy/Build Optimized Android APK")]
    public static void BuildOptimizedAndroidApk()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Builds");
        string outputPath = Path.Combine(outputDirectory, "FroggyOptimized.apk");
        Directory.CreateDirectory(outputDirectory);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[Froggy Optimizer] Android build {report.summary.result}: {report.summary.totalSize / (1024f * 1024f):F2} MB at {outputPath}");
    }

    [MenuItem("Tools/Froggy/Switch Active Platform To Android")]
    public static void SwitchActivePlatformToAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android,
            BuildTarget.Android
        );
    }

    public static void OptimizeWorldMap()
    {
        const string path = "Assets/WorldMap/FourSeasonsWorldMap.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Froggy Optimizer] World map FBX was not found.");
            return;
        }

        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.importTangents = ModelImporterTangents.None;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    public static void OptimizeBgm()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sound/BGM" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                continue;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
        }
    }

    public static void OptimizeUiTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/UI_Metirial" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.mipmapEnabled = false;
            importer.isReadable = false;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = Path.GetFileNameWithoutExtension(path).Equals("homeBackground", System.StringComparison.OrdinalIgnoreCase)
                ? 2048
                : 1024;
            android.format = TextureImporterFormat.Automatic;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.compressionQuality = 50;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }
}
#endif
