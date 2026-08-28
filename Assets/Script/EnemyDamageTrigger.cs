using UnityEngine;

public class EnemyDamageTrigger : MonoBehaviour
{
    public EnemyDamage enemyDamage;

    public void Start()
    {
        if (enemyDamage == null)
        {
            enemyDamage = GetComponentInParent<EnemyDamage>();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        StartPlayerDamage(other);
    }

    public void OnTriggerStay(Collider other)
    {
        StartPlayerDamage(other);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        enemyDamage.StopDamage();
    }

    public void StartPlayerDamage(Collider other)
    {
        if (enemyDamage == null ||
            enemyDamage.touchingPlayer ||
            !other.CompareTag("Player"))
            return;

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats != null)
            enemyDamage.StartDamage(playerStats);
    }
}
