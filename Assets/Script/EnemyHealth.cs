using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isDead;

    public void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (MusicManager.instance != null)
            MusicManager.instance.PlayMonsterHitSfx();

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Min(
            currentHealth + amount,
            maxHealth
        );
    }

    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddKill();

        Destroy(gameObject);
    }
}
