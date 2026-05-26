using UnityEngine;

public class MeleeEnemy : EnemyBase
{
    protected override void Start()
    {
        base.Start();
        enemyType = EnemyType.Melee;
        maxHealth = 30;
        currentHealth = maxHealth;
        damage = 15;
        moveSpeed = 3f;
        attackRange = 1.2f;
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
                MoveToPlayer();
            else
                TryAttack();
        }
    }

    void MoveToPlayer()
    {
        rb.linearVelocity =
            DirectionToPlayer() * GetEffectiveMoveSpeed();
    }

    void TryAttack()
    {
        rb.linearVelocity = Vector2.zero;

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        PlayerHealth ph = player
            .GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);

        Debug.Log($"Мечник атакує! -{damage} HP");
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            TryAttack();
    }
}
