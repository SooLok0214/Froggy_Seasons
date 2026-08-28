using UnityEngine;
using UnityEngine.UI;

public class LevelUpChoiceSystem : MonoBehaviour
{
    public enum SkillType
    {
        IncreaseAttack,
        IncreaseMaxHealth,
        RestoreHealth
    }

    public Sprite addDamageSprite;
    public Sprite addHealthSprite;
    public Sprite restoreHealthSprite;

    public Vector2 cardSize = new Vector2(357f, 600f);
    public float cardGap = 90f;
    public Color overlayColor = new Color(0f, 0f, 0f, 0.82f);
    public float overlayOverscan = 300f;

    public GameObject overlay;
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public Canvas rootCanvas;

    public PlayerStats currentPlayer;
    public UIManager uiManager;
    public bool selectionOpen;

    public void Awake()
    {
        BuildUI();
        SetOverlayActive(false);
    }

    public void ShowChoices(PlayerStats player)
    {
        if (selectionOpen || player == null || player.isDead)
        {
            return;
        }

        if (GameManager.instance != null &&
            !GameManager.instance.gameStarted)
        {
            return;
        }

        currentPlayer = player;
        BuildUI();

        int firstSkill = Random.Range(0, 3);
        int secondSkill = Random.Range(0, 2);

        if (secondSkill >= firstSkill)
        {
            secondSkill++;
        }

        SetupChoiceButton(
            leftChoiceButton,
            (SkillType)firstSkill
        );

        SetupChoiceButton(
            rightChoiceButton,
            (SkillType)secondSkill
        );

        selectionOpen = true;
        SetOverlayActive(true);
        overlay.transform.SetAsLastSibling();

        Time.timeScale = 0f;

        if (uiManager != null)
        {
            uiManager.SetCameraGameplayControl(false);
        }

    }

    public void SetupChoiceButton(
        Button button,
        SkillType skill
    )
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.sprite = GetSkillSprite(skill);
            image.preserveAspect = true;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(
            () => SelectSkill(skill)
        );
    }

    public Sprite GetSkillSprite(SkillType skill)
    {
        if (skill == SkillType.IncreaseAttack)
        {
            return addDamageSprite;
        }

        if (skill == SkillType.IncreaseMaxHealth)
        {
            return addHealthSprite;
        }

        return restoreHealthSprite;
    }

    public void SelectSkill(SkillType skill)
    {
        if (!selectionOpen || currentPlayer == null)
        {
            return;
        }

        if (MusicManager.instance != null)
            MusicManager.instance.PlayButtonClick();

        if (skill == SkillType.IncreaseAttack)
        {
            currentPlayer.IncreaseAttack(10f);
        }
        else if (skill == SkillType.IncreaseMaxHealth)
        {
            currentPlayer.IncreaseMaxHealth(50f);
        }
        else
        {
            currentPlayer.Heal(30f);
        }

        CloseChoices();
    }

    public void CloseChoices()
    {
        selectionOpen = false;
        currentPlayer = null;
        SetOverlayActive(false);

        if (uiManager != null &&
            GameManager.instance != null &&
            GameManager.instance.gameStarted)
        {
            Time.timeScale = 1f;
            uiManager.SetCameraGameplayControl(true);
        }
    }

    public void CancelChoices()
    {
        selectionOpen = false;
        currentPlayer = null;
        SetOverlayActive(false);
    }

    public void BuildUI()
    {
        Canvas canvas = rootCanvas;

        if (canvas == null)
        {
            return;
        }

        if (overlay == null)
        {
            Transform existing =
                canvas.transform.Find("LevelUpChoiceOverlay");

            if (existing != null)
            {
                overlay = existing.gameObject;
            }
        }

        if (overlay == null)
        {
            overlay = new GameObject(
                "LevelUpChoiceOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup)
            );

            overlay.transform.SetParent(
                canvas.transform,
                false
            );
        }

        RectTransform overlayRect =
            overlay.GetComponent<RectTransform>();

        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.offsetMin =
            new Vector2(-overlayOverscan, -overlayOverscan);
        overlayRect.offsetMax =
            new Vector2(overlayOverscan, overlayOverscan);

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;

        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.interactable = true;
        group.blocksRaycasts = true;

        leftChoiceButton = BuildChoiceButton(
            "LevelUpChoiceLeft",
            new Vector2(
                -(cardSize.x + cardGap) * 0.5f,
                0f
            )
        );

        rightChoiceButton = BuildChoiceButton(
            "LevelUpChoiceRight",
            new Vector2(
                (cardSize.x + cardGap) * 0.5f,
                0f
            )
        );
    }

    public Button BuildChoiceButton(
        string objectName,
        Vector2 position
    )
    {
        Transform existing = overlay.transform.Find(objectName);
        GameObject buttonObject;

        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );

            buttonObject.transform.SetParent(
                overlay.transform,
                false
            );
        }

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = cardSize;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        return button;
    }

    public void SetOverlayActive(bool active)
    {
        if (overlay != null)
        {
            overlay.SetActive(active);
        }
    }
}
