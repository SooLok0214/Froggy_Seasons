using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
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
    public float minSpawnDistance = 8f;
    public float maxSpawnDistance = 15f;

    public float spawnY = 3f;

    public int maxEnemies = 20;

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

        Vector2 randomDirection =
            Random.insideUnitCircle.normalized;

        float distance =
            Random.Range(
                minSpawnDistance,
                maxSpawnDistance
            );

        Vector3 spawnPosition =
            player.position +
            new Vector3(
                randomDirection.x,
                0,
                randomDirection.y
            ) * distance;

        // 固定所有怪物生成高度
        spawnPosition.y = spawnY;

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
