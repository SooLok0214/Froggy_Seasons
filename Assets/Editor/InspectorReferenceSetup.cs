#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InspectorReferenceSetup
{
    public static string[] scenePaths =
    {
        "Assets/Scenes/Main_Use_Scene.unity",
        "Assets/Scenes/InGameScene.unity"
    };

    static InspectorReferenceSetup()
    {
        EditorApplication.delayCall += ConnectOpenGameplayScene;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += ConnectOpenGameplayScene;
    }

    public static void ConnectOpenGameplayScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "InGameScene")
            return;

        ConnectScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Froggy] InGame HUD references connected and refreshed.");
    }

    [MenuItem("Tools/Froggy/Connect Inspector References")]
    public static void ConnectAllScenes()
    {
        EditorSceneManager.SaveOpenScenes();
        string originalScene = SceneManager.GetActiveScene().path;

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            ConnectScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        Debug.Log("[Froggy] Inspector references connected in both scenes.");
    }

    public static void ConnectScene(Scene scene)
    {
        GameManager gameManager = FindComponent<GameManager>(scene);
        UIManager uiManager = FindComponent<UIManager>(scene);
        ScoreManager scoreManager = FindComponent<ScoreManager>(scene);
        MusicManager musicManager = FindComponent<MusicManager>(scene);
        EnemySpawner enemySpawner = FindComponent<EnemySpawner>(scene);
        PlayerStats playerStats = FindComponent<PlayerStats>(scene);
        PlayerController playerController = FindComponent<PlayerController>(scene);
        PlayerAttack playerAttack = FindComponent<PlayerAttack>(scene);
        ThirdPersonCamera thirdPersonCamera = FindComponent<ThirdPersonCamera>(scene);
        Canvas rootCanvas = FindRootCanvas(scene);

        GameObject oldAttackTouchArea = FindObject(scene, "attackTouchArea");
        if (oldAttackTouchArea != null)
            Object.DestroyImmediate(oldAttackTouchArea, true);

        if (gameManager != null)
        {
            gameManager.uiManager = uiManager;
            gameManager.scoreManager = scoreManager;
            gameManager.musicManager = musicManager;
            gameManager.enemySpawner = enemySpawner;
            EditorUtility.SetDirty(gameManager);
        }

        if (musicManager != null)
        {
            musicManager.sceneGameManager = gameManager;
            musicManager.bgmSlider = FindComponentByName<Slider>(scene, "bgmSlider");
            musicManager.sfxSlider = FindComponentByName<Slider>(scene, "sfxSlider");
            EditorUtility.SetDirty(musicManager);
        }

        if (uiManager != null)
        {
            uiManager.thirdPersonCamera = thirdPersonCamera;
            uiManager.crosshair = FindObject(scene, "Crosshair", "crosshair");
            uiManager.scoreManager = scoreManager;
            uiManager.playerStats = playerStats;
            uiManager.rootCanvas = rootCanvas;

            GameObject infoBar = FindObject(scene, "infoBar");
            if (infoBar != null)
                uiManager.characterInfo = infoBar;

            uiManager.scoreBarRect = FindRect(scene, "scoreBar");
            uiManager.infoBarRect = FindRect(scene, "infoBar");
            uiManager.healthLine = FindChildComponent<Image>(uiManager.infoBarRect, "healthLine");
            uiManager.expLine = FindChildComponent<Image>(uiManager.infoBarRect, "expLine");
            uiManager.healthText = FindChildComponent<Text>(uiManager.infoBarRect, "HealthValue");
            uiManager.levelText = FindChildComponent<Text>(uiManager.infoBarRect, "LevelValue");
            uiManager.liveKillsText = FindChildComponent<Text>(uiManager.scoreBarRect, "LiveKillValue");
            uiManager.liveTimeText = FindChildComponent<Text>(uiManager.scoreBarRect, "LiveTimeValue");
            uiManager.cinzelFont = FindCinzelFont();
            uiManager.BuildHUD();
            uiManager.ResetHUDCache();
            uiManager.UpdateHUD();
            uiManager.BuildLevelUpUI();
            uiManager.SetLevelUpOverlayActive(false);

            EditorUtility.SetDirty(uiManager);
        }

        if (scoreManager != null)
        {
            scoreManager.playerStats = playerStats;
            scoreManager.scorePanel = FindRect(scene, "endScorePanel");
            scoreManager.cinzelFont = FindCinzelFont();
            scoreManager.BuildScoreDisplay();
            EditorUtility.SetDirty(scoreManager);

        }

        if (playerStats != null)
        {
            playerStats.scoreManager = scoreManager;
            playerStats.playerController = playerController;
            playerStats.uiManager = uiManager;
            playerStats.audioSource = playerStats.GetComponent<AudioSource>();
            EditorUtility.SetDirty(playerStats);
        }

        if (playerController != null)
        {
            Camera gameCamera = FindComponent<Camera>(scene);
            playerController.gameCamera = gameCamera == null ? null : gameCamera.gameObject;
            playerController.variableJoystick = FindComponent<VariableJoystick>(scene);
            EditorUtility.SetDirty(playerController);
        }

        if (playerAttack != null)
        {
            playerAttack.attackButton = FindComponentByName<Button>(scene, "attackButton");
            playerAttack.aimCamera = FindComponent<Camera>(scene);
            playerAttack.playerStats = playerStats;
            EditorUtility.SetDirty(playerAttack);
        }

        if (enemySpawner != null)
        {
            enemySpawner.player = playerStats == null ? null : playerStats.transform;
            EditorUtility.SetDirty(enemySpawner);
        }

    }

    public static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    public static T FindComponentByName<T>(Scene scene, string objectName) where T : Component
    {
        GameObject target = FindObject(scene, objectName);
        return target == null ? null : target.GetComponent<T>();
    }

    public static GameObject FindObject(Scene scene, params string[] names)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform current in transforms)
            {
                foreach (string objectName in names)
                {
                    if (current.name == objectName)
                        return current.gameObject;
                }
            }
        }

        return null;
    }

    public static RectTransform FindRect(Scene scene, string objectName)
    {
        GameObject target = FindObject(scene, objectName);
        return target == null ? null : target.GetComponent<RectTransform>();
    }

    public static T FindChildComponent<T>(RectTransform parent, string objectName)
        where T : Component
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(objectName);
        return child == null ? null : child.GetComponent<T>();
    }

    public static Canvas FindRootCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isRootCanvas)
                    return canvas;
            }
        }

        return null;
    }

    public static Font FindCinzelFont()
    {
        string[] guids = AssetDatabase.FindAssets("Cinzel-Bold t:Font");
        if (guids.Length == 0)
            return null;

        return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
#endif
