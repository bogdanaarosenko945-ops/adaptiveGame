using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public float preferredDistance = 5f;

    protected override void Awake()
    {
        base.Awake();
        enemyType = EnemyType.Ranged;
        maxHealth = 20;
        currentHealth = maxHealth;
        damage = 10;
        moveSpeed = 2f;
        attackRange = 7f;
        attackCooldown = 2f;
    }

    void Update()
    {
        if (isDead || !CanDetectPlayer())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = DistanceToPlayer();
        if (dist > detectionRange) return;

        if (dist < preferredDistance)
            rb.linearVelocity =
                -DirectionToPlayer() * GetEffectiveMoveSpeed();
        else if (dist > attackRange)
            rb.linearVelocity =
                DirectionToPlayer() * GetEffectiveMoveSpeed();
        else
            rb.linearVelocity = Vector2.zero;

        if (dist < attackRange)
            TryShoot();
    }

    void TryShoot()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        lastAttackTime = Time.time;

        if (arrowPrefab != null)
        {
            Vector3 spawnPos = transform.position +
                (Vector3)(DirectionToPlayer() * 0.8f);

            GameObject arrow = Instantiate(
                arrowPrefab,
                spawnPos,
                Quaternion.identity);

            // Встановлюємо видимість
            SpriteRenderer sr =
                arrow.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10;
                sr.color = Color.red;
            }

            // Налаштовуємо стрілу
            Arrow arrowScript =
                arrow.GetComponent<Arrow>();
            if (arrowScript != null)
            {
                arrowScript.damage = damage;
                arrowScript.direction =
                    DirectionToPlayer();
                arrowScript.owner = gameObject;
            }
        }
        else
        {
            if (DistanceToPlayer() < attackRange)
            {
                PlayerHealth ph = player
                    .GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(damage);
                Debug.Log($"Лучник атакує! -{damage}");
            }
        }
    }
}
