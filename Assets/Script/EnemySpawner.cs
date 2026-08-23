using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
    public Transform player;

    public float spawnTime = 5f;
    public float minSpawnDistance = 8f;
    public float maxSpawnDistance = 15f;

    public float spawnY = 3f;

    public int maxEnemies = 20;

    public void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        InvokeRepeating("SpawnEnemy", 2f, spawnTime);
    }

    public void SpawnEnemy()
    {
        if (player == null ||
            enemies == null ||
            enemies.Length == 0)
        {
            return;
        }

        if (FindObjectsByType<EnemyFollowPlayer>(
            FindObjectsSortMode.None
        ).Length >= maxEnemies)
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

        EnemyFollowPlayer follow =
            enemy.GetComponent<EnemyFollowPlayer>();

        if (follow == null)
        {
            follow =
                enemy.AddComponent<EnemyFollowPlayer>();
        }

        follow.player = player;
    }
}