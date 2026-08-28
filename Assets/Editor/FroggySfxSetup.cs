using UnityEditor;
using UnityEngine;

public static class FroggySfxSetup
{
    [MenuItem("Tools/Froggy Seasons/Setup SFX")]
    public static void SetupSfx()
    {
        MusicManagerAudioSetup.SetupAllScenes();
    }

    public static void SetupHomeLogoButton()
    {
        GameObject logo = GameObject.Find("homeLogo");

        if (logo == null)
            return;

        Transform existing = logo.transform.parent.Find("homeLogoButton");
        GameObject buttonObject = existing == null ? new GameObject("homeLogoButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)) : existing.gameObject;
        RectTransform logoRect = logo.GetComponent<RectTransform>();
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(logo.transform.parent, false);
        rect.anchorMin = logoRect.anchorMin;
        rect.anchorMax = logoRect.anchorMax;
        rect.anchoredPosition = logoRect.anchoredPosition;
        rect.sizeDelta = logoRect.sizeDelta;
        rect.pivot = logoRect.pivot;
        rect.localScale = logoRect.localScale;
        rect.localRotation = logoRect.localRotation;
        buttonObject.transform.SetSiblingIndex(logo.transform.GetSiblingIndex() + 1);

        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        EditorUtility.SetDirty(buttonObject);
    }}
