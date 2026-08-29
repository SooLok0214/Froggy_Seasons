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
        PlayerStats playerStats = FindComponent<PlayerStats>(scene);
        PlayerController playerController = FindComponent<PlayerController>(scene);
        PlayerAttack playerAttack = FindComponent<PlayerAttack>(scene);
        ThirdPersonCamera thirdPersonCamera = FindComponent<ThirdPersonCamera>(scene);
        Canvas rootCanvas = FindRootCanvas(scene);

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
            uiManager.healthLine = FindComponentByName<Image>(scene, "healthLine");
            uiManager.expLine = FindComponentByName<Image>(scene, "expLine");
            uiManager.cinzelFont = FindCinzelFont();
            uiManager.BuildHUD();
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
