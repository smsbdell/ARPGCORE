using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string FireDamageMin = "fireDamageMin";
        public const string FireDamageMax = "fireDamageMax";
        public const string ColdDamageMin = "coldDamageMin";
        public const string ColdDamageMax = "coldDamageMax";
        public const string LightningDamageMin = "lightningDamageMin";
        public const string LightningDamageMax = "lightningDamageMax";
        public const string FireResistance = "fireResistance";
        public const string ColdResistance = "coldResistance";
        public const string LightningResistance = "lightningResistance";
        public const string ShockDamageChance = "shockDamageChance";
        public const string AllowedSkillTags = "allowedSkillTags";
    }

    private static readonly Dictionary<string, StatDefinition> Definitions = new Dictionary<string, StatDefinition>();
    private static List<string> _knownStatIds;

    static StatRegistry()
    {
        Register(StatIds.MaxHealth, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.maxHealth, entry.value, op, sign));
        Register(StatIds.MoveSpeed, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.moveSpeed, entry.value, op, sign));
        Register(StatIds.BaseDamage, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.baseDamage, entry.value, op, sign));
        Register(StatIds.CritChance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.critChance, entry.value, op, sign));
        Register(StatIds.CritMultiplier, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.critMultiplier, entry.value, op, sign));
        Register(StatIds.AttackSpeedMultiplier, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.attackSpeedMultiplier, entry.value, op, sign));
        Register(StatIds.ProjectileCount, StatOperation.Add, (stats, entry, op, sign) => ApplyToInt(ref stats.projectileCount, entry.value, op, sign));
        Register(StatIds.ProjectileSpreadAngle, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.projectileSpreadAngle, entry.value, op, sign));
        Register(StatIds.WeaponAttackSpeed, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.weaponAttackSpeed, entry.value, op, sign));
        Register(StatIds.ChainCount, StatOperation.Add, (stats, entry, op, sign) => ApplyToInt(ref stats.chainCount, entry.value, op, sign));
        Register(StatIds.SplitCount, StatOperation.Add, (stats, entry, op, sign) => ApplyToInt(ref stats.splitCount, entry.value, op, sign));
        Register(StatIds.Armor, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.armor, entry.value, op, sign));
        Register(StatIds.DodgeChance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.dodgeChance, entry.value, op, sign));
        Register(StatIds.XpGainMultiplier, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.xpGainMultiplier, entry.value, op, sign));
        Register(StatIds.CooldownReduction, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.cooldownReduction, entry.value, op, sign));
        Register(StatIds.WeaponDamageMin, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.weaponDamageMin, entry.value, op, sign));
        Register(StatIds.WeaponDamageMax, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.weaponDamageMax, entry.value, op, sign));
        Register(StatIds.WeaponDamage, StatOperation.Add, (stats, entry, op, sign) => ApplyToWeaponDamageRange(stats, entry.value, op, sign));
        Register(StatIds.FireDamageMin, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.fireDamageMin, entry.value, op, sign));
        Register(StatIds.FireDamageMax, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.fireDamageMax, entry.value, op, sign));
        Register(StatIds.ColdDamageMin, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.coldDamageMin, entry.value, op, sign));
        Register(StatIds.ColdDamageMax, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.coldDamageMax, entry.value, op, sign));
        Register(StatIds.LightningDamageMin, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.lightningDamageMin, entry.value, op, sign));
        Register(StatIds.LightningDamageMax, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.lightningDamageMax, entry.value, op, sign));
        Register(StatIds.FireResistance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.fireResistance, entry.value, op, sign));
        Register(StatIds.ColdResistance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.coldResistance, entry.value, op, sign));
        Register(StatIds.LightningResistance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.lightningResistance, entry.value, op, sign));
        Register(StatIds.ShockDamageChance, StatOperation.Add, (stats, entry, op, sign) => ApplyToFloat(ref stats.shockDamageChance, entry.value, op, sign));
        Register(StatIds.AllowedSkillTags, StatOperation.Add, ApplySkillTagAllowance);
    }

    public static IReadOnlyList<string> KnownStatIds
    {
        get
        {
            if (_knownStatIds == null)
                _knownStatIds = Definitions.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();

            return _knownStatIds;
        }
    }

    public static bool TryApply(CharacterStats stats, StatEntry entry, int sign)
    {
        if (stats == null || entry == null || string.IsNullOrWhiteSpace(entry.statId))
            return false;

        if (!Definitions.TryGetValue(entry.statId, out StatDefinition definition))
            return false;

        StatOperation op = entry.operation == StatOperation.Default ? definition.defaultOperation : entry.operation;
        definition.apply(stats, entry, op, sign);
        return true;
    }

    private static void Register(string statId, StatOperation defaultOperation, Action<CharacterStats, StatEntry, StatOperation, int> applier)
    {
        Definitions[statId] = new StatDefinition(defaultOperation, applier);
        _knownStatIds = null;
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

    private static void ApplySkillTagAllowance(CharacterStats stats, StatEntry entry, StatOperation operation, int sign)
    {
        if (stats == null || entry == null)
            return;

        if (string.IsNullOrWhiteSpace(entry.stringValue))
            return;

        if (stats.allowedSkillTags == null)
            stats.allowedSkillTags = new List<string>();

        string normalized = entry.stringValue.Trim();
        if (string.IsNullOrEmpty(normalized))
            return;

        if (sign >= 0)
        {
            bool exists = stats.allowedSkillTags.Exists(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                stats.allowedSkillTags.Add(normalized);
        }
        else
        {
            stats.allowedSkillTags.RemoveAll(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase));
        }
    }

    private readonly struct StatDefinition
    {
        public readonly StatOperation defaultOperation;
        public readonly Action<CharacterStats, StatEntry, StatOperation, int> apply;

        public StatDefinition(StatOperation defaultOperation, Action<CharacterStats, StatEntry, StatOperation, int> apply)
        {
            this.defaultOperation = defaultOperation;
            this.apply = apply;
        }
    }
}
