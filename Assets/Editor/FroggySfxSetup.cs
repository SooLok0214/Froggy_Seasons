using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class FroggySfxSetup
{
    public const string MainScenePath = "Assets/Scenes/Main_Use_Scene.unity";
    public const string GameplayScenePath = "Assets/Scenes/InGameScene.unity";
    public const string ButtonSfxPath = "Assets/music test/pisseim-mund-online-audio-converter.mp3";
    public const string FrogCroakPath = "Assets/music test/frog-croak.mp3";
    public const string MixerPath = "Assets/AudioMixer.mixer";

    [MenuItem("Tools/Froggy Seasons/Setup SFX")]
    public static void SetupSfx()
    {
        AudioClip buttonSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(ButtonSfxPath);
        AudioClip frogCroak = AssetDatabase.LoadAssetAtPath<AudioClip>(FrogCroakPath);
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        AudioMixerGroup sfxGroup = mixer == null ? null : mixer.FindMatchingGroups("SFX").FirstOrDefault();

        if (buttonSfx == null || frogCroak == null || mixer == null || sfxGroup == null)
        {
            Debug.LogError("[Froggy Audio] Missing an audio clip, AudioMixer, or SFX mixer group.");
            return;
        }

        SetupScene(MainScenePath, buttonSfx, frogCroak, sfxGroup);
        SetupScene(GameplayScenePath, buttonSfx, frogCroak, sfxGroup);

        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log("[Froggy Audio] BGM, SFX, UI clicks, and mute toggles are consolidated in MusicManager.");
    }

    public static void SetupScene(string scenePath, AudioClip buttonSfx, AudioClip frogCroak, AudioMixerGroup sfxGroup)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        MusicManager manager = Object.FindObjectsByType<MusicManager>(FindObjectsInactive.Include).FirstOrDefault();

        if (manager == null)
        {
            Debug.LogError("[Froggy Audio] MusicManager was not found in " + scenePath);
            return;
        }

        GameObject oldSfxManager = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "SFXManager");

        if (oldSfxManager != null)
            Object.DestroyImmediate(oldSfxManager);

        AudioSource source = manager.sfxSource;

        if (source == null)
            source = manager.GetComponents<AudioSource>().FirstOrDefault(item => item.outputAudioMixerGroup == sfxGroup);

        if (source == null)
            source = manager.gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = sfxGroup;

        manager.sfxSource = source;
        manager.buttonClickSfx = buttonSfx;
        manager.frogCroakSfx = frogCroak;
        manager.sfxMixerGroup = sfxGroup;

        EditorUtility.SetDirty(source);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
