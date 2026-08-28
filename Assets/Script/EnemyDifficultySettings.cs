using UnityEngine;

public class EnemyDifficultySettings : MonoBehaviour
{
    public static EnemyDifficultySettings instance;

    [Header("Difficulty Time")]
    public float difficultyInterval = 30f;

    [Header("Increase Every Tier")]
    public float damagePerTier = 3f;
    public float healthPerTier = 20f;
    public float speedPerTier = 1f;

    [Header("Enemy Lifetime")]
    public float enemyLifeTime = 20f;

    public void Awake()
    {
        instance = this;
    }
}
