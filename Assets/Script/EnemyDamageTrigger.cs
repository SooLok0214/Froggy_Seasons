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
        if (!other.CompareTag("Player"))
            return;

        PlayerStats playerStats =
            other.GetComponent<PlayerStats>();

        if (playerStats == null)
            return;

        enemyDamage.StartDamage(playerStats);
    }

    public void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerStats playerStats =
            other.GetComponent<PlayerStats>();

        if (playerStats == null)
            return;

        enemyDamage.StartDamage(playerStats);
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        enemyDamage.StopDamage();
    }
}