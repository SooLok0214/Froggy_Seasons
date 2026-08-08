using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public MusicManager musicManager;

    public Button pauseResumeBtn;
    public Sprite pauseImg;
    public Sprite resumeImg;

    public GameObject pausePanel;
    public GameObject startPanel;
    public GameObject tapToStart;
    public GameObject gameOverPanel;

    public Button testGameStateBtn;

    public bool gameStarted;

    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject menuBack;
    public GameObject joystickObject;

    void Start()
    {
        Time.timeScale = 1;
        gameStarted = false;

        SetupSettingsMenu();
        SetJoystickVisible(false);

        startPanel.SetActive(true);

        pausePanel.SetActive(false);
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        menuBack.SetActive(false);

        pauseResumeBtn.gameObject.SetActive(false);
        pauseResumeBtn.interactable = false;

        gameOverPanel.SetActive(false);

        testGameStateBtn.gameObject.SetActive(true);
        UpdateTestButtonLabel("TEST START");

        musicManager.PlayHomeMusic();
    }

    public void SetupSettingsMenu()
    {
        foreach (Button button in pausePanel.GetComponentsInChildren<Button>(true))
        {
            if (button.name.Contains("Setting"))
            {
                button.onClick.AddListener(OpenSettings);
                break;
            }
        }

        menuBack.GetComponent<Button>().onClick.AddListener(BackToPauseMenu);
    }

    private void FindJoystick()
    {
        if (joystickObject != null)
        {
            ConfigureJoystickArea();
            return;
        }

        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.name == "Variable Joystick" && candidate.scene.IsValid())
            {
                joystickObject = candidate;
                ConfigureJoystickArea();
                break;
            }
        }
    }

    private void ConfigureJoystickArea()
    {
        if (joystickObject == null)
        {
            return;
        }

        RectTransform joystickRect = joystickObject.GetComponent<RectTransform>();
        Canvas canvas = joystickObject.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        if (joystickRect == null || canvasRect == null)
        {
            return;
        }

        // Floating Joystick can only start inside the left, middle-lower screen area.
        // Keep its existing scale so the joystick artwork size and proportions do not change.
        float scaleX = Mathf.Max(Mathf.Abs(joystickRect.localScale.x), 0.001f);
        float scaleY = Mathf.Max(Mathf.Abs(joystickRect.localScale.y), 0.001f);
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        joystickRect.anchorMin = Vector2.zero;
        joystickRect.anchorMax = Vector2.zero;
        joystickRect.pivot = Vector2.zero;
        joystickRect.anchoredPosition = new Vector2(canvasWidth * 0.05f, canvasHeight * 0.10f);
        joystickRect.sizeDelta = new Vector2(
            canvasWidth * 0.40f / scaleX,
            canvasHeight * 0.45f / scaleY
        );
    }

    private void SetJoystickVisible(bool visible)
    {
        FindJoystick();

        if (joystickObject != null)
        {
            joystickObject.SetActive(visible);
        }
    }

    public void OpenSettings()
    {
        if (!gameStarted)
        {
            return;
        }

        pauseMenu.SetActive(false);
        settingsPanel.SetActive(true);
        menuBack.SetActive(true);
        SetJoystickVisible(false);
    }

    public void BackToPauseMenu()
    {
        if (!gameStarted)
        {
            return;
        }

        ResetPauseMenu();
    }

    public void ResetPauseMenu()
    {
        pauseMenu.SetActive(true);
        settingsPanel.SetActive(false);
        menuBack.SetActive(false);
        SetJoystickVisible(false);
    }

    public void PauseResume()
    {
        if (!gameStarted)
        {
            return;
        }

        if (Time.timeScale == 0)
        {
            Time.timeScale = 1;

            pauseResumeBtn.image.overrideSprite = pauseImg;

            pausePanel.SetActive(false);
            pauseMenu.SetActive(false);
            settingsPanel.SetActive(false);
            menuBack.SetActive(false);
            SetJoystickVisible(true);
        }
        else
        {
            Time.timeScale = 0;

            pauseResumeBtn.image.overrideSprite = resumeImg;

            SetJoystickVisible(false);
            pausePanel.SetActive(true);
            ResetPauseMenu();
        }
    }

    public void TestGameState()
    {
        if (!gameStarted)
        {
            StartGame();
        }
        else
        {
            GameOver();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        Time.timeScale = 1;

        musicManager.PlayInGameMusic();

        startPanel.SetActive(false);

        if (tapToStart != null)
        {
            tapToStart.SetActive(false);
        }

        gameOverPanel.SetActive(false);

        pausePanel.SetActive(false);
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        menuBack.SetActive(false);

        pauseResumeBtn.image.overrideSprite = pauseImg;
        pauseResumeBtn.gameObject.SetActive(true);
        pauseResumeBtn.interactable = true;

        testGameStateBtn.gameObject.SetActive(true);
        UpdateTestButtonLabel("TEST DEATH");
        SetJoystickVisible(true);
    }

    public void GameOver()
    {
        gameStarted = false;
        Time.timeScale = 0;

        musicManager.PlayGameOverMusic();

        pausePanel.SetActive(false);
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        menuBack.SetActive(false);

        pauseResumeBtn.image.overrideSprite = pauseImg;
        pauseResumeBtn.gameObject.SetActive(true);
        pauseResumeBtn.interactable = false;

        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();

        UpdateTestButtonLabel("TEST START");
        testGameStateBtn.gameObject.SetActive(false);
        SetJoystickVisible(false);
    }

    public void UpdateTestButtonLabel(string labelText)
    {
        Text label = testGameStateBtn.GetComponentInChildren<Text>();

        if (label != null)
        {
            label.text = labelText;
        }
    }

    public void BackToStartMenu()
    {
        gameStarted = false;
        Time.timeScale = 1;

        musicManager.PlayHomeMusic();

        gameOverPanel.SetActive(false);

        pausePanel.SetActive(false);
        pauseMenu.SetActive(false);
        settingsPanel.SetActive(false);
        menuBack.SetActive(false);

        pauseResumeBtn.image.overrideSprite = pauseImg;
        pauseResumeBtn.gameObject.SetActive(false);
        pauseResumeBtn.interactable = false;

        startPanel.SetActive(true);

        if (tapToStart != null)
        {
            tapToStart.SetActive(true);
        }

        testGameStateBtn.gameObject.SetActive(true);
        UpdateTestButtonLabel("TEST START");
        SetJoystickVisible(false);
    }
}
