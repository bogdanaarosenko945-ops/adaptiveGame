using UnityEngine;

public class TankEnemy : EnemyBase
{
    protected override void Start()
    {
        base.Start();
        enemyType = EnemyType.Tank;
        maxHealth = 100;
        currentHealth = maxHealth;
        damage = 20;
        moveSpeed = 1f;
        attackRange = 1.5f;
        attackCooldown = 2f;
        detectionRange = 6f;
    }

    void Update()
    {
        if (isDead || !CanDetectPlayer())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = DistanceToPlayer();

        if (dist < detectionRange)
        {
            if (dist > attackRange)
                rb.linearVelocity =
                    DirectionToPlayer() * GetEffectiveMoveSpeed();
            else
            {
                rb.linearVelocity = Vector2.zero;
                TryAttack();
            }
        }
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        PlayerHealth ph = player
            .GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);

        Debug.Log($"Танк атакує! -{damage} HP");
    }
}
