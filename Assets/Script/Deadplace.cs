using UnityEngine;

public class Deadplace : MonoBehaviour
{
    public bool deathTriggered;

    public void OnTriggerEnter(Collider other)
    {
        if (deathTriggered || !other.CompareTag("Player"))
            return;

        deathTriggered = true;

        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }
}
