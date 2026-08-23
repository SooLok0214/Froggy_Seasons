using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public const string MainMenuScene = "Main_Use_Scene";
    public const string GameplayScene = "InGameScene";

    public MusicManager musicManager;
    public ScoreManager scoreManager;

    public Button pauseResumeBtn;
    public Sprite pauseImg;
    public Sprite resumeImg;

    public GameObject startPanel;
    public GameObject homeMenu;

    public GameObject pausePanel;
    public GameObject pauseMenu;
    public GameObject settingsPanel;
    public GameObject menuBack;

    public GameObject gameOverPanel;
    public GameObject joystickObject;

    public bool gameStarted;
    public bool settingsOpenedFromHome;

    public void Start()
    {
        if (MusicManager.instance != null)
            musicManager = MusicManager.instance;

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (SceneManager.GetActiveScene().name == GameplayScene)
            BeginGameplayScene();
        else
            ShowMainMenu();
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(GameplayScene);
    }

    public void BeginGameplayScene()
    {
        gameStarted = true;
        settingsOpenedFromHome = false;
        Time.timeScale = 1;

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (scoreManager != null)
            scoreManager.StartScore();

        if (musicManager != null)
            musicManager.PlayInGameMusic();

        SetActive(startPanel, false);
        SetActive(homeMenu, false);
        SetActive(gameOverPanel, false);
        HidePauseUI();

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(true);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }

        SetActive(joystickObject, true);
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
            SetActive(pausePanel, true);
            SetActive(pauseMenu, true);
            SetActive(settingsPanel, false);
            SetActive(menuBack, false);
            SetActive(joystickObject, false);
        }
        else
        {
            HidePauseUI();
            SetActive(joystickObject, true);
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
    }

    public void BackToPauseMenu()
    {
        if (settingsOpenedFromHome || !gameStarted)
        {
            settingsOpenedFromHome = false;
            HidePauseUI();
            SetActive(homeMenu, true);
            return;
        }

        SetActive(pauseMenu, true);
        SetActive(settingsPanel, false);
        SetActive(menuBack, false);
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
    }

    public void GameOver()
    {
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (scoreManager != null)
            scoreManager.StopScore();

        gameStarted = false;
        settingsOpenedFromHome = false;
        Time.timeScale = 0;

        if (musicManager != null)
            musicManager.PlayGameOverMusic();

        HidePauseUI();

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(false);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }

        SetActive(joystickObject, false);
        SetActive(gameOverPanel, true);

        if (gameOverPanel != null)
            gameOverPanel.transform.SetAsLastSibling();
    }

    public void BackToStartMenu()
    {
        if (SceneManager.GetActiveScene().name == GameplayScene)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(MainMenuScene);
            return;
        }

        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        gameStarted = false;
        settingsOpenedFromHome = false;
        Time.timeScale = 1;

        if (musicManager != null)
            musicManager.PlayHomeMusic();

        SetActive(startPanel, true);
        SetActive(gameOverPanel, false);
        SetActive(homeMenu, true);
        HidePauseUI();

        if (pauseResumeBtn != null)
        {
            pauseResumeBtn.gameObject.SetActive(false);
            pauseResumeBtn.image.overrideSprite = pauseImg;
        }

        SetActive(joystickObject, false);
    }

    public void HidePauseUI()
    {
        SetActive(pausePanel, false);
        SetActive(pauseMenu, false);
        SetActive(settingsPanel, false);
        SetActive(menuBack, false);
    }

    public void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
