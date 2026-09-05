using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class BootLogoSplash : MonoBehaviour
{
    [Header("Company Logo Sprites")]
    public Sprite companyIconSprite;
    public Sprite companyName1Sprite;
    public Sprite companyName2Sprite;

    [Header("Company Logo Layout")]
    public Vector2 companyIconSize = new Vector2(520f, 566f);
    public Vector2 companyIconPosition = new Vector2(-535f, 0f);
    public Vector2 companyName1Size = new Vector2(1050f, 140f);
    public Vector2 companyName1Position = new Vector2(295f, 55f);
    public Vector2 companyName2Size = new Vector2(1050f, 70f);
    public Vector2 companyName2Position = new Vector2(295f, -75f);

    [Header("Company Logo Timing (Seconds)")]
    [Min(0f)] public float companyIconPopDuration = 0.55f;
    [Min(0f)] public float companyName1RevealDuration = 0.75f;
    [Min(0f)] public float companyName2RevealDuration = 0.6f;
    [Min(0f)] public float companyHoldDuration = 2f;
    [Min(0f)] public float companyFadeOutDuration = 0.4f;

    [Header("Company Logo Pop")]
    [Range(0.25f, 1f)] public float companyIconStartScale = 0.68f;
    [Range(1f, 1.5f)] public float companyIconOvershootScale = 1.13f;

    [Header("Company Logo Background")]
    public Color companyBackgroundColor = Color.white;

    [Header("Game Logo")]
    public Sprite logoSprite;
    public Vector2 logoSize = new Vector2(1380f, 636f);
    public Color logoColor = Color.white;

    [Header("Game Logo Sound (Bypasses SFX Mixer)")]
    public AudioClip gameLogoSound;
    [Range(0f, 1f)] public float gameLogoSoundVolume = 1f;

    [Header("Background")]
    public Color backgroundColor = new Color(0.025f, 0.035f, 0.03f, 1f);

    [Header("Timing (Seconds)")]
    [Min(0f)] public float fadeInDuration = 0.5f;
    [Min(0f)] public float holdDuration = 1f;
    [Min(0f)] public float fadeOutDuration = 0.5f;

    [Header("Animation")]
    [Range(0.5f, 1f)] public float startLogoScale = 0.72f;
    [Range(1f, 1.5f)] public float popOvershootScale = 1.12f;

    private static bool hasShownThisLaunch;
    public static bool IsShowing { get; private set; }

    private GameObject overlayObject;
    private CanvasGroup overlayGroup;
    private Image backgroundImage;
    private GameObject companyLogoObject;
    private CanvasGroup companyLogoGroup;
    private Image companyIconImage;
    private RectTransform companyIconRect;
    private RectTransform companyName1MaskRect;
    private RectTransform companyName2MaskRect;
    private Image logoImage;
    private RectTransform logoRect;
    private AudioSource gameLogoAudioSource;
    private EventSystem disabledEventSystem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLaunchState()
    {
        hasShownThisLaunch = false;
        IsShowing = false;
    }

    public void Awake()
    {
        if (hasShownThisLaunch)
        {
            enabled = false;
            return;
        }

        hasShownThisLaunch = true;
        IsShowing = true;
        BuildOverlay();
        DisableMenuInput();
    }

    public void Start()
    {
        if (overlayObject != null)
            StartCoroutine(PlaySplash());
    }

    public void BuildOverlay()
    {
        overlayObject = new GameObject(
            "BootLogoSplashOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup)
        );

        Canvas canvas = overlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;

        CanvasScaler scaler = overlayObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        overlayGroup = overlayObject.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 1f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        gameLogoAudioSource = overlayObject.AddComponent<AudioSource>();
        gameLogoAudioSource.clip = gameLogoSound;
        gameLogoAudioSource.playOnAwake = false;
        gameLogoAudioSource.loop = false;
        gameLogoAudioSource.spatialBlend = 0f;
        gameLogoAudioSource.volume = gameLogoSoundVolume;
        gameLogoAudioSource.outputAudioMixerGroup = null;
        gameLogoAudioSource.ignoreListenerPause = true;

        GameObject backgroundObject = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        backgroundObject.transform.SetParent(overlayObject.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = HasCompanyLogo()
            ? companyBackgroundColor
            : backgroundColor;
        backgroundImage.raycastTarget = true;

        BuildCompanyLogo();

        GameObject logoObject = new GameObject(
            "GameLogo",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        logoObject.transform.SetParent(overlayObject.transform, false);

        logoRect = logoObject.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.sizeDelta = logoSize;
        logoRect.localScale = Vector3.one * startLogoScale;

        logoImage = logoObject.GetComponent<Image>();
        logoImage.sprite = logoSprite;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        logoImage.color = new Color(logoColor.r, logoColor.g, logoColor.b, 0f);
    }

    private void BuildCompanyLogo()
    {
        companyLogoObject = new GameObject(
            "CompanyLogo",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );
        companyLogoObject.transform.SetParent(overlayObject.transform, false);

        RectTransform companyRect = companyLogoObject.GetComponent<RectTransform>();
        companyRect.anchorMin = new Vector2(0.5f, 0.5f);
        companyRect.anchorMax = new Vector2(0.5f, 0.5f);
        companyRect.pivot = new Vector2(0.5f, 0.5f);
        companyRect.anchoredPosition = Vector2.zero;
        companyRect.sizeDelta = new Vector2(1680f, 620f);

        companyLogoGroup = companyLogoObject.GetComponent<CanvasGroup>();
        companyLogoGroup.alpha = 1f;
        companyLogoGroup.interactable = false;
        companyLogoGroup.blocksRaycasts = false;

        GameObject iconObject = new GameObject(
            "CompanyIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        iconObject.transform.SetParent(companyLogoObject.transform, false);

        companyIconRect = iconObject.GetComponent<RectTransform>();
        companyIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        companyIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        companyIconRect.pivot = new Vector2(0.5f, 0.5f);
        companyIconRect.anchoredPosition = companyIconPosition;
        companyIconRect.sizeDelta = companyIconSize;
        companyIconRect.localScale = Vector3.one * companyIconStartScale;

        companyIconImage = iconObject.GetComponent<Image>();
        companyIconImage.sprite = companyIconSprite;
        companyIconImage.preserveAspect = true;
        companyIconImage.raycastTarget = false;
        companyIconImage.color = new Color(1f, 1f, 1f, 0f);

        companyName1MaskRect = CreateRevealImage(
            "CompanyName1",
            companyName1Sprite,
            companyName1Size,
            companyName1Position,
            true
        );
        companyName2MaskRect = CreateRevealImage(
            "CompanyName2",
            companyName2Sprite,
            companyName2Size,
            companyName2Position,
            false
        );
    }

    private RectTransform CreateRevealImage(
        string objectName,
        Sprite sprite,
        Vector2 fullSize,
        Vector2 centerPosition,
        bool revealFromLeft
    )
    {
        GameObject maskObject = new GameObject(
            objectName + "Mask",
            typeof(RectTransform),
            typeof(RectMask2D)
        );
        maskObject.transform.SetParent(companyLogoObject.transform, false);

        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = new Vector2(0.5f, 0.5f);
        maskRect.anchorMax = new Vector2(0.5f, 0.5f);
        maskRect.pivot = revealFromLeft
            ? new Vector2(0f, 0.5f)
            : new Vector2(0.5f, 0.5f);
        maskRect.anchoredPosition = revealFromLeft
            ? centerPosition - new Vector2(fullSize.x * 0.5f, 0f)
            : centerPosition;
        maskRect.sizeDelta = new Vector2(0f, fullSize.y);

        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.transform.SetParent(maskObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = revealFromLeft
            ? new Vector2(0f, 0.5f)
            : new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = imageRect.anchorMin;
        imageRect.pivot = imageRect.anchorMin;
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = fullSize;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;

        return maskRect;
    }

    public IEnumerator PlaySplash()
    {
        if (HasCompanyLogo())
        {
            yield return PlayCompanyLogo();
            companyLogoObject.SetActive(false);
        }

        PlayGameLogoSound();
        yield return FadeLogoIn();

        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        yield return FadeOverlayOut();
        FinishSplash();
    }

    private void PlayGameLogoSound()
    {
        if (gameLogoAudioSource == null || gameLogoSound == null)
            return;

        gameLogoAudioSource.volume = gameLogoSoundVolume;
        gameLogoAudioSource.PlayOneShot(gameLogoSound);
    }

    private bool HasCompanyLogo()
    {
        return companyIconSprite != null
            && companyName1Sprite != null
            && companyName2Sprite != null;
    }

    private IEnumerator PlayCompanyLogo()
    {
        yield return AnimateCompanyIcon();
        yield return AnimateReveal(
            companyName1MaskRect,
            companyName1Size,
            companyName1RevealDuration
        );
        yield return AnimateReveal(
            companyName2MaskRect,
            companyName2Size,
            companyName2RevealDuration
        );

        if (companyHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(companyHoldDuration);

        yield return FadeCompanyLogoOut();
    }

    private IEnumerator AnimateCompanyIcon()
    {
        if (companyIconPopDuration <= 0f)
        {
            SetCompanyIconProgress(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < companyIconPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetCompanyIconProgress(
                Mathf.Clamp01(elapsed / companyIconPopDuration)
            );
            yield return null;
        }

        SetCompanyIconProgress(1f);
    }

    private void SetCompanyIconProgress(float progress)
    {
        companyIconImage.color = new Color(
            1f,
            1f,
            1f,
            Mathf.SmoothStep(0f, 1f, progress)
        );
        companyIconRect.localScale = Vector3.one * CalculatePopScale(
            progress,
            companyIconStartScale,
            companyIconOvershootScale
        );
    }

    private IEnumerator AnimateReveal(
        RectTransform maskRect,
        Vector2 fullSize,
        float duration
    )
    {
        if (duration <= 0f)
        {
            maskRect.sizeDelta = fullSize;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );
            maskRect.sizeDelta = new Vector2(
                fullSize.x * progress,
                fullSize.y
            );
            yield return null;
        }

        maskRect.sizeDelta = fullSize;
    }

    private IEnumerator FadeCompanyLogoOut()
    {
        if (companyFadeOutDuration <= 0f)
        {
            companyLogoGroup.alpha = 0f;
            backgroundImage.color = backgroundColor;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < companyFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / companyFadeOutDuration)
            );
            companyLogoGroup.alpha = 1f - progress;
            backgroundImage.color = Color.Lerp(
                companyBackgroundColor,
                backgroundColor,
                progress
            );
            yield return null;
        }

        companyLogoGroup.alpha = 0f;
        backgroundImage.color = backgroundColor;
    }

    public IEnumerator FadeLogoIn()
    {
        if (fadeInDuration <= 0f)
        {
            SetLogoProgress(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetLogoProgress(Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }

        SetLogoProgress(1f);
    }

    public void SetLogoProgress(float progress)
    {
        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        logoImage.color = new Color(
            logoColor.r,
            logoColor.g,
            logoColor.b,
            logoColor.a * smoothProgress
        );

        logoRect.localScale = Vector3.one * CalculatePopScale(
            progress,
            startLogoScale,
            popOvershootScale
        );
    }

    private float CalculatePopScale(
        float progress,
        float initialScale,
        float overshootScale
    )
    {
        const float popPeak = 0.7f;

        if (progress < popPeak)
        {
            float popProgress = Mathf.SmoothStep(0f, 1f, progress / popPeak);
            return Mathf.Lerp(initialScale, overshootScale, popProgress);
        }

        float settleProgress = Mathf.SmoothStep(
            0f,
            1f,
            (progress - popPeak) / (1f - popPeak)
        );
        return Mathf.Lerp(overshootScale, 1f, settleProgress);
    }

    public IEnumerator FadeOverlayOut()
    {
        if (fadeOutDuration <= 0f)
        {
            overlayGroup.alpha = 0f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            overlayGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            yield return null;
        }

        overlayGroup.alpha = 0f;
    }

    public void DisableMenuInput()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>(
            FindObjectsInactive.Include
        );

        if (eventSystem != null && eventSystem.enabled)
        {
            disabledEventSystem = eventSystem;
            disabledEventSystem.enabled = false;
        }
    }

    public void FinishSplash()
    {
        IsShowing = false;
        RestoreMenuInput();

        if (MusicManager.instance != null)
            MusicManager.instance.PlayHomeMusic();

        if (overlayObject != null)
            Destroy(overlayObject);

        enabled = false;
    }

    public void RestoreMenuInput()
    {
        if (disabledEventSystem != null)
        {
            disabledEventSystem.enabled = true;
            disabledEventSystem = null;
        }
    }

    public void OnDisable()
    {
        RestoreMenuInput();
    }
}
