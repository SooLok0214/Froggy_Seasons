using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoadingScreenController : MonoBehaviour
{
    [Header("Loading Background")]
    [Tooltip("Image used by the loading page background.")]
    public Image backgroundImage;
    [Tooltip("Main menu homeBackground. Its Sprite is copied to the loading background.")]
    public Image homeBackgroundSource;
    public bool useHomeBackground = true;

    [Header("Background Layout")]
    [Tooltip("Position offset of the loading background inside SafeArea.")]
    public Vector2 backgroundPosition = Vector2.zero;
    [Min(0.1f), Tooltip("Overall background size. HomeBackground uses 1.01.")]
    public float backgroundScale = 1.01f;
    [Min(0.1f), Tooltip("Width divided by height. HomeBackground uses 1.7716264.")]
    public float backgroundAspectRatio = 1.7716264f;
    [Tooltip("Envelope Parent fills the screen and crops excess edges, matching homeBackground.")]
    public AspectRatioFitter.AspectMode backgroundAspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

    [Header("Background Gradient")]
    public bool enableBackgroundGradient = true;
    [Tooltip("Colour and opacity at the bottom of the loading background.")]
    public Color gradientBottomColor = new Color(0f, 0f, 0f, 0.55f);
    [Tooltip("Colour and opacity at the top of the loading background.")]
    public Color gradientTopColor = new Color(1f, 1f, 1f, 0.18f);
    [Range(0f, 1f), Tooltip("Overall opacity of the gradient overlay.")]
    public float gradientStrength = 1f;
    [Range(0.05f, 0.95f), Tooltip("Vertical position where the two gradient colours are evenly mixed.")]
    public float gradientMidpoint = 0.5f;
    [Range(8, 256), Tooltip("Higher values make the gradient smoother.")]
    public int gradientResolution = 64;

    [Header("Loading References")]
    [Tooltip("Whole loading graphic. Position and scale settings below control this RectTransform.")]
    public RectTransform loadingGraphic;
    [Tooltip("The roll plus four season icons. This transform rotates around Mid Fix.")]
    public RectTransform rotatingOrbit;
    [Tooltip("Season icons orbit with Roll while remaining upright.")]
    public RectTransform[] uprightSeasonIcons;

    [Header("Loading Timing")]
    [Min(0f), Tooltip("Minimum time the loading page remains visible, using unscaled time.")]
    public float minimumDisplayTime = 0.8f;

    [Header("Rotation")]
    [Min(0f)] public float rotationSpeed = 90f;
    public bool clockwise = true;

    [Header("SafeArea Layout")]
    [Tooltip("Keep the background exactly stretched to all four edges of Canvas/SafeArea.")]
    public bool stretchBackgroundToSafeArea = true;
    [Tooltip("Position of the complete loading graphic from the bottom-right SafeArea corner.")]
    public Vector2 loadingGraphicPosition = new Vector2(-125f, 110f);
    [Tooltip("Reference width and height of the complete loading graphic.")]
    public Vector2 loadingGraphicSize = new Vector2(190f, 190f);
    [Range(0.1f, 4f), Tooltip("Overall size multiplier for Roll, Mid Fix, and all four season icons.")]
    public float loadingGraphicScale = 1f;

    public bool IsLoading { get; private set; }

    private Quaternion[] iconBaseRotations;
    private RawImage gradientOverlay;
    private Texture2D gradientTexture;
#if UNITY_EDITOR
    [System.NonSerialized] private bool editorValidationQueued;
#endif

    private void Awake()
    {
        SyncBackgroundSprite();
        EnsureGradientOverlay();
        ApplyGradientSettings();
        CacheIconRotations();
        ApplyInspectorLayout();
    }

    private void OnEnable()
    {
        SyncBackgroundSprite();
        EnsureGradientOverlay();
        ApplyGradientSettings();
        CacheIconRotations();
    }

    private void OnValidate()
    {
        minimumDisplayTime = Mathf.Max(0f, minimumDisplayTime);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        loadingGraphicSize.x = Mathf.Max(1f, loadingGraphicSize.x);
        loadingGraphicSize.y = Mathf.Max(1f, loadingGraphicSize.y);
        loadingGraphicScale = Mathf.Max(0.1f, loadingGraphicScale);
        gradientStrength = Mathf.Clamp01(gradientStrength);
        gradientMidpoint = Mathf.Clamp(gradientMidpoint, 0.05f, 0.95f);
        gradientResolution = Mathf.Clamp(gradientResolution, 8, 256);
        backgroundScale = Mathf.Max(0.1f, backgroundScale);
        backgroundAspectRatio = Mathf.Max(0.1f, backgroundAspectRatio);
#if UNITY_EDITOR
        QueueEditorValidationRefresh();
#endif
    }

#if UNITY_EDITOR
    private void QueueEditorValidationRefresh()
    {
        if (editorValidationQueued)
            return;

        editorValidationQueued = true;
        UnityEditor.EditorApplication.delayCall += ApplyDeferredEditorValidation;
    }

    private void ApplyDeferredEditorValidation()
    {
        editorValidationQueued = false;
        if (this == null)
            return;

        // RectTransform and AspectRatioFitter must not be changed while Unity is
        // running OnValidate/CheckConsistency. Apply the same Inspector preview
        // one editor update later, when layout callbacks are safe.
        SyncBackgroundSprite();
        ApplyGradientSettings();
        ApplyInspectorLayout();
    }
#endif

    private void OnDestroy()
    {
        if (gradientTexture != null)
            Destroy(gradientTexture);
    }

    private void Update()
    {
        if (rotatingOrbit == null)
            return;

        float direction = clockwise ? -1f : 1f;
        rotatingOrbit.Rotate(0f, 0f, direction * rotationSpeed * Time.unscaledDeltaTime);
        KeepSeasonIconsUpright();
    }

    public void LoadScene(string sceneName)
    {
        if (IsLoading || string.IsNullOrWhiteSpace(sceneName))
            return;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void ApplyInspectorLayout()
    {
        if (transform is RectTransform panelRect && stretchBackgroundToSafeArea)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        if (loadingGraphic != null)
        {
            loadingGraphic.anchorMin = new Vector2(1f, 0f);
            loadingGraphic.anchorMax = new Vector2(1f, 0f);
            loadingGraphic.pivot = new Vector2(0.5f, 0.5f);
            loadingGraphic.anchoredPosition = loadingGraphicPosition;
            loadingGraphic.sizeDelta = loadingGraphicSize;
            loadingGraphic.localScale = Vector3.one * loadingGraphicScale;
        }

        ApplyBackgroundLayout();
    }

    private void ApplyBackgroundLayout()
    {
        if (backgroundImage == null)
            return;

        RectTransform backgroundRect = backgroundImage.rectTransform;
        if (backgroundRect == transform)
            return;

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.zero;
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = backgroundPosition;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.localScale = Vector3.one * Mathf.Max(0.1f, backgroundScale);

        AspectRatioFitter fitter = backgroundImage.GetComponent<AspectRatioFitter>();
        if (fitter == null && Application.isPlaying)
            fitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();

        if (fitter != null)
        {
            fitter.aspectMode = backgroundAspectMode;
            fitter.aspectRatio = Mathf.Max(0.1f, backgroundAspectRatio);
        }
    }

    private void SyncBackgroundSprite()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (!useHomeBackground || backgroundImage == null)
            return;

        if (homeBackgroundSource == null)
        {
            Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (Image image in images)
            {
                if (image != null && image != backgroundImage &&
                    image.gameObject.scene == gameObject.scene &&
                    string.Equals(image.name, "homeBackground", System.StringComparison.OrdinalIgnoreCase))
                {
                    homeBackgroundSource = image;
                    break;
                }
            }
        }

        if (homeBackgroundSource == null || homeBackgroundSource.sprite == null)
            return;

        backgroundImage.sprite = homeBackgroundSource.sprite;
        backgroundImage.type = homeBackgroundSource.type;
        backgroundImage.preserveAspect = homeBackgroundSource.preserveAspect;
        backgroundImage.color = homeBackgroundSource.color;
    }

    private void EnsureGradientOverlay()
    {
        if (gradientOverlay != null)
            return;

        Transform gradientParent = backgroundImage != null ? backgroundImage.transform : transform;
        Transform existing = gradientParent.Find("LoadingBackgroundGradient");
        if (existing == null && gradientParent != transform)
        {
            existing = transform.Find("LoadingBackgroundGradient");
            if (existing != null)
                existing.SetParent(gradientParent, false);
        }

        if (existing != null)
            gradientOverlay = existing.GetComponent<RawImage>();

        if (gradientOverlay == null && Application.isPlaying)
        {
            GameObject overlayObject = new GameObject(
                "LoadingBackgroundGradient",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage)
            );
            overlayObject.layer = gameObject.layer;
            overlayObject.transform.SetParent(gradientParent, false);

            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            if (gradientParent == transform)
                rect.SetAsFirstSibling();
            else
                rect.SetAsLastSibling();

            gradientOverlay = overlayObject.GetComponent<RawImage>();
            gradientOverlay.raycastTarget = false;
            gradientOverlay.color = Color.white;
        }
    }

    private void ApplyGradientSettings()
    {
        if (gradientOverlay == null)
            return;

        gradientOverlay.enabled = enableBackgroundGradient;
        if (!enableBackgroundGradient)
            return;

        int height = Mathf.Clamp(gradientResolution, 8, 256);
        if (gradientTexture == null || gradientTexture.height != height)
        {
            if (gradientTexture != null)
                Destroy(gradientTexture);

            gradientTexture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime Loading Background Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
        }

        Color[] pixels = new Color[height];
        float midpoint = Mathf.Clamp(gradientMidpoint, 0.05f, 0.95f);
        float strength = Mathf.Clamp01(gradientStrength);
        for (int y = 0; y < height; y++)
        {
            float vertical = y / (height - 1f);
            float blend = vertical <= midpoint
                ? 0.5f * vertical / midpoint
                : 0.5f + 0.5f * (vertical - midpoint) / (1f - midpoint);
            Color pixel = Color.Lerp(gradientBottomColor, gradientTopColor, blend);
            pixel.a *= strength;
            pixels[y] = pixel;
        }

        gradientTexture.SetPixels(pixels);
        gradientTexture.Apply(false, false);
        gradientOverlay.texture = gradientTexture;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;
        float shownAt = Time.realtimeSinceStartup;

        // Let Canvas render this page before Unity starts scene loading.
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            IsLoading = false;
            gameObject.SetActive(false);
            yield break;
        }

        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f)
            yield return null;

        while (Time.realtimeSinceStartup - shownAt < minimumDisplayTime)
            yield return null;

        operation.allowSceneActivation = true;
    }

    private void CacheIconRotations()
    {
        if (uprightSeasonIcons == null)
        {
            iconBaseRotations = null;
            return;
        }

        iconBaseRotations = new Quaternion[uprightSeasonIcons.Length];
        for (int i = 0; i < uprightSeasonIcons.Length; i++)
            iconBaseRotations[i] = uprightSeasonIcons[i] != null
                ? uprightSeasonIcons[i].localRotation
                : Quaternion.identity;
    }

    private void KeepSeasonIconsUpright()
    {
        if (uprightSeasonIcons == null || iconBaseRotations == null ||
            uprightSeasonIcons.Length != iconBaseRotations.Length)
        {
            CacheIconRotations();
        }

        if (uprightSeasonIcons == null || iconBaseRotations == null)
            return;

        Quaternion counterRotation = Quaternion.Inverse(rotatingOrbit.localRotation);
        for (int i = 0; i < uprightSeasonIcons.Length; i++)
        {
            if (uprightSeasonIcons[i] != null)
                uprightSeasonIcons[i].localRotation = counterRotation * iconBaseRotations[i];
        }
    }
}
