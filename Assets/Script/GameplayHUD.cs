using UnityEngine;
using UnityEngine.UI;

public class GameplayHUD : MonoBehaviour
{
    public ScoreManager scoreManager;
    public PlayerStats playerStats;

    public RectTransform scoreBar;
    public RectTransform infoBar;

    public Image healthLine;
    public Image expLine;

    public Text liveKillsText;
    public Text liveTimeText;
    public Text healthText;
    public Text levelText;

    public Font cinzelFont;

    [System.NonSerialized] public int displayedKills = -1;
    [System.NonSerialized] public int displayedTime = -1;
    [System.NonSerialized] public float displayedHealth = -1f;
    [System.NonSerialized] public float displayedMaxHealth = -1f;
    [System.NonSerialized] public float displayedExp = -1f;
    [System.NonSerialized] public float displayedExpTarget = -1f;
    [System.NonSerialized] public int displayedLevel = -1;

    public void Start()
    {
        BuildHUD();
        UpdateHUD();
    }

    public void Update()
    {
        UpdateHUD();
    }

    public void BuildHUD()
    {
        Color scoreColor = new Color32(62, 49, 39, 255);

        if (scoreBar != null)
        {
            liveKillsText = CreateText(
                scoreBar,
                "LiveKillValue",
                liveKillsText,
                new Vector2(-160f, -18f),
                new Vector2(300f, 90f),
                44,
                scoreColor
            );

            liveTimeText = CreateText(
                scoreBar,
                "LiveTimeValue",
                liveTimeText,
                new Vector2(150f, -18f),
                new Vector2(300f, 90f),
                44,
                scoreColor
            );
        }

        if (infoBar != null)
        {
            healthText = CreateText(
                infoBar,
                "HealthValue",
                healthText,
                new Vector2(1020f, -20f),
                new Vector2(1000f, 300f),
                60,
                new Color32(255, 250, 226, 255)
            );

            levelText = CreateText(
                infoBar,
                "LevelValue",
                levelText,
                new Vector2(-1195f, -185f),
                new Vector2(260f, 150f),
                92,
                scoreColor
            );
        }

        PrepareBar(healthLine);
        PrepareBar(expLine);
    }

    public void PrepareBar(Image bar)
    {
        if (bar == null)
        {
            return;
        }

        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;
        bar.fillClockwise = true;
        bar.preserveAspect = true;
        bar.raycastTarget = false;
    }

    public Text CreateText(
        RectTransform parent,
        string objectName,
        Text currentText,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        Color color
    )
    {
        if (currentText == null)
        {
            Transform existing = parent.Find(objectName);

            if (existing != null)
            {
                currentText = existing.GetComponent<Text>();
            }
        }

        if (currentText == null)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

            textObject.transform.SetParent(parent, false);
            currentText = textObject.GetComponent<Text>();
        }

        RectTransform rectTransform = currentText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;

        currentText.font =
            cinzelFont != null
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

            int currentSecond = Mathf.FloorToInt(scoreManager.survivalTime);

            if (liveTimeText != null && displayedTime != currentSecond)
            {
                displayedTime = currentSecond;
                liveTimeText.text =
                    scoreManager.FormatTime(scoreManager.survivalTime);
            }
        }

        if (playerStats == null)
        {
            return;
        }

        bool healthChanged =
            !Mathf.Approximately(displayedHealth, playerStats.currentHealth) ||
            !Mathf.Approximately(displayedMaxHealth, playerStats.maxHealth);

        bool expChanged =
            !Mathf.Approximately(displayedExp, playerStats.currentExp) ||
            !Mathf.Approximately(displayedExpTarget, playerStats.expToLevel);

        if (healthChanged)
        {
            displayedHealth = playerStats.currentHealth;
            displayedMaxHealth = playerStats.maxHealth;

            if (healthLine != null)
            {
                healthLine.fillAmount = Mathf.Clamp01(
                    playerStats.currentHealth / Mathf.Max(1f, playerStats.maxHealth)
                );
            }

            if (healthText != null)
            {
                int currentHealth = Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentHealth));
                int maximumHealth = Mathf.CeilToInt(Mathf.Max(1f, playerStats.maxHealth));
                healthText.text = currentHealth + " / " + maximumHealth;
            }
        }

        if (expChanged)
        {
            displayedExp = playerStats.currentExp;
            displayedExpTarget = playerStats.expToLevel;

            if (expLine != null)
            {
                expLine.fillAmount = Mathf.Clamp01(
                    playerStats.currentExp / Mathf.Max(1f, playerStats.expToLevel)
                );
            }
        }

        if (levelText != null && displayedLevel != playerStats.currentLevel)
        {
            displayedLevel = playerStats.currentLevel;
            levelText.text = playerStats.currentLevel.ToString();
        }
    }
}
