using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool isDead;

    [Header("Hit Feedback")]
    public Color hitColor = Color.red;
    [Range(0f, 1f)] public float hitColorOpacity = 0.8f;
    [Min(0f)]
    public float hitFlashDuration = 0.12f;

    [Header("Death Feedback")]
    public Vector3 deathFallRotation = new Vector3(0f, 0f, -90f);
    [Min(0f)] public float deathFallDuration = 0.45f;
    [Min(0f)] public float deathDisappearDelay = 0.25f;

    [HideInInspector] public Coroutine hitFlashCoroutine;
    [HideInInspector] public List<HitRenderTarget> hitRenderTargets = new List<HitRenderTarget>();

    public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    public static readonly int ColorId = Shader.PropertyToID("_Color");

    public class HitRenderTarget
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorPropertyId;
        public Color originalColor;
        public MaterialPropertyBlock originalProperties;
    }

    public void Awake()
    {
        currentHealth = maxHealth;
        CacheHitRenderTargets();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (currentHealth >= previousHealth)
            return;

        PlayHitFeedback();

        if (MusicManager.instance != null)
            MusicManager.instance.PlayMonsterHitSfx();

        if (currentHealth <= 0f)
            Die();
    }

    public void CacheHitRenderTargets()
    {
        hitRenderTargets.Clear();

        foreach (Renderer enemyRenderer in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = enemyRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                int colorPropertyId = material.HasProperty(BaseColorId)
                    ? BaseColorId
                    : material.HasProperty(ColorId) ? ColorId : 0;

                if (colorPropertyId == 0)
                    continue;

                MaterialPropertyBlock originalProperties = new MaterialPropertyBlock();
                enemyRenderer.GetPropertyBlock(originalProperties, materialIndex);

                hitRenderTargets.Add(new HitRenderTarget
                {
                    renderer = enemyRenderer,
                    materialIndex = materialIndex,
                    colorPropertyId = colorPropertyId,
                    originalColor = material.GetColor(colorPropertyId),
                    originalProperties = originalProperties
                });
            }
        }
    }

    public void PlayHitFeedback()
    {
        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        hitFlashCoroutine = StartCoroutine(FlashRed());
    }

    public IEnumerator FlashRed()
    {
        foreach (HitRenderTarget target in hitRenderTargets)
        {
            if (target.renderer == null)
                continue;

            MaterialPropertyBlock hitProperties = new MaterialPropertyBlock();
            target.renderer.GetPropertyBlock(hitProperties, target.materialIndex);
            hitProperties.SetColor(
                target.colorPropertyId,
                Color.Lerp(target.originalColor, hitColor, hitColorOpacity)
            );
            target.renderer.SetPropertyBlock(hitProperties, target.materialIndex);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        foreach (HitRenderTarget target in hitRenderTargets)
        {
            if (target.renderer != null)
                target.renderer.SetPropertyBlock(
                    target.originalProperties,
                    target.materialIndex
                );
        }

        hitFlashCoroutine = null;
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
        StopEnemyInteractions();

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddKill();

        StartCoroutine(DeathSequence());
    }

    public void StopEnemyInteractions()
    {
        foreach (EnemyFollowPlayer movement in GetComponentsInChildren<EnemyFollowPlayer>(true))
            movement.enabled = false;

        foreach (EnemyDamage damage in GetComponentsInChildren<EnemyDamage>(true))
            damage.enabled = false;

        foreach (EnemyDamageTrigger damageTrigger in GetComponentsInChildren<EnemyDamageTrigger>(true))
            damageTrigger.enabled = false;

        foreach (Collider enemyCollider in GetComponentsInChildren<Collider>(true))
            enemyCollider.enabled = false;

        foreach (Rigidbody enemyBody in GetComponentsInChildren<Rigidbody>(true))
        {
            // Kinematic bodies have no simulated velocity to clear. Unity 6
            // warns when velocity is assigned to an already kinematic body.
            if (!enemyBody.isKinematic)
            {
                enemyBody.linearVelocity = Vector3.zero;
                enemyBody.angularVelocity = Vector3.zero;
            }
            enemyBody.isKinematic = true;
        }
    }

    public IEnumerator DeathSequence()
    {
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(deathFallRotation);

        if (deathFallDuration <= 0f)
        {
            transform.localRotation = targetRotation;
        }
        else
        {
            float elapsedTime = 0f;
            while (elapsedTime < deathFallDuration)
            {
                elapsedTime += Time.deltaTime;
                transform.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    Mathf.Clamp01(elapsedTime / deathFallDuration)
                );
                yield return null;
            }

            transform.localRotation = targetRotation;
        }

        if (deathDisappearDelay > 0f)
            yield return new WaitForSeconds(deathDisappearDelay);

        Destroy(gameObject);
    }
}
