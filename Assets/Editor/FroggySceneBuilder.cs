using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FroggySceneBuilder
{
    public const string MenuScenePath = "Assets/Scenes/Main_Use_Scene.unity";
    public const string GameplayScenePath = "Assets/Scenes/InGameScene.unity";
    public const string MapAssetPath = "Assets/WorldMap/FourSeasonsWorldMap.fbx";
    public const string MaterialFolder = "Assets/WorldMap/Materials";

    public static readonly string[] HomeObjectNames =
    {
        "StartPanel",
        "HomeMenu",
        "homeBackground",
        "homeLogo",
        "homeStartBtn",
        "homeSettingBtn",
        "homeTutorialVisual",
        "homeTutorialBtn",
        "HomeSettingsPanel"
    };

    [MenuItem("Tools/Froggy Seasons/Build Gameplay Scene")]
    public static void BuildGameplayScene()
    {
        AssetDatabase.ImportAsset(MapAssetPath, ImportAssetOptions.ForceSynchronousImport);

        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetPath);
        if (mapPrefab == null)
        {
            Debug.LogError("Map FBX is missing: " + MapAssetPath);
            return;
        }

        EditorSceneManager.SaveOpenScenes();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) != null)
            AssetDatabase.DeleteAsset(GameplayScenePath);

        if (!AssetDatabase.CopyAsset(MenuScenePath, GameplayScenePath))
        {
            Debug.LogError("Could not create the gameplay scene.");
            return;
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

        foreach (string objectName in HomeObjectNames)
        {
            GameObject homeObject = FindSceneObject(scene, objectName);
            if (homeObject != null)
                Object.DestroyImmediate(homeObject);
        }

        GameObject existingMap = FindSceneObject(scene, "FourSeasonsWorldMap");
        if (existingMap != null)
            Object.DestroyImmediate(existingMap);

        GameObject mapRoot = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
        mapRoot.name = "FourSeasonsWorldMap";
        mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        mapRoot.transform.localScale = Vector3.one;

        ConvertMapMaterials(mapRoot);
        FrameMapWithCamera(scene, mapRoot);

        UIManager uiManager = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager != null)
        {
            uiManager.startPanel = null;
            uiManager.homeMenu = null;
            uiManager.gameStarted = true;
            EditorUtility.SetDirty(uiManager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();

        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        Debug.Log("[Codex] InGameScene created. Map imported at 1:1 scale; gameplay UI and death test retained.");
    }

    public static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    public static void FrameMapWithCamera(Scene scene, GameObject mapRoot)
    {
        Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Camera camera = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault();

        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.transform.position = bounds.center + Vector3.up * Mathf.Max(50f, bounds.size.magnitude);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        float aspect = 16f / 9f;
        camera.orthographicSize = Mathf.Max(bounds.extents.z, bounds.extents.x / aspect) * 1.12f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = Mathf.Max(1000f, bounds.size.magnitude * 4f);
        EditorUtility.SetDirty(camera);
    }

    public static void ConvertMapMaterials(GameObject mapRoot)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/WorldMap"))
                AssetDatabase.CreateFolder("Assets", "WorldMap");
            AssetDatabase.CreateFolder("Assets/WorldMap", "Materials");
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        foreach (Renderer renderer in mapRoot.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null)
                    continue;

                if (!converted.TryGetValue(source, out Material target))
                {
                    string safeName = MakeSafeFileName(source.name);
                    string assetPath = MaterialFolder + "/" + safeName + ".mat";
                    target = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (target == null)
                    {
                        target = new Material(shader) { name = source.name };
                        AssetDatabase.CreateAsset(target, assetPath);
                    }
                    else
                    {
                        target.shader = shader;
                        target.name = source.name;
                    }

                    target.color = source.HasProperty("_BaseColor")
                        ? source.GetColor("_BaseColor")
                        : source.color;

                    if (source.HasProperty("_MainTex") && source.mainTexture != null)
                        target.mainTexture = source.mainTexture;

                    if (source.HasProperty("_Metallic"))
                        target.SetFloat("_Metallic", source.GetFloat("_Metallic"));

                    if (source.HasProperty("_Glossiness"))
                        target.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));

                    EditorUtility.SetDirty(target);
                    converted.Add(source, target);
                }

                materials[i] = target;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    public static string MakeSafeFileName(string value)
    {
        foreach (char character in Path.GetInvalidFileNameChars())
            value = value.Replace(character, '_');
        return string.IsNullOrWhiteSpace(value) ? "MapMaterial" : value;
    }

    public static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(item => item.path == MenuScenePath || item.path == GameplayScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(GameplayScenePath, true));
        scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
