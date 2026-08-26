#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SplitPlayerHUDSetup
{
    public static bool setupQueued;

    static SplitPlayerHUDSetup()
    {
        if (!SessionState.GetBool("SplitPlayerHUDSetupComplete", false))
        {
            EditorApplication.delayCall += Setup;
        }
    }

    public static void Setup()
    {
        if (setupQueued || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        setupQueued = true;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        RectTransform oldInfo =
            FindRectTransform("characterInfo");

        if (canvas == null || oldInfo == null)
        {
            setupQueued = false;
            return;
        }

        RectTransform info =
            CreateImage(
                oldInfo.parent,
                "infoBar",
                "Assets/UI_Metirial/infoBar.png"
            );

        info.anchorMin = oldInfo.anchorMin;
        info.anchorMax = oldInfo.anchorMax;
        info.pivot = oldInfo.pivot;
        info.anchoredPosition = oldInfo.anchoredPosition;
        info.sizeDelta = oldInfo.sizeDelta;
        info.localScale = oldInfo.localScale;
        info.SetSiblingIndex(oldInfo.GetSiblingIndex());

        Image infoImage = info.GetComponent<Image>();
        infoImage.preserveAspect = true;
        infoImage.raycastTarget = false;

        RectTransform health =
            CreateImage(
                info,
                "healthLine",
                "Assets/UI_Metirial/healthLine.png"
            );

        SetAnchors(
            health,
            new Vector2(0.185f, 0.405f),
            new Vector2(0.850f, 0.596f)
        );

        PrepareFilledBar(health.GetComponent<Image>());

        RectTransform exp =
            CreateImage(
                info,
                "expLine",
                "Assets/UI_Metirial/expLine.png"
            );

        SetAnchors(
            exp,
            new Vector2(0.205f, 0.310f),
            new Vector2(0.693f, 0.361f)
        );

        PrepareFilledBar(exp.GetComponent<Image>());

        oldInfo.gameObject.SetActive(false);

        EditorUtility.SetDirty(info.gameObject);
        EditorSceneManager.MarkSceneDirty(info.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();

        SessionState.SetBool(
            "SplitPlayerHUDSetupComplete",
            true
        );
    }

    public static RectTransform FindRectTransform(string objectName)
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

    public static RectTransform CreateImage(
        Transform parent,
        string objectName,
        string assetPath
    )
    {
        RectTransform existing =
            FindRectTransform(objectName);

        if (existing != null)
        {
            Image existingImage =
                existing.GetComponent<Image>();

            if (existingImage == null)
            {
                existingImage =
                    existing.gameObject.AddComponent<Image>();
            }

            existingImage.sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            return existing;
        }

        GameObject imageObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        return imageObject.GetComponent<RectTransform>();
    }

    public static void SetAnchors(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax
    )
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    public static void PrepareFilledBar(Image bar)
    {
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin =
            (int)Image.OriginHorizontal.Left;
        bar.fillClockwise = true;
        bar.fillAmount = 1f;
        bar.preserveAspect = false;
        bar.raycastTarget = false;
    }
}
#endif
