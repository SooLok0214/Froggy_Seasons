using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class MusicManagerAudioSetup
{
    [MenuItem("Tools/Froggy/Setup MusicManager Audio Children")]
    public static void SetupAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string originalScene = SceneManager.GetActiveScene().path;

        if (!string.IsNullOrEmpty(originalScene) && SceneManager.GetActiveScene().isDirty)
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        SetupScene("Assets/Scenes/Main_Use_Scene.unity");
        SetupScene("Assets/Scenes/InGameScene.unity");

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        Debug.Log("[Froggy Audio] MusicManager 的 BGM / SFX 子物件與 Audio Mixer 已完成連接。");
    }

    public static void SetupScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        MusicManager[] managers = Object.FindObjectsByType<MusicManager>(FindObjectsInactive.Include);
        MusicManager manager = managers.Length == 0 ? null : managers[0];

        if (manager == null)
        {
            Debug.LogWarning("[Froggy Audio] 找不到 MusicManager: " + scenePath);
            return;
        }

        AudioMixerGroup bgmGroup = FindGroup(manager.mixer, "BGM");
        AudioMixerGroup sfxGroup = FindGroup(manager.mixer, "SFX");

        SetupSource(manager.transform, "BGM-Home", null, bgmGroup, true);
        SetupSource(manager.transform, "BGM-InGame", null, bgmGroup, true);
        SetupSource(manager.transform, "BGM-GameOver", null, bgmGroup, false);
        SetupSource(manager.transform, "SFX-Click", LoadClip("a8d5abdb6b07242479c98803197d9514"), sfxGroup, false);
        SetupSource(manager.transform, "SFX-FrogCroak", LoadClip("f0b312f94c4f679438044522fff3e04e"), sfxGroup, false);
        SetupSource(manager.transform, "SFX-Fire", LoadClip("feae47775d055c345932272ea6ded8b8"), sfxGroup, false);
        SetupSource(manager.transform, "SFX-LevelUp", LoadClip("e9c443ff94211ba43a4d551206fd51b6"), sfxGroup, false);

        AudioSource rootSource = manager.GetComponent<AudioSource>();

        if (rootSource != null)
            Object.DestroyImmediate(rootSource);

        manager.ConnectAudioChildren();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static AudioSource SetupSource(Transform parent, string objectName, AudioClip defaultClip,
        AudioMixerGroup group, bool loop)
    {
        Transform child = parent.Find(objectName);

        if (child == null)
        {
            GameObject childObject = new GameObject(objectName);
            childObject.transform.SetParent(parent, false);
            child = childObject.transform;
        }

        AudioSource source = child.GetComponent<AudioSource>();

        if (source == null)
            source = child.gameObject.AddComponent<AudioSource>();

        if (source.clip == null && defaultClip != null)
            source.clip = defaultClip;

        source.outputAudioMixerGroup = group;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(source);
        return source;
    }

    public static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
    {
        if (mixer == null)
            return null;

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        return groups.Length == 0 ? null : groups[0];
    }

    public static AudioClip LoadClip(string guid)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
    }
}
