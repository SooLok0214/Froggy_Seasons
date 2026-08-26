using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public int currentLevel = 1;
    public float currentExp = 0f;
    public float expToLevel = 100f;
    public float expPerKill = 10f;

    public float attack = 10f;
    public float attackSpeed = 1f;

    public bool isDead = false;

    public ScoreManager scoreManager;
    public UIManager uiManager;

    public AudioSource audioSource;
    public AudioClip atkSFX;

    public void Start()
    {
        currentHealth = maxHealth;
        currentLevel = 1;
        currentExp = 0f;

        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        SyncLevelRecord();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        if (audioSource != null && atkSFX != null)
        {
            audioSource.PlayOneShot(atkSFX);
        }

        Debug.Log("Player HP: " + currentHealth + " / " + maxHealth);

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

        currentHealth += amount;

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
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
        SyncLevelRecord();
    }

    public void SyncLevelRecord()
    {
        if (scoreManager == null)
        {
            scoreManager = FindAnyObjectByType<ScoreManager>();
        }

        if (scoreManager != null)
        {
            scoreManager.SetLevel(currentLevel);
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (uiManager != null)
        {
            uiManager.GameOver();
        }
    }
}
