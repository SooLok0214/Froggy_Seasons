using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public const string MainMenuScene = "Main_Use_Scene";
    public const string GameplayScene = "InGameScene";

    public UIManager uiManager;
    public ScoreManager scoreManager;
    public MusicManager musicManager;
    public EnemySpawner enemySpawner;

    [Header("Scene Loading")]
    [Tooltip("Loading UI used for scene changes between the main menu and gameplay.")]
    public LoadingScreenController loadingScreen;

    public bool gameStarted;

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        if (SceneManager.GetActiveScene().name == GameplayScene)
            GameStart();
        else
            ShowMainMenu();
    }

    // 首頁 START 按鈕使用。
    public void StartGame()
    {
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name != GameplayScene)
        {
            if (loadingScreen == null)
                loadingScreen = FindAnyObjectByType<LoadingScreenController>(FindObjectsInactive.Include);

            if (loadingScreen != null)
            {
                loadingScreen.LoadScene(GameplayScene);
                return;
            }

            SceneManager.LoadScene(GameplayScene);
            return;
        }

        GameStart();
    }

    // 所有 Manager 的遊戲開始入口，方式跟 01 ZigZag 相同。
    public void GameStart()
    {
        if (gameStarted)
            return;

        gameStarted = true;
        Time.timeScale = 1f;

        if (uiManager != null)
            uiManager.GameStartUI();

        if (scoreManager != null)
            scoreManager.StartScore();

        if (musicManager != null)
            musicManager.PlayInGameMusic();

        if (enemySpawner != null)
            enemySpawner.StartSpawning();
    }

    // 玩家、死亡區域等只需要呼叫這一個入口。
    public void GameOver()
    {
        if (!gameStarted)
            return;

        gameStarted = false;

        if (uiManager != null)
            uiManager.CancelLevelUpChoices();

        if (enemySpawner != null)
            enemySpawner.StopSpawning();

        if (scoreManager != null)
            scoreManager.StopScore();

        if (musicManager != null)
            musicManager.PlayGameOverMusic();

        if (uiManager != null)
            uiManager.GameOverUI();

        Time.timeScale = 0f;
    }

    public void BackToStartMenu()
    {
        gameStarted = false;
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name == GameplayScene)
        {
            if (loadingScreen == null)
                loadingScreen = FindAnyObjectByType<LoadingScreenController>(FindObjectsInactive.Include);

            if (loadingScreen != null)
            {
                loadingScreen.LoadScene(MainMenuScene);
                return;
            }

            SceneManager.LoadScene(MainMenuScene);
            return;
        }

        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        gameStarted = false;
        Time.timeScale = 1f;

        if (uiManager != null)
            uiManager.ShowMainMenuUI();

        if (musicManager != null && !BootLogoSplash.IsShowing)
            musicManager.PlayHomeMusic();
    }
}
