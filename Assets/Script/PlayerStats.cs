using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToLevel = 100f;
    public float expPerKill = 10f;

    public float attack = 25f;
    public float attackSpeed = 1f;
    public float speedIncreaseEveryFiveLevels = 0.5f;

    public bool isDead = false;

    public ScoreManager scoreManager;
    public PlayerController playerController;
    public LevelUpChoiceSystem levelUpChoiceSystem;

    public AudioSource audioSource;
    public AudioClip atkSFX;

    public void Start()
    {
        currentHealth = maxHealth;
        currentLevel = 1;
        currentExp = 0f;

        CacheReferences();

        // 火球的基礎傷害是 25。舊場景若仍保存為 10，載入時同步更新。
        if (attack < 25f)
        {
            attack = 25f;
        }

        SyncLevelRecord();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (audioSource != null && atkSFX != null)
        {
            audioSource.PlayOneShot(atkSFX);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void IncreaseAttack(float amount)
    {
        attack += amount;
    }

    public void IncreaseAttackSpeed(float amount)
    {
        attackSpeed += amount;
    }

    public void AddExperience(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        currentExp += amount;

        if (currentExp >= expToLevel)
        {
            currentExp = 0f;
            LevelUp();
        }
    }

    public void LevelUp()
    {
        currentLevel++;

        if (currentLevel % 5 == 0)
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerController != null)
            {
                playerController.speed += speedIncreaseEveryFiveLevels;
            }
        }

        if (MusicManager.instance != null)
        {
            MusicManager.instance.PlayLevelUpSfx();
        }

        SyncLevelRecord();

        if (levelUpChoiceSystem != null)
        {
            levelUpChoiceSystem.ShowChoices(this);
        }
    }

    public void SyncLevelRecord()
    {
        if (scoreManager != null)
        {
            scoreManager.SetLevel(currentLevel);
        }
    }

    public void CacheReferences()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }
}
