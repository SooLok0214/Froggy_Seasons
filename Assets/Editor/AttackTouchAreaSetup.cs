using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AttackTouchAreaSetup
{
    public const string AreaName = "attackTouchArea";

    static AttackTouchAreaSetup()
    {
        EditorApplication.delayCall += SetupOpenScene;
    }

    [MenuItem("Tools/Froggy Seasons/Setup Attack Touch Area")]
    public static void SetupOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "InGameScene")
            return;

        PlayerAttack playerAttack =
            Object.FindFirstObjectByType<PlayerAttack>(
                FindObjectsInactive.Include
            );

        if (playerAttack == null || playerAttack.attackButton == null)
            return;

        Transform safeArea = playerAttack.attackButton.transform.parent;
        Transform existing = safeArea.Find(AreaName);
        GameObject areaObject;
        bool created = existing == null;

        if (created)
        {
            areaObject = new GameObject(
                AreaName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)
            );
            Undo.RegisterCreatedObjectUndo(
                areaObject,
                "Create attack touch area"
            );
            areaObject.transform.SetParent(safeArea, false);
        }
        else
        {
            areaObject = existing.gameObject;
        }

        RectTransform rect = areaObject.GetComponent<RectTransform>();
        Image image = areaObject.GetComponent<Image>();
        Button touchButton = areaObject.GetComponent<Button>();

        if (created)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        image.color = new Color(1f, 0.55f, 0f, 0f);
        image.raycastTarget = true;

        Button iconButton = playerAttack.attackButton;
        touchButton.targetGraphic = iconButton.targetGraphic;
        touchButton.transition = iconButton.transition;
        touchButton.colors = iconButton.colors;
        touchButton.spriteState = iconButton.spriteState;
        touchButton.animationTriggers = iconButton.animationTriggers;
        touchButton.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };

        areaObject.transform.SetSiblingIndex(
            iconButton.transform.GetSiblingIndex()
        );

        playerAttack.attackTouchAreaButton = touchButton;
        EditorUtility.SetDirty(playerAttack);
        EditorUtility.SetDirty(areaObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "[Attack Touch Area] Canvas range is ready. " +
            "Select Canvas/SafeArea/attackTouchArea and edit its " +
            "Rect Transform."
        );
    }
}
