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

    void Start()
    {
        Time.timeScale = 1;
        gameStarted = false;

        SetupSettingsMenu();

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

    public void OpenSettings()
    {
        if (!gameStarted)
        {
            return;
        }

        pauseMenu.SetActive(false);
        settingsPanel.SetActive(true);
        menuBack.SetActive(true);
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
        }
        else
        {
            Time.timeScale = 0;

            pauseResumeBtn.image.overrideSprite = resumeImg;

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
    }
}