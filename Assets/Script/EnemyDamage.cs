using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public const float MaximumDamage = 750f;

    public float damage = 5f;
    public float attackInterval = 1f;

    public PlayerStats playerStats;

    public bool touchingPlayer = false;
    public float attackTimer = 0f;

    public float CurrentDamage => Mathf.Clamp(damage, 0f, MaximumDamage);

    public void Awake()
    {
        damage = CurrentDamage;
    }

    public void OnValidate()
    {
        damage = CurrentDamage;
    }

    public void Update()
    {
        if (!touchingPlayer || playerStats == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            playerStats.TakeDamage(CurrentDamage);

            attackTimer = 0f;
        }
    }

    public void StartDamage(PlayerStats player)
    {
        if (touchingPlayer)
            return;

        playerStats = player;
        touchingPlayer = true;

        // 一碰到立刻扣一次血
        playerStats.TakeDamage(CurrentDamage);

        // 然後重新開始計時
        attackTimer = 0f;
    }

    public void StopDamage()
    {
        touchingPlayer = false;
        playerStats = null;
        attackTimer = 0f;
    }

    public void IncreaseDamage(float amount)
    {
        if (amount <= 0f)
            return;

        damage = Mathf.Min(MaximumDamage, CurrentDamage + amount);
    }
}
