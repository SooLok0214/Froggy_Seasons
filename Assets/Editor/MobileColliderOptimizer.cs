using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MobileColliderOptimizer
{
    public const string SceneName = "InGameScene";
    public const string MapName = "FourSeasonsWorldMap";

    static MobileColliderOptimizer()
    {
        EditorApplication.delayCall += OptimizeOpenScene;
    }

    [MenuItem("Tools/Froggy Seasons/Optimize Mobile Colliders")]
    public static void OptimizeOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject map = FindSceneObject(scene, MapName);
        if (map == null)
            return;

        int addedBoxes = 0;
        int removedMeshes = 0;

        foreach (MeshCollider meshCollider in map.GetComponentsInChildren<MeshCollider>(true))
        {
            if (meshCollider == null || meshCollider.isTrigger)
                continue;

            GameObject target = meshCollider.gameObject;
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            BoxCollider boxCollider = target.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = target.AddComponent<BoxCollider>();
                Bounds bounds = meshFilter.sharedMesh.bounds;
                boxCollider.center = bounds.center;
                boxCollider.size = new Vector3(
                    Mathf.Max(0.05f, bounds.size.x),
                    Mathf.Max(0.12f, bounds.size.y),
                    Mathf.Max(0.05f, bounds.size.z)
                );
                addedBoxes++;
            }

            boxCollider.enabled = true;
            boxCollider.isTrigger = false;
            Object.DestroyImmediate(meshCollider);
            removedMeshes++;
        }

        GameObject player = FindSceneObject(scene, "Player");
        if (player != null)
        {
            foreach (MeshCollider meshCollider in player.GetComponentsInChildren<MeshCollider>(true))
            {
                if (meshCollider != null && !meshCollider.isTrigger)
                {
                    Object.DestroyImmediate(meshCollider);
                    removedMeshes++;
                }
            }
        }

        if (addedBoxes == 0 && removedMeshes == 0)
            return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Mobile Colliders] Added {addedBoxes} BoxColliders and removed {removedMeshes} MeshColliders. Special trigger/boundary colliders were preserved.");
    }

    public static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    public static Transform FindChild(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        foreach (Transform child in current)
        {
            Transform found = FindChild(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
