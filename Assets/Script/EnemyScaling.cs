using UnityEngine;

public class EnemyScaling : MonoBehaviour
{
    public float lifeTimer;
    public float difficultyCheckInterval = 1f;
    public float difficultyCheckTimer;

    public int appliedDifficultyTier;

    public EnemyDifficultySettings settings;
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

        float lifeTime = settings != null
            ? settings.enemyLifeTime
            : 20f;

        if (lifeTimer >= lifeTime)
        {
            // 逾時只清除，不呼叫 EnemyHealth.Die，所以不計入擊殺。
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
        {
            enemyDamage = GetComponent<EnemyDamage>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = gameObject.AddComponent<EnemyHealth>();
        }

        if (enemyFollowPlayer == null)
        {
            enemyFollowPlayer = GetComponent<EnemyFollowPlayer>();
        }
    }

    public int GetCurrentDifficultyTier()
    {
        if (ScoreManager.instance == null)
        {
            return 0;
        }

        float difficultyInterval = settings != null
            ? Mathf.Max(1f, settings.difficultyInterval)
            : 30f;

        return Mathf.FloorToInt(
            ScoreManager.instance.survivalTime /
            difficultyInterval
        );
    }

    public void ApplyCurrentDifficulty()
    {
        CacheEnemyComponents();

        int currentTier = GetCurrentDifficultyTier();

        if (currentTier <= appliedDifficultyTier)
        {
            return;
        }

        int newTiers = currentTier - appliedDifficultyTier;

        if (enemyDamage != null)
        {
            float damagePerTier = settings != null
                ? settings.damagePerTier
                : 3f;

            enemyDamage.IncreaseDamage(
                damagePerTier * newTiers
            );
        }

        if (enemyHealth != null)
        {
            float healthPerTier = settings != null
                ? settings.healthPerTier
                : 20f;

            enemyHealth.IncreaseMaxHealth(
                healthPerTier * newTiers
            );
        }

        if (enemyFollowPlayer != null)
        {
            float speedPerTier = settings != null
                ? settings.speedPerTier
                : 1f;

            enemyFollowPlayer.moveSpeed +=
                speedPerTier * newTiers;
        }

        appliedDifficultyTier = currentTier;
    }
}
