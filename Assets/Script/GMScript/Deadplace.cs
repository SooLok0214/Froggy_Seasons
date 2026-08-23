using UnityEngine;

public class Deadplace : MonoBehaviour
{
    public UIManager uiManager;
    public bool deathTriggered;

    public void Start()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (deathTriggered || !other.CompareTag("Player"))
            return;

        deathTriggered = true;

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        if (uiManager != null)
            uiManager.GameOver();
    }
}
