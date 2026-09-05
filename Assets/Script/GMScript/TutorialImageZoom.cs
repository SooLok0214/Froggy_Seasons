using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Added to the tutorial Image by UIManager. No scene wiring or image resizing
// is needed: the larger view is a separate runtime-only canvas child.
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class TutorialImageZoom : MonoBehaviour, IPointerClickHandler
{
    [Range(0.5f, 1f)] public float screenFill = 0.94f;

    private Image source;
    private GameObject overlay;
    private Image enlargedImage;
    private Canvas overlayParent;

    public bool IsZoomed => overlay != null && overlay.activeSelf;

    public void Initialize()
    {
        source = GetComponent<Image>();
        source.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Touch taps are delivered as left-button pointer clicks too.
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        Toggle();
    }

    public void Toggle()
    {
        if (IsZoomed) { Close(); return; }
        Initialize();
        if (!isActiveAndEnabled || !source.enabled || source.sprite == null)
            return;

        Canvas canvas = source.canvas;
        if (canvas == null || canvas.rootCanvas == null) return;
        Canvas root = canvas.rootCanvas;
        if (overlay == null) CreateOverlay(root);
        if (overlayParent != root)
        {
            overlay.transform.SetParent(root.transform, false);
            overlayParent = root;
        }

        float inset = (1f - Mathf.Clamp(screenFill, 0.5f, 1f)) * 0.5f;
        RectTransform rect = enlargedImage.rectTransform;
        rect.anchorMin = Vector2.one * inset;
        rect.anchorMax = Vector2.one * (1f - inset);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        enlargedImage.sprite = source.sprite;
        enlargedImage.color = source.color;
        overlay.transform.SetAsLastSibling();
        overlay.SetActive(true);
    }

    private void CreateOverlay(Canvas root)
    {
        overlay = new GameObject("TutorialImageZoomOverlay", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        overlay.SetActive(false);
        overlay.layer = root.gameObject.layer;
        overlay.transform.SetParent(root.transform, false);
        overlayParent = root;
        RectTransform backdropRect = (RectTransform)overlay.transform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.88f);
        backdrop.raycastTarget = true;

        Button close = overlay.GetComponent<Button>();
        close.targetGraphic = backdrop;
        close.transition = Selectable.Transition.None;
        close.navigation = new Navigation { mode = Navigation.Mode.None };
        close.onClick.AddListener(Close);

        var imageObject = new GameObject("EnlargedTutorialImage", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = overlay.layer;
        imageObject.transform.SetParent(overlay.transform, false);
        enlargedImage = imageObject.GetComponent<Image>();
        enlargedImage.type = Image.Type.Simple;
        enlargedImage.preserveAspect = true;
        // The backdrop handles taps over both the image and the dark margins,
        // blocking clicks from reaching the page buttons underneath.
        enlargedImage.raycastTarget = false;
    }

    public void Close()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    private void LateUpdate()
    {
        if (IsZoomed && (source == null || !source.enabled || source.sprite != enlargedImage.sprite))
            Close();
    }

    private void OnDisable() { Close(); }

    private void OnDestroy()
    {
        if (overlay == null) return;
        overlay.SetActive(false);
        if (Application.isPlaying) Destroy(overlay);
        else DestroyImmediate(overlay);
    }
}
