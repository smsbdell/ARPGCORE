using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry for known stat IDs and their application logic.
/// New stats can be registered here without changing item/equipment code paths.
/// </summary>
public static class StatRegistry
{
    public static class StatIds
    {
        public const string MaxHealth = "maxHealth";
        public const string MoveSpeed = "moveSpeed";
        public const string BaseDamage = "baseDamage";
        public const string CritChance = "critChance";
        public const string CritMultiplier = "critMultiplier";
        public const string AttackSpeedMultiplier = "attackSpeedMultiplier";
        public const string ProjectileCount = "projectileCount";
        public const string ProjectileSpreadAngle = "projectileSpreadAngle";
        public const string WeaponAttackSpeed = "weaponAttackSpeed";
        public const string ChainCount = "chainCount";
        public const string SplitCount = "splitCount";
        public const string Armor = "armor";
        public const string DodgeChance = "dodgeChance";
        public const string XpGainMultiplier = "xpGainMultiplier";
        public const string CooldownReduction = "cooldownReduction";
        public const string WeaponDamageMin = "weaponDamageMin";
        public const string WeaponDamageMax = "weaponDamageMax";
        public const string WeaponDamage = "weaponDamage";
    }

    private static readonly Dictionary<string, StatDefinition> Definitions = new Dictionary<string, StatDefinition>();

    static StatRegistry()
    {
        Register(StatIds.MaxHealth, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.maxHealth, value, op, sign));
        Register(StatIds.MoveSpeed, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.moveSpeed, value, op, sign));
        Register(StatIds.BaseDamage, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.baseDamage, value, op, sign));
        Register(StatIds.CritChance, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.critChance, value, op, sign));
        Register(StatIds.CritMultiplier, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.critMultiplier, value, op, sign));
        Register(StatIds.AttackSpeedMultiplier, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.attackSpeedMultiplier, value, op, sign));
        Register(StatIds.ProjectileCount, StatOperation.Add, (stats, value, op, sign) => ApplyToInt(ref stats.projectileCount, value, op, sign));
        Register(StatIds.ProjectileSpreadAngle, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.projectileSpreadAngle, value, op, sign));
        Register(StatIds.WeaponAttackSpeed, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.weaponAttackSpeed, value, op, sign));
        Register(StatIds.ChainCount, StatOperation.Add, (stats, value, op, sign) => ApplyToInt(ref stats.chainCount, value, op, sign));
        Register(StatIds.SplitCount, StatOperation.Add, (stats, value, op, sign) => ApplyToInt(ref stats.splitCount, value, op, sign));
        Register(StatIds.Armor, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.armor, value, op, sign));
        Register(StatIds.DodgeChance, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.dodgeChance, value, op, sign));
        Register(StatIds.XpGainMultiplier, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.xpGainMultiplier, value, op, sign));
        Register(StatIds.CooldownReduction, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.cooldownReduction, value, op, sign));
        Register(StatIds.WeaponDamageMin, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.weaponDamageMin, value, op, sign));
        Register(StatIds.WeaponDamageMax, StatOperation.Add, (stats, value, op, sign) => ApplyToFloat(ref stats.weaponDamageMax, value, op, sign));
        Register(StatIds.WeaponDamage, StatOperation.Add, ApplyToWeaponDamageRange);
    }

    public static IReadOnlyCollection<string> KnownStatIds => Definitions.Keys;

    public static bool TryApply(CharacterStats stats, StatEntry entry, int sign)
    {
        if (stats == null || entry == null || string.IsNullOrWhiteSpace(entry.statId))
            return false;

        if (!Definitions.TryGetValue(entry.statId, out StatDefinition definition))
            return false;

        StatOperation op = entry.operation == StatOperation.Default ? definition.defaultOperation : entry.operation;
        definition.apply(stats, entry.value, op, sign);
        return true;
    }

    private static void Register(string statId, StatOperation defaultOperation, Action<CharacterStats, float, StatOperation, int> applier)
    {
        Definitions[statId] = new StatDefinition(defaultOperation, applier);
    }

    private static void ApplyToFloat(ref float target, float value, StatOperation operation, int sign)
    {
        switch (operation)
        {
            case StatOperation.Multiply:
                target *= ComputeMultiplier(value, sign);
                break;
            case StatOperation.Add:
            default:
                target += sign * value;
                break;
        }
    }

    private static void ApplyToInt(ref int target, float value, StatOperation operation, int sign)
    {
        switch (operation)
        {
            case StatOperation.Multiply:
                target = Mathf.RoundToInt(target * ComputeMultiplier(value, sign));
                break;
            case StatOperation.Add:
            default:
                target += sign * Mathf.RoundToInt(value);
                break;
        }
    }

    private static float ComputeMultiplier(float value, int sign)
    {
        float factor = 1f + value;
        if (Mathf.Approximately(factor, 0f))
            factor = 0.0001f; // prevent divide-by-zero when removing

        return sign >= 0 ? factor : 1f / factor;
    }

    private static void ApplyToWeaponDamageRange(CharacterStats stats, float value, StatOperation operation, int sign)
    {
        ApplyToFloat(ref stats.weaponDamageMin, value, operation, sign);
        ApplyToFloat(ref stats.weaponDamageMax, value, operation, sign);
    }

    private readonly struct StatDefinition
    {
        public readonly StatOperation defaultOperation;
        public readonly Action<CharacterStats, float, StatOperation, int> apply;

        public StatDefinition(StatOperation defaultOperation, Action<CharacterStats, float, StatOperation, int> apply)
        {
            this.defaultOperation = defaultOperation;
            this.apply = apply;
        }
    }
}
