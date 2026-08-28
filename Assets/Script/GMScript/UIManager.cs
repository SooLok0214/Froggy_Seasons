using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
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

    // Gameplay UI
    public GameObject joystickObject;
    public GameObject characterInfo;
    public GameObject attackButton;
    public GameObject scoreBar;
    public GameObject gameplayEmpty;
    public GameObject crosshair;
    public ThirdPersonCamera thirdPersonCamera;

    public bool gameStarted;
    public bool settingsOpenedFromHome;

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
            HideGameplayUI();
        }
        else
        {
            HidePauseUI();
            ShowGameplayUI();
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
        HideGameplayUI();
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
    }

    public void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
