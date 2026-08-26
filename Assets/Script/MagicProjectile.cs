using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public GameObject owner;
    public float damage = 25f;
    public float lifeTime = 4f;
    public Color fireColor = new Color(1f, 0.24f, 0.02f, 1f);
    public bool hasHit;

    public void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void BuildFireLook()
    {
        Renderer projectileRenderer = GetComponent<Renderer>();

        if (projectileRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Standard");

            Material fireMaterial = new Material(shader);
            fireMaterial.color = fireColor;

            if (fireMaterial.HasProperty("_EmissionColor"))
            {
                fireMaterial.EnableKeyword("_EMISSION");
                fireMaterial.SetColor(
                    "_EmissionColor",
                    fireColor * 3f
                );
            }

            projectileRenderer.material = fireMaterial;
        }

        Light fireLight = gameObject.AddComponent<Light>();
        fireLight.color = fireColor;
        fireLight.range = 2.5f;
        fireLight.intensity = 2f;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.gameObject == owner ||
            other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        EnemyFollowPlayer enemy =
            other.GetComponentInParent<EnemyFollowPlayer>();

        if (enemy != null)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            if (health == null)
                health = enemy.gameObject.AddComponent<EnemyHealth>();

            hasHit = true;
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
