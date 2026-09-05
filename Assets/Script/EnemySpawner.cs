using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
    [Tooltip("Target assigned to spawned enemies; does not control the spawn center.")]
    public Transform player;

    [Header("Difficulty Time")]
    public float difficultyInterval = 30f;

    [Header("Increase Every Tier")]
    public float damagePerTier = 3f;
    public float healthPerTier = 20f;
    public float speedPerTier = 1f;

    [Header("Enemy Lifetime")]
    public float enemyLifeTime = 20f;

    public float spawnTime = 5f;
    [Tooltip("Minimum horizontal spawn distance from this EnemySpawner object's world position.")]
    public float minSpawnDistance = 8f;
    [Tooltip("Maximum horizontal spawn distance from this EnemySpawner object's world position.")]
    public float maxSpawnDistance = 15f;

    [Tooltip("Reference height for the ground search; actual spawn Y is fitted to the ground and body collider.")]
    public float spawnY = 3f;

    public int maxEnemies = 20;

    [Header("Safe Spawn")]
    [Tooltip("Minimum horizontal distance from the player's current position. This exclusion area follows the player without moving the spawner-centered range. Set 0 to disable.")]
    [Min(0f)] public float minPlayerSpawnDistance = 10f;
    [Min(1)] public int spawnPositionAttempts = 24;
    [Min(0.01f)] public float spawnGroundClearance = 0.03f;
    [Min(1f)] public float groundSearchHeight = 50f;
    [Min(1f)] public float groundSearchDepth = 100f;
    private readonly RaycastHit[] groundHits = new RaycastHit[128];

    public bool spawning;
    public List<GameObject> spawnedEnemies = new List<GameObject>();

    public void StartSpawning()
    {
        CancelInvoke("SpawnEnemy");
        spawning = true;

        InvokeRepeating("SpawnEnemy", 2f, spawnTime);
    }

    public void StopSpawning()
    {
        spawning = false;
        CancelInvoke("SpawnEnemy");
    }

    public void SpawnEnemy()
    {
        if (!spawning ||
            player == null ||
            enemies == null ||
            enemies.Length == 0)
        {
            return;
        }

        spawnedEnemies.RemoveAll(enemy => enemy == null);

        if (spawnedEnemies.Count >= maxEnemies)
        {
            return;
        }

        int randomEnemy =
            Random.Range(0, enemies.Length + 1);

        // 這一輪有機會不生成
        if (randomEnemy == enemies.Length)
        {
            return;
        }

        if (!TryFindSpawnPosition(enemies[randomEnemy], out Vector3 spawnPosition))
            return; // No clear space this round: never fall back to an occupied point.

        GameObject enemy =
            Instantiate(
                enemies[randomEnemy],
                spawnPosition,
                Quaternion.identity
            );

        spawnedEnemies.Add(enemy);

        EnemyFollowPlayer follow =
            enemy.GetComponent<EnemyFollowPlayer>();

        if (follow == null)
        {
            follow =
                enemy.AddComponent<EnemyFollowPlayer>();
        }

        follow.player = player;

        EnemyScaling scaling =
            enemy.GetComponent<EnemyScaling>();

        if (scaling == null)
        {
            scaling = enemy.AddComponent<EnemyScaling>();
        }

        scaling.settings = this;
        scaling.ApplyCurrentDifficulty();
    }

    public bool TryFindSpawnPosition(GameObject prefab, out Vector3 position)
    {
        position = default;
        if (prefab == null) return false;
        BoxCollider body = prefab.GetComponent<BoxCollider>();
        if (body == null || !body.enabled || body.isTrigger) return false;

        // Sync once per spawn batch, including enemies spawned earlier this frame.
        Physics.SyncTransforms();
        var collision = new EnemyObstacleCollision(body, prefab.transform.localScale,
            gameObject.scene.GetPhysicsScene());
        Vector3 scaledCenter = Vector3.Scale(body.center, prefab.transform.localScale);
        float bodyBottom = scaledCenter.y - Mathf.Abs(body.size.y * prefab.transform.localScale.y) * 0.5f;
        for (int attempt = 0; attempt < Mathf.Clamp(spawnPositionAttempts, 1, 128); attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 candidate = transform.position + new Vector3(direction.x, 0f, direction.y) * distance;
            // Keep a moving safe area around the player, independent of the
            // fixed spawner center. Ignore height differences (XZ distance).
            if (player != null && minPlayerSpawnDistance > 0f)
            {
                Vector3 toPlayer = candidate - player.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < minPlayerSpawnDistance * minPlayerSpawnDistance)
                    continue;
            }
            // Probe under the body center (which can be offset from the pivot).
            if (!TryFindGround(candidate + new Vector3(scaledCenter.x, 0f, scaledCenter.z), out float groundY))
                continue;
            candidate.y = groundY - bodyBottom + Mathf.Max(0.01f, spawnGroundClearance);
            if (!collision.IsClear(candidate, Quaternion.identity)) continue;
            position = candidate;
            return true;
        }
        return false;
    }

    private bool TryFindGround(Vector3 point, out float groundY)
    {
        groundY = float.NegativeInfinity;
        float above = Mathf.Max(1f, groundSearchHeight);
        point.y = Mathf.Max(transform.position.y, spawnY) + above;
        int count = gameObject.scene.GetPhysicsScene().Raycast(point, Vector3.down, groundHits,
            above + Mathf.Max(1f, groundSearchDepth), Physics.AllLayers, QueryTriggerInteraction.Ignore);
        if (count == groundHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.normal.y < 0.7f || !IsSpawnGround(hit.collider)) continue;
            groundY = Mathf.Max(groundY, hit.point.y);
        }
        return !float.IsNegativeInfinity(groundY);
    }

    private static bool IsSpawnGround(Collider collider)
    {
        if (collider == null || collider.isTrigger || collider.attachedRigidbody != null) return false;
        if (collider is TerrainCollider || collider.gameObject.layer == LayerMask.NameToLayer("Ground")) return true;
        // The imported map already groups its ground here. Do not treat trees,
        // houses or mountains as spawn platforms, or change their layers/colliders.
        for (Transform current = collider.transform; current != null; current = current.parent)
            if (current.name == "\u5730\u5F62") return true;
        return false;
    }
}

