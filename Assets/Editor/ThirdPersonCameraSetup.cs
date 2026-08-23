using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdPersonCameraSetup
{
    [MenuItem("Tools/Froggy Seasons/Setup Third Person Camera")]
    public static void SetupThirdPersonCamera()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != "InGameScene")
        {
            Debug.LogWarning("[Froggy Camera] Open InGameScene before setup.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        GameObject cameraObject = GameObject.Find("Main Camera");

        if (player == null || cameraObject == null)
        {
            Debug.LogError("[Froggy Camera] Player or Main Camera is missing.");
            return;
        }

        ThirdPersonCamera cameraControl = cameraObject.GetComponent<ThirdPersonCamera>();

        if (cameraControl == null)
            cameraControl = cameraObject.AddComponent<ThirdPersonCamera>();

        cameraControl.target = player.transform;
        cameraControl.distance = 5f;
        cameraControl.targetHeight = 1f;
        cameraControl.pitch = 20f;
        cameraControl.touchSensitivity = 0.12f;
        cameraControl.mouseSensitivity = 0.08f;
        cameraControl.smoothSpeed = 12f;
        cameraControl.minPitch = 10f;
        cameraControl.maxPitch = 65f;

        Camera gameCamera = cameraObject.GetComponent<Camera>();

        if (gameCamera != null)
        {
            gameCamera.orthographic = false;
            gameCamera.fieldOfView = 60f;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();

        if (playerController != null)
            playerController.gameCamera = cameraObject;

        EditorUtility.SetDirty(cameraObject);
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = cameraObject;
        Debug.Log("[Froggy Camera] Third-person camera and right-half touch control are ready.");
    }
}
