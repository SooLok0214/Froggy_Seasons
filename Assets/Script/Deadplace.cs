using UnityEngine;

public class Deadplace : MonoBehaviour
{
    // 保留給既有 FroggyPlayerSetup Editor 工具指定；
    // 實際死亡流程統一交給 GameManager。
    public UIManager uiManager;
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
