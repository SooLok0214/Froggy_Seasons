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

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        BuildScoreDisplay();
    }

    public void Update()
    {
        if (!recording)
            return;

        survivalTime += Time.deltaTime;
        UpdateScoreDisplay();
    }

    public void StartScore()
    {
        kills = 0;
        survivalTime = 0;
        currentLevel = 1;
        recording = true;
        UpdateScoreDisplay();
    }

    public void StopScore()
    {
        recording = false;

        PlayerPrefs.SetInt(MyKillsKey, kills);
        PlayerPrefs.SetFloat(MySurvivalKey, survivalTime);
        PlayerPrefs.SetInt(MyLevelKey, currentLevel);

        if (!PlayerPrefs.HasKey(BestKillsKey) || kills > PlayerPrefs.GetInt(BestKillsKey))
            PlayerPrefs.SetInt(BestKillsKey, kills);

        if (!PlayerPrefs.HasKey(BestSurvivalKey) || survivalTime > PlayerPrefs.GetFloat(BestSurvivalKey))
            PlayerPrefs.SetFloat(BestSurvivalKey, survivalTime);

        if (!PlayerPrefs.HasKey(BestLevelKey) || currentLevel > PlayerPrefs.GetInt(BestLevelKey))
            PlayerPrefs.SetInt(BestLevelKey, currentLevel);

        PlayerPrefs.Save();
        UpdateScoreDisplay();
    }

    public void AddKill()
    {
        if (!recording)
            return;

        kills++;
        UpdateScoreDisplay();
    }

    public void SetLevel(int level)
    {
        if (!recording)
            return;

        currentLevel = Mathf.Max(1, level);
        UpdateScoreDisplay();
    }

    public void LevelUp()
    {
        SetLevel(currentLevel + 1);
    }

    public void BuildScoreDisplay()
    {
        if (scorePanel == null)
            scorePanel = FindScorePanel();

        if (scorePanel == null)
            return;

        if (cinzelFont == null)
            cinzelFont = Resources.Load<Font>("Fonts/Cinzel-Bold");

        killsText = CreateScoreText("KillValue", killsText, new Vector2(-218, -20), 52);
        survivalText = CreateScoreText("SurvivalValue", survivalText, new Vector2(0, -20), 52);
        levelText = CreateScoreText("LevelValue", levelText, new Vector2(218, -20), 52);

        bestKillsText = CreateScoreText("BestKillValue", bestKillsText, new Vector2(-218, -55), 27);
        bestSurvivalText = CreateScoreText("BestSurvivalValue", bestSurvivalText, new Vector2(0, -55), 27);
        bestLevelText = CreateScoreText("BestLevelValue", bestLevelText, new Vector2(218, -55), 27);
    }

    public RectTransform FindScorePanel()
    {
        RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rect in rects)
        {
            if (rect.gameObject.scene.IsValid() && rect.name == "endScorePanel")
                return rect;
        }

        return null;
    }

    public Text CreateScoreText(string objectName, Text textField, Vector2 position, int fontSize)
    {
        if (textField == null)
        {
            Transform existing = scorePanel.Find(objectName);

            if (existing != null)
                textField = existing.GetComponent<Text>();
        }

        if (textField == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(scorePanel, false);
            textField = textObject.GetComponent<Text>();
        }

        RectTransform rect = textField.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(210, 100);

        textField.font = cinzelFont;
        textField.fontSize = fontSize;
        textField.fontStyle = FontStyle.Bold;
        textField.alignment = TextAnchor.MiddleCenter;
        textField.horizontalOverflow = HorizontalWrapMode.Overflow;
        textField.verticalOverflow = VerticalWrapMode.Overflow;
        textField.color = new Color32(62, 49, 39, 255);
        textField.raycastTarget = false;

        return textField;
    }

    public void UpdateScoreDisplay()
    {
        if (killsText == null)
            BuildScoreDisplay();

        if (killsText != null)
            killsText.text = kills.ToString();

        if (survivalText != null)
            survivalText.text = FormatTime(survivalTime);

        if (levelText != null)
            levelText.text = currentLevel.ToString();

        if (bestKillsText != null)
            bestKillsText.text = PlayerPrefs.GetInt(BestKillsKey, 0).ToString();

        if (bestSurvivalText != null)
            bestSurvivalText.text = FormatTime(PlayerPrefs.GetFloat(BestSurvivalKey, 0));

        if (bestLevelText != null)
            bestLevelText.text = PlayerPrefs.GetInt(BestLevelKey, 1).ToString();
    }

    public string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }
}
