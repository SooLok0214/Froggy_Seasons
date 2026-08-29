using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public enum SkillType
    {
        IncreaseAttack,
        IncreaseMaxHealth,
        RestoreHealth
    }

    public Button pauseResumeBtn;
    public Sprite pauseImg;
    public Sprite resumeImg;

    public GameObject startPanel;
    public GameObject homeMenu;

    public GameObject pausePanel;
    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject menuBack;

    [Header("Credits")]
    public Button infoCreditsButton;
    public GameObject creditsPanel;
    public Button creditsBackButton;

    public GameObject gameOverPanel;

    // Gameplay UI
    public GameObject joystickObject;
    public GameObject characterInfo;
    public GameObject attackButton;
    public GameObject scoreBar;
    public GameObject gameplayEmpty;
    public GameObject crosshair;
    public ThirdPersonCamera thirdPersonCamera;

    [Header("Gameplay HUD")]
    public ScoreManager scoreManager;
    public PlayerStats playerStats;
    public RectTransform scoreBarRect;
    public RectTransform infoBarRect;
    public Image healthLine;
    public Image expLine;
    public Text liveKillsText;
    public Text liveTimeText;
    public Text healthText;
    public Text levelText;
    public Font cinzelFont;

    [Header("Level Up Choices")]
    public Sprite addDamageSprite;
    public Sprite addHealthSprite;
    public Sprite restoreHealthSprite;
    public Vector2 cardSize = new Vector2(357f, 600f);
    public float cardGap = 90f;
    public Color overlayColor = new Color(0f, 0f, 0f, 0.82f);
    public float overlayOverscan = 300f;
    public GameObject levelUpOverlay;
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public Canvas rootCanvas;
    public PlayerStats levelUpPlayer;
    public bool levelUpSelectionOpen;

    public bool gameStarted;
    public bool settingsOpenedFromHome;

    [System.NonSerialized] public int displayedKills = -1;
    [System.NonSerialized] public int displayedTime = -1;
    [System.NonSerialized] public float displayedHealth = -1f;
    [System.NonSerialized] public float displayedMaxHealth = -1f;
    [System.NonSerialized] public float displayedExp = -1f;
    [System.NonSerialized] public float displayedExpTarget = -1f;
    [System.NonSerialized] public int displayedLevel = -1;

    public void Start()
    {
        BindCreditsButtons();
        BuildHUD();
        BuildLevelUpUI();
        SetLevelUpOverlayActive(false);
        SetActive(creditsPanel, false);
        ResetHUDCache();
        UpdateHUD();
    }

    public void Update()
    {
        UpdateHUD();
    }

    public void StartGame()
    {
        if (GameManager.instance != null)
            GameManager.instance.StartGame();
    }

    public void BeginGameplayScene()
    {
        if (GameManager.instance != null)
            GameManager.instance.GameStart();
    }

    // 只處理遊戲開始時的 UI，由 GameManager 呼叫。
    public void GameStartUI()
    {
        gameStarted = true;
        settingsOpenedFromHome = false;

        SetActive(startPanel, false);
        SetActive(homeMenu, false);
        SetActive(gameOverPanel, false);
        HidePauseUI();

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(true);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }

        ShowGameplayUI();
        SetCameraGameplayControl(true);
        ResetHUDCache();
        UpdateHUD();
    }

    public void PauseResume()
    {
        if (!gameStarted)
            return;

        bool pausing = Time.timeScale == 1;
        Time.timeScale = pausing ? 0 : 1;

        if (pauseResumeBtn != null)
            pauseResumeBtn.image.overrideSprite = pausing ? resumeImg : pauseImg;

        if (pausing)
        {
            SetCameraGameplayControl(false);
            SetActive(pausePanel, true);
            SetActive(pauseMenu, true);
            SetActive(settingsPanel, false);
            SetActive(menuBack, false);
            SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, true);
            SetActive(creditsPanel, false);
            HideGameplayUI();
        }
        else
        {
            HidePauseUI();
            ShowGameplayUI();
            SetCameraGameplayControl(true);
        }
    }

    public void OpenSettings()
    {
        if (!gameStarted)
            return;

        settingsOpenedFromHome = false;
        SetActive(pauseMenu, false);
        SetActive(settingsPanel, true);
        SetActive(menuBack, true);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, false);
        SetActive(creditsPanel, false);
        HideGameplayUI();
    }

    public void OpenCredits()
    {
        if (!gameStarted)
            return;

        bool creditsInsideSettings = creditsPanel != null && settingsPanel != null &&
                                     creditsPanel.transform.IsChildOf(settingsPanel.transform);

        Time.timeScale = 0f;
        SetCameraGameplayControl(false);
        SetActive(pausePanel, true);
        SetActive(pauseMenu, false);
        SetActive(settingsPanel, creditsInsideSettings);
        SetActive(menuBack, false);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, false);
        SetActive(creditsPanel, true);
        HideGameplayUI();

        if (creditsPanel != null)
            creditsPanel.transform.SetAsLastSibling();
    }

    public void BackFromCredits()
    {
        if (!gameStarted)
            return;

        bool creditsInsideSettings = creditsPanel != null && settingsPanel != null &&
                                     creditsPanel.transform.IsChildOf(settingsPanel.transform);

        SetActive(creditsPanel, false);
        SetActive(pausePanel, true);
        SetActive(pauseMenu, !creditsInsideSettings);
        SetActive(settingsPanel, creditsInsideSettings);
        SetActive(menuBack, creditsInsideSettings);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, true);
        HideGameplayUI();
    }

    public void BindCreditsButtons()
    {
        if (infoCreditsButton != null)
        {
            infoCreditsButton.onClick.RemoveListener(OpenCredits);
            infoCreditsButton.onClick.AddListener(OpenCredits);
        }

        if (creditsBackButton != null)
        {
            creditsBackButton.onClick.RemoveListener(BackFromCredits);
            creditsBackButton.onClick.AddListener(BackFromCredits);
        }
    }

    public void BackToPauseMenu()
    {
        if (settingsOpenedFromHome || !gameStarted)
        {
            settingsOpenedFromHome = false;
            HidePauseUI();
            SetActive(homeMenu, true);
            HideGameplayUI();
            return;
        }

        SetActive(pauseMenu, true);
        SetActive(settingsPanel, false);
        SetActive(menuBack, false);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, true);
        SetActive(creditsPanel, false);
        HideGameplayUI();
    }

    public void OpenHomeSettings()
    {
        if (gameStarted)
            return;

        settingsOpenedFromHome = true;
        SetActive(homeMenu, false);
        SetActive(pausePanel, true);
        SetActive(pauseMenu, false);
        SetActive(settingsPanel, true);
        SetActive(menuBack, true);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, true);
        SetActive(creditsPanel, false);
        HideGameplayUI();
    }

    public void GameOver()
    {
        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }

    // 只處理死亡時的 UI，由 GameManager 呼叫。
    public void GameOverUI()
    {
        gameStarted = false;
        settingsOpenedFromHome = false;

        HidePauseUI();
        HideGameplayUI();
        HideGameplayObjectsByName();
        SetCameraGameplayControl(false);
        CancelLevelUpChoices();

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(false);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }

        SetActive(gameOverPanel, true);

        if (gameOverPanel != null)
            gameOverPanel.transform.SetAsLastSibling();
    }

    public void BackToStartMenu()
    {
        if (GameManager.instance != null)
            GameManager.instance.BackToStartMenu();
    }

    public void ShowMainMenu()
    {
        if (GameManager.instance != null)
            GameManager.instance.ShowMainMenu();
    }

    // 只處理首頁 UI，由 GameManager 呼叫。
    public void ShowMainMenuUI()
    {
        gameStarted = false;
        settingsOpenedFromHome = false;

        SetActive(startPanel, true);
        SetActive(gameOverPanel, false);
        SetActive(homeMenu, true);
        HidePauseUI();
        HideGameplayUI();
        SetCameraGameplayControl(false);

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(false);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }
    }

    public void ShowGameplayUI()
    {
        SetActive(joystickObject, true);
        SetActive(characterInfo, true);
        SetActive(attackButton, true);
        SetActive(scoreBar, true);
        SetActive(gameplayEmpty, true);
    }

    public void HideGameplayUI()
    {
        SetActive(joystickObject, false);
        SetActive(characterInfo, false);
        SetActive(attackButton, false);
        SetActive(scoreBar, false);
        SetActive(gameplayEmpty, false);
    }

    public void HideGameplayObjectsByName()
    {
        SetActive(characterInfo, false);
        SetActive(crosshair, false);
    }

    public void SetCameraGameplayControl(bool active)
    {
        if (thirdPersonCamera != null)
            thirdPersonCamera.SetGameplayControl(active);
    }

    public void HidePauseUI()
    {
        SetActive(pausePanel, false);
        SetActive(pauseMenu, false);
        SetActive(settingsPanel, false);
        SetActive(menuBack, false);
        SetActive(infoCreditsButton == null ? null : infoCreditsButton.gameObject, false);
        SetActive(creditsPanel, false);
    }

    public void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    public void ShowLevelUpChoices(PlayerStats player)
    {
        if (levelUpSelectionOpen || player == null || player.isDead ||
            GameManager.instance == null || !GameManager.instance.gameStarted)
            return;

        levelUpPlayer = player;
        BuildLevelUpUI();

        int first = Random.Range(0, 3);
        int second = Random.Range(0, 2);
        if (second >= first)
            second++;

        SetupChoiceButton(leftChoiceButton, (SkillType)first);
        SetupChoiceButton(rightChoiceButton, (SkillType)second);

        levelUpSelectionOpen = true;
        SetLevelUpOverlayActive(true);
        levelUpOverlay.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        SetCameraGameplayControl(false);
    }

    public void SetupChoiceButton(Button button, SkillType skill)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = skill == SkillType.IncreaseAttack
                ? addDamageSprite
                : skill == SkillType.IncreaseMaxHealth
                    ? addHealthSprite
                    : restoreHealthSprite;
            image.preserveAspect = true;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectLevelUpSkill(skill));
    }

    public void SelectLevelUpSkill(SkillType skill)
    {
        if (!levelUpSelectionOpen || levelUpPlayer == null)
            return;

        if (MusicManager.instance != null)
            MusicManager.instance.PlayButtonClick();

        if (skill == SkillType.IncreaseAttack)
            levelUpPlayer.IncreaseAttack(10f);
        else if (skill == SkillType.IncreaseMaxHealth)
            levelUpPlayer.IncreaseMaxHealth(50f);
        else
            levelUpPlayer.Heal(30f);

        CloseLevelUpChoices();
    }

    public void CloseLevelUpChoices()
    {
        levelUpSelectionOpen = false;
        levelUpPlayer = null;
        SetLevelUpOverlayActive(false);

        if (GameManager.instance != null && GameManager.instance.gameStarted)
        {
            Time.timeScale = 1f;
            SetCameraGameplayControl(true);
        }
    }

    public void CancelLevelUpChoices()
    {
        levelUpSelectionOpen = false;
        levelUpPlayer = null;
        SetLevelUpOverlayActive(false);
    }

    public void BuildLevelUpUI()
    {
        if (rootCanvas == null)
            return;

        if (levelUpOverlay == null)
        {
            Transform existing = rootCanvas.transform.Find("LevelUpChoiceOverlay");
            if (existing != null)
                levelUpOverlay = existing.gameObject;
        }

        if (levelUpOverlay == null)
        {
            levelUpOverlay = new GameObject(
                "LevelUpChoiceOverlay",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup)
            );
            levelUpOverlay.transform.SetParent(rootCanvas.transform, false);
        }

        RectTransform overlayRect = levelUpOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.offsetMin = new Vector2(-overlayOverscan, -overlayOverscan);
        overlayRect.offsetMax = new Vector2(overlayOverscan, overlayOverscan);

        levelUpOverlay.GetComponent<Image>().color = overlayColor;
        CanvasGroup group = levelUpOverlay.GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

        leftChoiceButton = BuildChoiceButton(
            "LevelUpChoiceLeft",
            new Vector2(-(cardSize.x + cardGap) * 0.5f, 0f)
        );
        rightChoiceButton = BuildChoiceButton(
            "LevelUpChoiceRight",
            new Vector2((cardSize.x + cardGap) * 0.5f, 0f)
        );
    }

    public Button BuildChoiceButton(string objectName, Vector2 position)
    {
        Transform existing = levelUpOverlay.transform.Find(objectName);
        GameObject buttonObject = existing == null
            ? new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button))
            : existing.gameObject;

        if (existing == null)
            buttonObject.transform.SetParent(levelUpOverlay.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = cardSize;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        button.colors = colors;
        return button;
    }

    public void SetLevelUpOverlayActive(bool active)
    {
        if (levelUpOverlay != null)
            levelUpOverlay.SetActive(active);
    }

    public void BuildHUD()
    {
        Color scoreColor = new Color32(62, 49, 39, 255);

        if (scoreBarRect != null)
        {
            liveKillsText = CreateText(scoreBarRect, "LiveKillValue", liveKillsText,
                new Vector2(-160f, -18f), new Vector2(300f, 90f), 44, scoreColor);
            liveTimeText = CreateText(scoreBarRect, "LiveTimeValue", liveTimeText,
                new Vector2(150f, -18f), new Vector2(300f, 90f), 44, scoreColor);
        }

        if (infoBarRect != null)
        {
            healthText = CreateText(infoBarRect, "HealthValue", healthText,
                new Vector2(1020f, -20f), new Vector2(1000f, 300f), 60,
                new Color32(255, 250, 226, 255));
            levelText = CreateText(infoBarRect, "LevelValue", levelText,
                new Vector2(-1195f, -185f), new Vector2(260f, 150f), 92, scoreColor);
        }

        PrepareBar(healthLine);
        PrepareBar(expLine);
    }

    public void ResetHUDCache()
    {
        displayedKills = -1;
        displayedTime = -1;
        displayedHealth = -1f;
        displayedMaxHealth = -1f;
        displayedExp = -1f;
        displayedExpTarget = -1f;
        displayedLevel = -1;
    }

    public void PrepareBar(Image bar)
    {
        if (bar == null)
            return;

        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;
        bar.fillClockwise = true;
        bar.preserveAspect = true;
        bar.raycastTarget = false;
    }

    public Text CreateText(RectTransform parent, string objectName, Text currentText,
        Vector2 position, Vector2 size, int fontSize, Color color)
    {
        if (currentText == null)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
                currentText = existing.GetComponent<Text>();
        }

        if (currentText == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            currentText = textObject.GetComponent<Text>();
        }

        RectTransform rect = currentText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        currentText.font = cinzelFont != null
            ? cinzelFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        currentText.fontSize = fontSize;
        currentText.fontStyle = FontStyle.Bold;
        currentText.alignment = TextAnchor.MiddleCenter;
        currentText.color = color;
        currentText.raycastTarget = false;
        currentText.horizontalOverflow = HorizontalWrapMode.Overflow;
        currentText.verticalOverflow = VerticalWrapMode.Overflow;
        return currentText;
    }

    public void UpdateHUD()
    {
        if (scoreManager != null)
        {
            if (liveKillsText != null && displayedKills != scoreManager.kills)
            {
                displayedKills = scoreManager.kills;
                liveKillsText.text = scoreManager.kills.ToString("D3");
            }

            int second = Mathf.FloorToInt(scoreManager.survivalTime);
            if (liveTimeText != null && displayedTime != second)
            {
                displayedTime = second;
                liveTimeText.text = scoreManager.FormatTime(scoreManager.survivalTime);
            }
        }

        if (playerStats == null)
            return;

        if (!Mathf.Approximately(displayedHealth, playerStats.currentHealth) ||
            !Mathf.Approximately(displayedMaxHealth, playerStats.maxHealth))
        {
            displayedHealth = playerStats.currentHealth;
            displayedMaxHealth = playerStats.maxHealth;
            if (healthLine != null)
                healthLine.fillAmount = Mathf.Clamp01(
                    playerStats.currentHealth / Mathf.Max(1f, playerStats.maxHealth));
            if (healthText != null)
                healthText.text = Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentHealth)) +
                    " / " + Mathf.CeilToInt(Mathf.Max(1f, playerStats.maxHealth));
        }

        if (!Mathf.Approximately(displayedExp, playerStats.currentExp) ||
            !Mathf.Approximately(displayedExpTarget, playerStats.expToLevel))
        {
            displayedExp = playerStats.currentExp;
            displayedExpTarget = playerStats.expToLevel;
            if (expLine != null)
                expLine.fillAmount = Mathf.Clamp01(
                    playerStats.currentExp / Mathf.Max(1f, playerStats.expToLevel));
        }

        if (levelText != null && displayedLevel != playerStats.currentLevel)
        {
            displayedLevel = playerStats.currentLevel;
            levelText.text = playerStats.currentLevel.ToString();
        }
    }
}
