using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    public float damageTakenMultiplier { get; private set; } = 1f;
    public float meleeDamageMultiplier { get; private set; } = 1f;
    public float meleeRangeBonus { get; private set; } = 0f;
    public float rangedDamageMultiplier { get; private set; } = 1f;
    public float rangedCooldownMultiplier { get; private set; } = 1f;
    public float projectileSpeedMultiplier { get; private set; } = 1f;
    public float healMultiplier { get; private set; } = 1f;
    public bool hasFireball { get; private set; }
    public bool hasFreeze { get; private set; }
    public bool isInvisible { get; private set; }
    public bool canUseMeleeAbility { get; private set; }
    public bool canUseRangedAbility { get; private set; }

    private PlayerHealth health;
    private PlayerCombat combat;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        combat = GetComponent<PlayerCombat>();
        RecalculateFromGameManager();
    }

    void Start()
    {
        RecalculateFromGameManager();
    }

    public void RecalculateFromGameManager()
    {
        if (GameManager.Instance == null)
        {
            ApplyAbilities(null);
            return;
        }

        ApplyAbilities(GameManager.Instance.selectedAbilities);
    }

    public void ApplyAbilities(List<AbilityData> abilities)
    {
        damageTakenMultiplier = 1f;
        meleeDamageMultiplier = 1f;
        meleeRangeBonus = 0f;
        rangedDamageMultiplier = 1f;
        rangedCooldownMultiplier = 1f;
        projectileSpeedMultiplier = 1f;
        healMultiplier = 1f;
        hasFireball = false;
        hasFreeze = false;
        isInvisible = false;
        canUseMeleeAbility = false;
        canUseRangedAbility = false;

        if (abilities != null)
        {
            foreach (AbilityData ability in abilities)
            {
                if (ability == null) continue;

                switch (ability.GetResolvedEffect())
                {
                    case AbilityEffect.SwordMastery:
                        canUseMeleeAbility = true;
                        meleeDamageMultiplier += 0.35f;
                        meleeRangeBonus += 0.25f;
                        break;

                    case AbilityEffect.Shield:
                        canUseMeleeAbility = true;
                        damageTakenMultiplier *= 0.70f;
                        break;

                    case AbilityEffect.BowMastery:
                        canUseRangedAbility = true;
                        rangedDamageMultiplier += 0.25f;
                        rangedCooldownMultiplier *= 0.80f;
                        projectileSpeedMultiplier += 0.20f;
                        break;

                    case AbilityEffect.Crossbow:
                        canUseRangedAbility = true;
                        rangedDamageMultiplier += 0.55f;
                        rangedCooldownMultiplier *= 1.15f;
                        break;

                    case AbilityEffect.Fireball:
                        canUseRangedAbility = true;
                        hasFireball = true;
                        rangedDamageMultiplier += 0.20f;
                        break;

                    case AbilityEffect.Freeze:
                        canUseRangedAbility = true;
                        hasFreeze = true;
                        break;

                    case AbilityEffect.Invisibility:
                        isInvisible = true;
                        break;

                    case AbilityEffect.PotionMastery:
                        healMultiplier += 0.50f;
                        break;
                }
            }
        }

        if (combat != null)
            combat.ApplyAbilityStats(this);

        if (health != null)
            health.ApplyAbilityStats(this);

        Debug.Log(
            "Abilities applied: " +
            $"melee=x{meleeDamageMultiplier:F2}, " +
            $"ranged=x{rangedDamageMultiplier:F2}, " +
            $"damageTaken=x{damageTakenMultiplier:F2}, " +
            $"fireball={hasFireball}, " +
            $"freeze={hasFreeze}, " +
            $"invisible={isInvisible}, " +
            $"heal=x{healMultiplier:F2}");
    }
}
