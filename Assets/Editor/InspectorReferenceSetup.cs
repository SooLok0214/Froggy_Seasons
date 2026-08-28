#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class InspectorReferenceSetup
{
    public static string[] scenePaths =
    {
        "Assets/Scenes/Main_Use_Scene.unity",
        "Assets/Scenes/InGameScene.unity"
    };

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
        LevelUpChoiceSystem levelSystem = FindComponent<LevelUpChoiceSystem>(scene);
        PlayerStats playerStats = FindComponent<PlayerStats>(scene);
        PlayerController playerController = FindComponent<PlayerController>(scene);
        PlayerAttack playerAttack = FindComponent<PlayerAttack>(scene);
        ThirdPersonCamera thirdPersonCamera = FindComponent<ThirdPersonCamera>(scene);
        EnemyDifficultySettings difficultySettings = FindComponent<EnemyDifficultySettings>(scene);
        Canvas rootCanvas = FindRootCanvas(scene);

        if (gameManager != null)
        {
            gameManager.uiManager = uiManager;
            gameManager.scoreManager = scoreManager;
            gameManager.musicManager = musicManager;
            gameManager.enemySpawner = enemySpawner;
            gameManager.levelUpChoiceSystem = levelSystem;
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

            GameObject infoBar = FindObject(scene, "infoBar");
            if (infoBar != null)
                uiManager.characterInfo = infoBar;

            EditorUtility.SetDirty(uiManager);
        }

        if (scoreManager != null)
        {
            scoreManager.playerStats = playerStats;
            scoreManager.scorePanel = FindRect(scene, "endScorePanel");
            scoreManager.cinzelFont = FindCinzelFont();
            scoreManager.BuildScoreDisplay();
            EditorUtility.SetDirty(scoreManager);

            GameplayHUD hud = scoreManager.GetComponent<GameplayHUD>();
            if (hud == null)
                hud = scoreManager.gameObject.AddComponent<GameplayHUD>();

            hud.scoreManager = scoreManager;
            hud.playerStats = playerStats;
            hud.scoreBar = FindRect(scene, "scoreBar");
            hud.infoBar = FindRect(scene, "infoBar");
            hud.healthLine = FindComponentByName<Image>(scene, "healthLine");
            hud.expLine = FindComponentByName<Image>(scene, "expLine");
            hud.cinzelFont = FindCinzelFont();
            hud.BuildHUD();
            EditorUtility.SetDirty(hud);
        }

        if (levelSystem != null)
        {
            levelSystem.uiManager = uiManager;
            levelSystem.rootCanvas = rootCanvas;
            levelSystem.BuildUI();
            levelSystem.SetOverlayActive(false);
            EditorUtility.SetDirty(levelSystem);
        }

        if (playerStats != null)
        {
            playerStats.scoreManager = scoreManager;
            playerStats.playerController = playerController;
            playerStats.levelUpChoiceSystem = levelSystem;
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
            enemySpawner.difficultySettings = difficultySettings;
            EditorUtility.SetDirty(enemySpawner);
        }

        ConnectButtonAudio(scene);
    }

    public static void ConnectButtonAudio(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button.name == "attackButton")
                    continue;

                UIButtonAudio audio = button.GetComponent<UIButtonAudio>();
                if (audio == null)
                    audio = button.gameObject.AddComponent<UIButtonAudio>();

                audio.playFrogCroak = button.name == "homeLogoButton";
                audio.toggleBgmMute = button.name == "musicBtn";
                audio.toggleSfxMute = button.name == "SFXbtn";
                EditorUtility.SetDirty(audio);
            }
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
