using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromMainMenu
{
    public const string MainMenuScenePath = "Assets/Scenes/Main_Use_Scene.unity";

    static PlayFromMainMenu()
    {
        SetMainMenuAsPlayStart();
    }

    [MenuItem("Tools/Froggy Seasons/Always Play From Main Menu")]
    public static void SetMainMenuAsPlayStart()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);

        if (mainMenuScene == null)
        {
            UnityEngine.Debug.LogError("[Froggy Scenes] Main_Use_Scene could not be found.");
            return;
        }

        EditorSceneManager.playModeStartScene = mainMenuScene;
    }
}
