using UnityEngine;

/// <summary>
/// Helper for migrating legacy StatModifier serialized fields into the new entry-driven schema.
/// </summary>
public static class StatModifierMigrationUtility
{
#pragma warning disable 618
    public static bool MigrateLegacyValues(StatModifier modifier)
    {
        if (modifier == null)
            return false;

        bool changed = false;

        EnsureEntriesList(modifier);

        changed |= ConsumeLegacy(ref modifier.maxHealth, StatRegistry.StatIds.MaxHealth, modifier);
        changed |= ConsumeLegacy(ref modifier.moveSpeed, StatRegistry.StatIds.MoveSpeed, modifier);
        changed |= ConsumeLegacy(ref modifier.baseDamage, StatRegistry.StatIds.BaseDamage, modifier);
        changed |= ConsumeLegacy(ref modifier.critChance, StatRegistry.StatIds.CritChance, modifier);
        changed |= ConsumeLegacy(ref modifier.critMultiplier, StatRegistry.StatIds.CritMultiplier, modifier);
        changed |= ConsumeLegacy(ref modifier.attackSpeedMultiplier, StatRegistry.StatIds.AttackSpeedMultiplier, modifier);
        changed |= ConsumeLegacy(ref modifier.projectileCount, StatRegistry.StatIds.ProjectileCount, modifier);
        changed |= ConsumeLegacy(ref modifier.projectileSpreadAngle, StatRegistry.StatIds.ProjectileSpreadAngle, modifier);
        changed |= ConsumeLegacy(ref modifier.weaponAttackSpeed, StatRegistry.StatIds.WeaponAttackSpeed, modifier);
        changed |= ConsumeLegacy(ref modifier.chainCount, StatRegistry.StatIds.ChainCount, modifier);
        changed |= ConsumeLegacy(ref modifier.splitCount, StatRegistry.StatIds.SplitCount, modifier);
        changed |= ConsumeLegacy(ref modifier.armor, StatRegistry.StatIds.Armor, modifier);
        changed |= ConsumeLegacy(ref modifier.dodgeChance, StatRegistry.StatIds.DodgeChance, modifier);
        changed |= ConsumeLegacy(ref modifier.xpGainMultiplier, StatRegistry.StatIds.XpGainMultiplier, modifier);
        changed |= ConsumeLegacy(ref modifier.cooldownReduction, StatRegistry.StatIds.CooldownReduction, modifier);
        changed |= ConsumeLegacy(ref modifier.weaponDamageMin, StatRegistry.StatIds.WeaponDamageMin, modifier);
        changed |= ConsumeLegacy(ref modifier.weaponDamageMax, StatRegistry.StatIds.WeaponDamageMax, modifier);

        return changed;
    }

    public static void ClearLegacyFields(StatModifier modifier)
    {
        if (modifier == null)
            return;

        modifier.maxHealth = 0f;
        modifier.moveSpeed = 0f;
        modifier.baseDamage = 0f;
        modifier.critChance = 0f;
        modifier.critMultiplier = 0f;
        modifier.attackSpeedMultiplier = 0f;
        modifier.projectileCount = 0;
        modifier.projectileSpreadAngle = 0f;
        modifier.weaponAttackSpeed = 0f;
        modifier.chainCount = 0;
        modifier.splitCount = 0;
        modifier.armor = 0f;
        modifier.dodgeChance = 0f;
        modifier.xpGainMultiplier = 0f;
        modifier.cooldownReduction = 0f;
        modifier.weaponDamageMin = 0f;
        modifier.weaponDamageMax = 0f;
    }
#pragma warning restore 618

    private static bool ConsumeLegacy(ref float value, string statId, StatModifier modifier)
    {
        if (Mathf.Approximately(value, 0f))
            return false;

        modifier.AddEntry(statId, value);
        value = 0f;
        return true;
    }

    private static bool ConsumeLegacy(ref int value, string statId, StatModifier modifier)
    {
        if (value == 0)
            return false;

        modifier.AddEntry(statId, value);
        value = 0;
        return true;
    }

    private static void EnsureEntriesList(StatModifier modifier)
    {
        if (modifier.entries == null)
            modifier.entries = new System.Collections.Generic.List<StatEntry>();
    }
}