// Every spawned enemy needs its own lifetime and applied-tier state. The class
// stays a component, but lives with EnemySpawner because it is created only by it.
public class EnemyScaling : MonoBehaviour
{
    public float lifeTimer;
    public float difficultyCheckInterval = 1f;
    public float difficultyCheckTimer;
    public int appliedDifficultyTier;

    public EnemySpawner settings;
    public EnemyDamage enemyDamage;
    public EnemyHealth enemyHealth;
    public EnemyFollowPlayer enemyFollowPlayer;

    public void Start()
    {
        CacheEnemyComponents();
        ApplyCurrentDifficulty();
    }

    public void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= (settings != null ? settings.enemyLifeTime : 20f))
        {
            Destroy(gameObject);
            return;
        }

        difficultyCheckTimer += Time.deltaTime;
        if (difficultyCheckTimer >= difficultyCheckInterval)
        {
            difficultyCheckTimer = 0f;
            ApplyCurrentDifficulty();
        }
    }

    public void CacheEnemyComponents()
    {
        if (enemyDamage == null)
            enemyDamage = GetComponent<EnemyDamage>();
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
        if (enemyFollowPlayer == null)
            enemyFollowPlayer = GetComponent<EnemyFollowPlayer>();
    }

    public int GetCurrentDifficultyTier()
    {
        if (ScoreManager.instance == null)
            return 0;

        float interval = settings != null
            ? Mathf.Max(1f, settings.difficultyInterval)
            : 30f;
        return Mathf.FloorToInt(ScoreManager.instance.survivalTime / interval);
    }

    public void ApplyCurrentDifficulty()
    {
        CacheEnemyComponents();
        int currentTier = GetCurrentDifficultyTier();
        if (currentTier <= appliedDifficultyTier)
            return;

        int newTiers = currentTier - appliedDifficultyTier;
        if (enemyDamage != null)
            enemyDamage.IncreaseDamage(
                (settings != null ? settings.damagePerTier : 3f) * newTiers);
        if (enemyHealth != null)
            enemyHealth.IncreaseMaxHealth(
                (settings != null ? settings.healthPerTier : 20f) * newTiers);
        if (enemyFollowPlayer != null)
            enemyFollowPlayer.moveSpeed +=
                (settings != null ? settings.speedPerTier : 1f) * newTiers;

        appliedDifficultyTier = currentTier;
    }
}
