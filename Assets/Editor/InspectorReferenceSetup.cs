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
            SetupCreditsUI(scene, uiManager);
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

    public static void SetupCreditsUI(Scene scene, UIManager uiManager)
    {
        GameObject pausePanel = FindObject(scene, "PausePanel");
        if (pausePanel == null || uiManager == null)
            return;

        GameObject settingsPanel = FindObject(scene, "SettingsPanel");
        Transform creditsParent = pausePanel.transform;
        Transform infoParent = pausePanel.transform;
        if (scene.name == "Main_Use_Scene" && settingsPanel != null)
        {
            creditsParent = settingsPanel.transform;
            infoParent = settingsPanel.transform;
        }

        Sprite infoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI_Metirial/info_credit/infoBtn.png");
        Sprite creditsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI_Metirial/info_credit/gameCredits.png");
        Sprite creditsBackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UI_Metirial/setting/menuBack.png");

        GameObject infoObject = FindObject(scene, "infoCredits");
        if (infoObject == null)
        {
            infoObject = new GameObject(
                "infoCredits", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
        }

        if (infoObject.transform.parent != infoParent)
            infoObject.transform.SetParent(infoParent, false);

        RectTransform infoRect = infoObject.GetComponent<RectTransform>();
        infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 0.5f);
        infoRect.pivot = new Vector2(0.5f, 0.5f);
        infoRect.anchoredPosition = new Vector2(395f, 243.5f);
        infoRect.sizeDelta = new Vector2(100f, 100f);
        infoRect.localScale = Vector3.one * 0.71717f;

        Image infoImage = infoObject.GetComponent<Image>();
        if (infoImage == null)
            infoImage = infoObject.AddComponent<Image>();

        infoImage.sprite = infoSprite;
        infoImage.color = Color.white;
        infoImage.preserveAspect = true;
        infoImage.raycastTarget = true;

        Button infoButton = infoObject.GetComponent<Button>();
        if (infoButton == null)
            infoButton = infoObject.AddComponent<Button>();

        infoButton.targetGraphic = infoImage;
        Navigation infoNavigation = infoButton.navigation;
        infoNavigation.mode = Navigation.Mode.None;
        infoButton.navigation = infoNavigation;

        GameObject creditsPanel = FindObject(scene, "CreditsPanel");
        if (creditsPanel == null)
        {
            creditsPanel = new GameObject(
                "CreditsPanel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup));
        }

        if (creditsPanel.transform.parent != creditsParent)
            creditsPanel.transform.SetParent(creditsParent, false);

        RectTransform panelRect = creditsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = new Vector2(-300f, -300f);
        panelRect.offsetMax = new Vector2(300f, 300f);
        panelRect.localScale = Vector3.one;

        Image blocker = creditsPanel.GetComponent<Image>();
        blocker.sprite = null;
        blocker.color = new Color(0f, 0f, 0f, 0.84f);
        blocker.raycastTarget = true;

        CanvasGroup group = creditsPanel.GetComponent<CanvasGroup>();
        if (group == null)
            group = creditsPanel.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject creditsCard = null;
        Transform cardTransform = creditsPanel.transform.Find("CreditsCard");
        if (cardTransform != null)
            creditsCard = cardTransform.gameObject;

        if (creditsCard == null)
        {
            Image[] oldImages = infoObject.GetComponentsInChildren<Image>(true);
            foreach (Image oldImage in oldImages)
            {
                if (oldImage.gameObject != infoObject && oldImage.sprite == creditsSprite)
                {
                    creditsCard = oldImage.gameObject;
                    creditsCard.name = "CreditsCard";
                    creditsCard.transform.SetParent(creditsPanel.transform, false);
                    break;
                }
            }
        }

        if (creditsCard == null)
        {
            creditsCard = new GameObject(
                "CreditsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            creditsCard.transform.SetParent(creditsPanel.transform, false);
        }

        creditsCard.SetActive(true);
        RectTransform cardRect = creditsCard.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(981f, 780f);
        cardRect.localScale = Vector3.one;

        Image cardImage = creditsCard.GetComponent<Image>();
        cardImage.sprite = creditsSprite;
        cardImage.color = Color.white;
        cardImage.preserveAspect = true;
        cardImage.raycastTarget = false;

        GameObject backObject = FindObject(scene, "creditsBackBtn");
        if (backObject == null)
        {
            backObject = new GameObject(
                "creditsBackBtn", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            backObject.transform.SetParent(creditsPanel.transform, false);
        }
        else if (backObject.transform.parent != creditsPanel.transform)
        {
            backObject.transform.SetParent(creditsPanel.transform, false);
        }

        RectTransform backRect = backObject.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = new Vector2(0f, -280f);
        backRect.sizeDelta = new Vector2(500f, 100f);
        backRect.localScale = Vector3.one;

        Image backImage = backObject.GetComponent<Image>();
        backImage.sprite = creditsBackSprite;
        backImage.color = Color.white;
        backImage.preserveAspect = true;
        backImage.raycastTarget = true;

        backObject.transform.SetAsLastSibling();

        Button backButton = backObject.GetComponent<Button>();
        backButton.targetGraphic = backImage;
        Navigation backNavigation = backButton.navigation;
        backNavigation.mode = Navigation.Mode.None;
        backButton.navigation = backNavigation;

        uiManager.infoCreditsButton = infoButton;
        uiManager.creditsPanel = creditsPanel;
        uiManager.creditsBackButton = backButton;
        uiManager.BindCreditsButtons();

        creditsPanel.SetActive(false);
        EditorUtility.SetDirty(infoObject);
        EditorUtility.SetDirty(creditsPanel);
        EditorUtility.SetDirty(uiManager);
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
