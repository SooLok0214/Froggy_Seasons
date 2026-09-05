using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public const string MyKillsKey = "MyKills";
    public const string MySurvivalKey = "MySurvivalTime";
    public const string MyLevelKey = "MyLevel";

    public const string BestKillsKey = "BestKills";
    public const string BestSurvivalKey = "BestSurvivalTime";
    public const string BestLevelKey = "BestLevel";

    public RectTransform scorePanel;

    public Text killsText;
    public Text survivalText;
    public Text levelText;

    public Text bestKillsText;
    public Text bestSurvivalText;
    public Text bestLevelText;

    public Font cinzelFont;

    public int kills;
    public float survivalTime;
    public int currentLevel = 1;

    public bool recording;
    public PlayerStats playerStats;
    public int displayedSurvivalSecond = -1;


    public void Awake()
    {
        instance = this;
    }


    public void Start()
    {
        BuildScoreDisplay();
        UpdateScoreDisplay();
    }


    public void Update()
    {
        if (!recording)
            return;

        survivalTime += Time.deltaTime;

        int survivalSecond = Mathf.FloorToInt(survivalTime);

        if (survivalSecond != displayedSurvivalSecond)
        {
            displayedSurvivalSecond = survivalSecond;

            if (survivalText != null)
                survivalText.text = FormatTime(survivalTime);
        }
    }


    public void StartScore()
    {
        kills = 0;
        survivalTime = 0;
        currentLevel = 1;

        recording = true;
        displayedSurvivalSecond = -1;

        UpdateScoreDisplay();
    }


    public void StopScore()
    {
        recording = false;

        // 保存本次成績
        PlayerPrefs.SetInt(MyKillsKey, kills);
        PlayerPrefs.SetFloat(MySurvivalKey, survivalTime);
        PlayerPrefs.SetInt(MyLevelKey, currentLevel);


        // 保存最高擊殺
        if (!PlayerPrefs.HasKey(BestKillsKey) ||
            kills > PlayerPrefs.GetInt(BestKillsKey))
        {
            PlayerPrefs.SetInt(BestKillsKey, kills);
        }


        // 保存最高生存時間
        if (!PlayerPrefs.HasKey(BestSurvivalKey) ||
            survivalTime > PlayerPrefs.GetFloat(BestSurvivalKey))
        {
            PlayerPrefs.SetFloat(BestSurvivalKey, survivalTime);
        }


        // 保存最高等級
        if (!PlayerPrefs.HasKey(BestLevelKey) ||
            currentLevel > PlayerPrefs.GetInt(BestLevelKey))
        {
            PlayerPrefs.SetInt(BestLevelKey, currentLevel);
        }


        PlayerPrefs.Save();

        UpdateScoreDisplay();
    }


    public void AddKill()
    {
        if (!recording)
            return;

        kills++;

        if (playerStats != null)
        {
            playerStats.AddExperience(playerStats.expPerKill);
        }


        UpdateScoreDisplay();
    }


    public void SetLevel(int level)
    {
        if (!recording)
            return;

        currentLevel = Mathf.Max(1, level);

        UpdateScoreDisplay();
    }


    public void BuildScoreDisplay()
    {
        if (scorePanel == null)
            return;


        // =========================
        // 上面：本次遊戲分數
        // =========================

        killsText = CreateScoreText(
            "KillValue",
            killsText,
            new Vector2(-218, -20),
            52,
            255
        );


        survivalText = CreateScoreText(
            "SurvivalValue",
            survivalText,
            new Vector2(0, -20),
            52,
            255
        );


        levelText = CreateScoreText(
            "LevelValue",
            levelText,
            new Vector2(218, -20),
            52,
            255
        );


        // =========================
        // 底下：最高紀錄
        // 更小 + 往下 + 30%透明
        // =========================

        bestKillsText = CreateScoreText(
            "BestKillValue",
            bestKillsText,
            new Vector2(-218, -68),
            22,
            200
        );


        bestSurvivalText = CreateScoreText(
            "BestSurvivalValue",
            bestSurvivalText,
            new Vector2(0, -68),
            22,
            200
        );


        bestLevelText = CreateScoreText(
            "BestLevelValue",
            bestLevelText,
            new Vector2(218, -68),
            22,
            200
        );
    }


    public Text CreateScoreText(
        string objectName,
        Text textField,
        Vector2 position,
        int fontSize,
        byte alpha
    )
    {
        // 如果 Inspector 沒有拉 Text
        // 就先找現有物件
        if (textField == null)
        {
            Transform existing = scorePanel.Find(objectName);

            if (existing != null)
            {
                textField = existing.GetComponent<Text>();
            }
        }


        // 如果完全沒有
        // 就自動建立
        if (textField == null)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );


            textObject.transform.SetParent(scorePanel, false);

            textField = textObject.GetComponent<Text>();
        }


        RectTransform rect = textField.rectTransform;


        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);


        rect.anchoredPosition = position;

        rect.sizeDelta = new Vector2(210, 100);


        // 字體
        textField.font = cinzelFont;

        textField.fontSize = fontSize;

        textField.fontStyle = FontStyle.Bold;


        // 對齊
        textField.alignment = TextAnchor.MiddleCenter;


        // 避免文字被裁掉
        textField.horizontalOverflow =
            HorizontalWrapMode.Overflow;

        textField.verticalOverflow =
            VerticalWrapMode.Overflow;


        // 顏色
        // 255 = 100%透明度
        // 77 ≈ 30%透明度
        textField.color =
            new Color32(62, 49, 39, alpha);


        textField.raycastTarget = false;


        return textField;
    }


    public void UpdateScoreDisplay()
    {
        if (killsText == null)
            BuildScoreDisplay();


        // =========================
        // 本次分數
        // =========================

        if (killsText != null)
        {
            killsText.text =
                kills.ToString();
        }


        if (survivalText != null)
        {
            survivalText.text =
                FormatTime(survivalTime);
        }


        if (levelText != null)
        {
            levelText.text =
                currentLevel.ToString();
        }


        // =========================
        // 最高紀錄
        // =========================

        if (bestKillsText != null)
        {
            bestKillsText.text =
                PlayerPrefs.GetInt(
                    BestKillsKey,
                    0
                ).ToString();
        }


        if (bestSurvivalText != null)
        {
            bestSurvivalText.text =
                FormatTime(
                    PlayerPrefs.GetFloat(
                        BestSurvivalKey,
                        0
                    )
                );
        }


        if (bestLevelText != null)
        {
            bestLevelText.text =
                PlayerPrefs.GetInt(
                    BestLevelKey,
                    1
                ).ToString();
        }
    }


    public string FormatTime(float seconds)
    {
        int totalSeconds =
            Mathf.FloorToInt(seconds);

        int minutes =
            totalSeconds / 60;

        int remainingSeconds =
            totalSeconds % 60;


        return
            minutes.ToString("00")
            + ":"
            + remainingSeconds.ToString("00");
    }
}
