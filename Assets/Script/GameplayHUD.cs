using UnityEngine;
using UnityEngine.SceneManagement;
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

    public Font cinzelFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterSceneConnection()
    {
        SceneManager.sceneLoaded -= AddGameplayHUD;
        SceneManager.sceneLoaded += AddGameplayHUD;
    }

    public static void AddGameplayHUD(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != UIManager.GameplayScene)
        {
            return;
        }

        ScoreManager manager = Object.FindAnyObjectByType<ScoreManager>();

        if (manager != null && manager.GetComponent<GameplayHUD>() == null)
        {
            manager.gameObject.AddComponent<GameplayHUD>();
        }
    }

    public void Start()
    {
        FindConnections();
        BuildHUD();
        UpdateHUD();
    }

    public void Update()
    {
        if (
            scoreManager == null ||
            playerStats == null ||
            scoreBar == null ||
            infoBar == null ||
            healthLine == null ||
            expLine == null
        )
        {
            FindConnections();
            BuildHUD();
        }

        UpdateHUD();
    }

    public void FindConnections()
    {
        if (scoreManager == null)
        {
            scoreManager = Object.FindAnyObjectByType<ScoreManager>();
        }

        if (playerStats == null)
        {
            playerStats = Object.FindAnyObjectByType<PlayerStats>();
        }

        if (scoreBar == null)
        {
            scoreBar = FindRectTransform("scoreBar");
        }

        if (infoBar == null)
        {
            infoBar = FindRectTransform("infoBar");
        }

        if (healthLine == null)
        {
            healthLine = FindImage("healthLine");
        }

        if (expLine == null)
        {
            expLine = FindImage("expLine");
        }

        if (cinzelFont == null)
        {
            cinzelFont = Resources.Load<Font>("Fonts/Cinzel-Bold");
        }
    }

    public RectTransform FindRectTransform(string objectName)
    {
        RectTransform[] rectTransforms =
            Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (
                rectTransform.name == objectName &&
                rectTransform.gameObject.scene.IsValid()
            )
            {
                return rectTransform;
            }
        }

        return null;
    }

    public Image FindImage(string objectName)
    {
        RectTransform rectTransform = FindRectTransform(objectName);

        if (rectTransform == null)
        {
            return null;
        }

        return rectTransform.GetComponent<Image>();
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
            if (liveKillsText != null)
            {
                liveKillsText.text = scoreManager.kills.ToString("D3");
            }

            if (liveTimeText != null)
            {
                liveTimeText.text =
                    scoreManager.FormatTime(scoreManager.survivalTime);
            }
        }

        if (playerStats == null)
        {
            return;
        }

        float healthPercent =
            playerStats.currentHealth /
            Mathf.Max(1f, playerStats.maxHealth);

        float expPercent =
            playerStats.currentExp /
            Mathf.Max(1f, playerStats.expToLevel);

        if (healthLine != null)
        {
            healthLine.fillAmount = Mathf.Clamp01(healthPercent);
        }

        if (expLine != null)
        {
            expLine.fillAmount = Mathf.Clamp01(expPercent);
        }

        if (healthText != null)
        {
            int currentHealth =
                Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentHealth));

            int maximumHealth =
                Mathf.CeilToInt(Mathf.Max(1f, playerStats.maxHealth));

            healthText.text = currentHealth + " / " + maximumHealth;
        }
    }
}
