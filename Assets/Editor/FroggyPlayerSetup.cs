using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FroggyPlayerSetup
{
    public const string ScenePath = "Assets/Scenes/InGameScene.unity";
    public const string PlayerFolder = "Assets/Player";
    public const string CharacterPath = PlayerFolder + "/guaguagua_mixamo_ready.fbx";
    public const string IdlePath = PlayerFolder + "/Zombie Idle.fbx";
    public const string RunPath = PlayerFolder + "/Fast Run (1).fbx";
    public const string HealPath = PlayerFolder + "/Magic Heal.fbx";
    public const string ControllerPath = PlayerFolder + "/FroggyPlayer.controller";

    [MenuItem("Tools/Froggy Seasons/Setup Player And Animations")]
    public static void SetupPlayerAndAnimations()
    {
        ConfigureCharacterRig();

        Avatar avatar = LoadAvatar(CharacterPath);
        if (avatar == null)
        {
            Debug.LogError("[Froggy Player] Character Avatar could not be created.");
            return;
        }

        ConfigureAnimationRig(IdlePath, avatar, true);
        ConfigureAnimationRig(RunPath, avatar, true);
        ConfigureAnimationRig(HealPath, avatar, false);

        AnimationClip idleClip = LoadAnimationClip(IdlePath);
        AnimationClip runClip = LoadAnimationClip(RunPath);
        AnimationClip healClip = LoadAnimationClip(HealPath);

        if (idleClip == null || runClip == null || healClip == null)
        {
            Debug.LogError("[Froggy Player] One or more animation clips are missing.");
            return;
        }

        AnimatorController controller = CreateAnimatorController(idleClip, runClip, healClip);
        CreatePlayerInScene(controller, avatar);
        FroggySceneBuilder.AddScenesToBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("[Froggy Player] Player, joystick movement, camera follow, terrain collision, Idle, Run and Magic Heal are ready.");
    }

    [MenuItem("Tools/Froggy Seasons/Repair Player Animations Only")]
    public static void RepairPlayerAnimationsOnly()
    {
        ConfigureCharacterRig();

        Avatar avatar = LoadAvatar(CharacterPath);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            Debug.LogError("[Froggy Player] A valid Humanoid Avatar could not be created from guaguagua_mixamo_ready.");
            return;
        }

        ConfigureAnimationRig(IdlePath, avatar, true);
        ConfigureAnimationRig(RunPath, avatar, true);
        ConfigureAnimationRig(HealPath, avatar, false);

        AnimationClip idleClip = LoadAnimationClip(IdlePath);
        AnimationClip runClip = LoadAnimationClip(RunPath);
        AnimationClip healClip = LoadAnimationClip(HealPath);

        if (idleClip == null || runClip == null || healClip == null)
        {
            Debug.LogError("[Froggy Player] One or more animation clips are missing.");
            return;
        }

        AnimatorController controller = CreateAnimatorController(idleClip, runClip, healClip);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = FindSceneObject(scene, "Player");

        if (player == null)
        {
            Debug.LogError("[Froggy Player] Player is missing from InGameScene.");
            return;
        }

        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = player.AddComponent<Animator>();

        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[Froggy Player] Humanoid Avatar repaired. Zombie Idle is default; Fast Run follows joystick movement; Magic Heal is an override animation.");
    }

    public static void ConfigureCharacterRig()
    {
        ModelImporter importer = AssetImporter.GetAtPath(CharacterPath) as ModelImporter;
        if (importer == null)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = false;
        importer.SaveAndReimport();
    }

    public static void ConfigureAnimationRig(string assetPath, Avatar avatar, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.sourceAvatar = null;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = loop;
            clip.loopPose = loop;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    public static Avatar LoadAvatar(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Avatar>()
            .FirstOrDefault();
    }

    public static AnimationClip LoadAnimationClip(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    public static AnimatorController CreateAnimatorController(
        AnimationClip idleClip,
        AnimationClip runClip,
        AnimationClip healClip)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MagicHeal", AnimatorControllerParameterType.Trigger);

        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].defaultWeight = 1f;
        controller.layers = layers;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Zombie Idle");
        AnimatorState runState = stateMachine.AddState("Fast Run (1)");
        AnimatorState healState = stateMachine.AddState("Magic Heal");

        idleState.motion = idleClip;
        runState.motion = runClip;
        healState.motion = healClip;
        stateMachine.defaultState = idleState;

        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.12f;
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.12f;
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        AnimatorStateTransition anyToHeal = stateMachine.AddAnyStateTransition(healState);
        anyToHeal.hasExitTime = false;
        anyToHeal.duration = 0.08f;
        anyToHeal.canTransitionToSelf = false;
        anyToHeal.AddCondition(AnimatorConditionMode.If, 0, "MagicHeal");

        AnimatorStateTransition healToRun = healState.AddTransition(runState);
        healToRun.hasExitTime = true;
        healToRun.exitTime = 0.92f;
        healToRun.duration = 0.1f;
        healToRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

        AnimatorStateTransition healToIdle = healState.AddTransition(idleState);
        healToIdle.hasExitTime = true;
        healToIdle.exitTime = 0.92f;
        healToIdle.duration = 0.1f;
        healToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    public static void CreatePlayerInScene(AnimatorController controller, Avatar avatar)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject oldPlayer = FindSceneObject(scene, "Player");
        if (oldPlayer != null)
            Object.DestroyImmediate(oldPlayer);

        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
        if (characterPrefab == null)
        {
            Debug.LogError("[Froggy Player] Character FBX is missing.");
            return;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab, scene);
        player.name = "Player";
        player.tag = "Player";
        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        ResizeCharacter(player, 3f);
        AddTerrainColliders(scene);
        SetupDeadplaceTrigger(scene);

        GameObject fateIsland = FindSceneObject(scene, "Fate Isle Continent");
        Bounds islandBounds = GetRenderBounds(fateIsland);
        Vector3 spawnPosition = islandBounds.size == Vector3.zero
            ? new Vector3(0, 5, 0)
            : new Vector3(islandBounds.center.x, islandBounds.max.y + 0.2f, islandBounds.center.z);
        player.transform.position = spawnPosition;

        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator == null)
            animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;

        Bounds playerBounds = GetRenderBounds(player);
        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = player.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.center = player.transform.InverseTransformPoint(playerBounds.center)
            + Vector3.up * 0.02f;
        capsule.height = playerBounds.size.y / player.transform.lossyScale.y;
        float calculatedRadius = Mathf.Max(playerBounds.extents.x, playerBounds.extents.z)
            / Mathf.Max(player.transform.lossyScale.x, player.transform.lossyScale.z);
        capsule.radius = Mathf.Min(calculatedRadius, capsule.height * 0.45f);

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
            rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        VariableJoystick joystick = Object.FindObjectsByType<VariableJoystick>(
            FindObjectsInactive.Include
        ).FirstOrDefault();
        Camera camera = Camera.main;
        if (camera == null)
            camera = Object.FindObjectsByType<Camera>().FirstOrDefault();

        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 14f;
            camera.transform.position = spawnPosition + Vector3.up * 35f;
            camera.transform.rotation = Quaternion.Euler(90f, 0, 0);
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
            playerController = player.AddComponent<PlayerController>();
        playerController.variableJoystick = joystick;
        playerController.rb = rb;
        playerController.animator = animator;
        playerController.gameCamera = camera == null ? null : camera.gameObject;
        playerController.speed = 8f;
        playerController.rotateSpeed = 12f;

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(playerController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    public static void AddTerrainColliders(Scene scene)
    {
        string[] terrainNames =
        {
            "Fate Isle Continent",
            "Spring Continent",
            "Summer Continent",
            "Autumn Continent",
            "Winter Continent"
        };

        foreach (string terrainName in terrainNames)
        {
            GameObject terrain = FindSceneObject(scene, terrainName);
            if (terrain == null)
                continue;

            MeshFilter filter = terrain.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            MeshCollider collider = terrain.GetComponent<MeshCollider>();
            if (collider == null)
                collider = terrain.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);
        }
    }

    public static void SetupDeadplaceTrigger(Scene scene)
    {
        GameObject oldDeadplace = FindSceneObject(scene, "Deadplace");
        if (oldDeadplace != null)
        {
            MeshCollider oldCollider = oldDeadplace.GetComponent<MeshCollider>();
            if (oldCollider != null)
                oldCollider.enabled = false;

            Deadplace oldScript = oldDeadplace.GetComponent<Deadplace>();
            if (oldScript != null)
                Object.DestroyImmediate(oldScript);
        }

        string[] terrainNames =
        {
            "Fate Isle Continent",
            "Spring Continent",
            "Summer Continent",
            "Autumn Continent",
            "Winter Continent"
        };

        Bounds terrainBounds = new Bounds();
        bool foundTerrain = false;

        foreach (string terrainName in terrainNames)
        {
            GameObject terrain = FindSceneObject(scene, terrainName);
            if (terrain == null)
                continue;

            Bounds bounds = GetRenderBounds(terrain);
            if (bounds.size == Vector3.zero)
                continue;

            if (!foundTerrain)
            {
                terrainBounds = bounds;
                foundTerrain = true;
            }
            else
            {
                terrainBounds.Encapsulate(bounds);
            }
        }

        if (!foundTerrain)
            return;

        GameObject triggerObject = FindSceneObject(scene, "DeadplaceTrigger");
        if (triggerObject == null)
        {
            triggerObject = new GameObject("DeadplaceTrigger");
            SceneManager.MoveGameObjectToScene(triggerObject, scene);
        }

        triggerObject.transform.SetPositionAndRotation(
            new Vector3(
                terrainBounds.center.x,
                terrainBounds.min.y - 2f,
                terrainBounds.center.z
            ),
            Quaternion.identity
        );
        triggerObject.transform.localScale = Vector3.one;

        BoxCollider trigger = triggerObject.GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = triggerObject.AddComponent<BoxCollider>();
        trigger.center = Vector3.zero;
        trigger.size = new Vector3(
            terrainBounds.size.x * 1.15f,
            2f,
            terrainBounds.size.z * 1.15f
        );
        trigger.isTrigger = true;

        Deadplace deadplace = triggerObject.GetComponent<Deadplace>();
        if (deadplace == null)
            deadplace = triggerObject.AddComponent<Deadplace>();

        GameObject uiManagerObject = FindSceneObject(scene, "UIManager");
        if (uiManagerObject != null)
            deadplace.uiManager = uiManagerObject.GetComponent<UIManager>();

        if (oldDeadplace != null)
            EditorUtility.SetDirty(oldDeadplace);
        EditorUtility.SetDirty(triggerObject);
        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(deadplace);
    }

    public static void ResizeCharacter(GameObject player, float targetHeight)
    {
        Bounds bounds = GetRenderBounds(player);
        if (bounds.size.y <= 0)
            return;

        float scale = targetHeight / bounds.size.y;
        player.transform.localScale = player.transform.localScale * scale;
    }

    public static Bounds GetRenderBounds(GameObject target)
    {
        if (target == null)
            return new Bounds();

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    public static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }
}
