using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public float damage = 5f;
    public float attackInterval = 1f;

    public PlayerStats playerStats;

    public bool touchingPlayer = false;
    public float attackTimer = 0f;

    public void Update()
    {
        if (!touchingPlayer || playerStats == null)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            playerStats.TakeDamage(damage);

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
        playerStats.TakeDamage(damage);

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
        damage += amount;
    }
}