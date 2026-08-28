#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelUpChoiceSetup
{
    public const string GameplayScenePath =
        "Assets/Scenes/InGameScene.unity";

    [MenuItem("Tools/Froggy Seasons/Setup Level Up Choices")]
    public static void SetupLevelUpChoices()
    {
        string originalScenePath =
            SceneManager.GetActiveScene().path;

        EditorSceneManager.SaveOpenScenes();

        Scene gameplayScene = EditorSceneManager.OpenScene(
            GameplayScenePath,
            OpenSceneMode.Single
        );

        Canvas canvas = Object.FindAnyObjectByType<Canvas>(
            FindObjectsInactive.Include
        );

        if (canvas == null)
        {
            Debug.LogError(
                "[Level Up] InGameScene 找不到 Canvas。"
            );
            RestoreOriginalScene(originalScenePath);
            return;
        }

        GameObject systemObject =
            GameObject.Find("LevelUpChoiceSystem");

        if (systemObject == null)
        {
            systemObject = new GameObject(
                "LevelUpChoiceSystem",
                typeof(RectTransform)
            );

            systemObject.transform.SetParent(
                canvas.transform,
                false
            );
        }

        LevelUpChoiceSystem system =
            systemObject.GetComponent<LevelUpChoiceSystem>();

        if (system == null)
        {
            system = systemObject.AddComponent<LevelUpChoiceSystem>();
        }

        system.addDamageSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI_Metirial/skillUI/addDamage.png"
            );

        system.addHealthSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI_Metirial/skillUI/addHP.png"
            );

        system.restoreHealthSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/UI_Metirial/skillUI/restoreHP.png"
            );

        PlayerStats playerStats =
            Object.FindAnyObjectByType<PlayerStats>(
                FindObjectsInactive.Include
            );

        if (playerStats != null)
        {
            playerStats.attack = 25f;
            playerStats.levelUpChoiceSystem = system;
        }

        EditorUtility.SetDirty(system);

        if (playerStats != null)
        {
            EditorUtility.SetDirty(playerStats);
        }

        EditorSceneManager.MarkSceneDirty(gameplayScene);
        EditorSceneManager.SaveScene(gameplayScene);

        Debug.Log(
            "[Level Up] 二選一技能、全螢幕遮罩與三張技能卡已完成綁定。"
        );

        RestoreOriginalScene(originalScenePath);
    }

    public static void RestoreOriginalScene(
        string originalScenePath
    )
    {
        if (!string.IsNullOrEmpty(originalScenePath) &&
            originalScenePath != GameplayScenePath)
        {
            EditorSceneManager.OpenScene(
                originalScenePath,
                OpenSceneMode.Single
            );
        }
    }
}
#endif
