using UnityEditor;

public static class FroggySfxSetup
{
    [MenuItem("Tools/Froggy Seasons/Setup SFX")]
    public static void SetupSfx()
    {
        MusicManagerAudioSetup.SetupAllScenes();
    }
}
