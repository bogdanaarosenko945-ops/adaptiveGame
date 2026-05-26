using UnityEngine;

public enum AbilityType
{
    Melee,
    Ranged,
    Magic,
    Stealth,
    Support
}

public enum AbilityEffect
{
    None,
    SwordMastery,
    Shield,
    BowMastery,
    Crossbow,
    Fireball,
    Freeze,
    Invisibility,
    PotionMastery
}

[CreateAssetMenu(fileName = "Ability",
    menuName = "Palimpsest/Ability")]
public class AbilityData : ScriptableObject
{
    [Header("Base")]
    public string abilityName = "Sword strike";
    public AbilityType abilityType;
    public Sprite icon;

    [Header("Player description")]
    [TextArea]
    public string description;

    [Header("Gameplay effect")]
    public AbilityEffect effect = AbilityEffect.None;

    [Header("Map influence for ML")]
    [Range(-1f, 1f)]
    public float meleeWeight = 0f;
    [Range(-1f, 1f)]
    public float rangedWeight = 0f;
    [Range(-1f, 1f)]
    public float magicWeight = 0f;

    public AbilityEffect GetResolvedEffect()
    {
        if (effect != AbilityEffect.None)
            return effect;

        string lowerName = abilityName != null
            ? abilityName.ToLowerInvariant()
            : "";

        if (lowerName.Contains("щит"))
            return AbilityEffect.Shield;
        if (lowerName.Contains("меч"))
            return AbilityEffect.SwordMastery;
        if (lowerName.Contains("лук"))
            return AbilityEffect.BowMastery;
        if (lowerName.Contains("арбалет"))
            return AbilityEffect.Crossbow;
        if (lowerName.Contains("вогня"))
            return AbilityEffect.Fireball;
        if (lowerName.Contains("замороз"))
            return AbilityEffect.Freeze;
        if (lowerName.Contains("невид"))
            return AbilityEffect.Invisibility;
        if (lowerName.Contains("зіл"))
            return AbilityEffect.PotionMastery;

        switch (abilityType)
        {
            case AbilityType.Melee:
                return AbilityEffect.SwordMastery;
            case AbilityType.Ranged:
                return AbilityEffect.BowMastery;
            case AbilityType.Magic:
                return AbilityEffect.Fireball;
            case AbilityType.Stealth:
                return AbilityEffect.Invisibility;
            case AbilityType.Support:
                return AbilityEffect.PotionMastery;
            default:
                return AbilityEffect.None;
        }
    }

    public string GetDisplayDescription()
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        switch (GetResolvedEffect())
        {
            case AbilityEffect.SwordMastery:
                return "Melee damage +35%, melee range +0.25.";
            case AbilityEffect.Shield:
                return "Incoming damage reduced by 30%.";
            case AbilityEffect.BowMastery:
                return "Ranged damage +25%, faster shots and projectiles.";
            case AbilityEffect.Crossbow:
                return "Ranged damage +55%, but shots are slower.";
            case AbilityEffect.Fireball:
                return "Magic projectiles explode and damage nearby enemies.";
            case AbilityEffect.Freeze:
                return "Projectiles slow enemies for 2 seconds.";
            case AbilityEffect.Invisibility:
                return "Enemies ignore you until you get close enough to attack.";
            case AbilityEffect.PotionMastery:
                return "Healing effects are 50% stronger.";
            default:
                return "Changes player stats and ML adaptation weights.";
        }
    }
}
