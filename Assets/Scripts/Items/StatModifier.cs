using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a single stat change for a piece of equipment.
/// </summary>
[Serializable]
public class StatEntry
{
    public string statId;
    public float value;
    public StatOperation operation = StatOperation.Default;

    public StatEntry() { }

    public StatEntry(string statId, float value, StatOperation operation = StatOperation.Default)
    {
        this.statId = statId;
        this.value = value;
        this.operation = operation;
    }
}

public enum StatOperation
{
    /// <summary>
    /// Use the operation configured in the StatRegistry for this stat.
    /// </summary>
    Default = 0,
    Add = 1,
    Multiply = 2
}

/// <summary>
/// A collection of stat changes (additive or multiplicative) for an item.
/// Includes automatic migration from legacy serialized fields to the dynamic entry list.
/// </summary>
[Serializable]
public class StatModifier : ISerializationCallbackReceiver
{
    public List<StatEntry> entries = new List<StatEntry>();

    // Legacy fields kept for migration only. They are consumed into entries on deserialize.
#pragma warning disable 649
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float maxHealth;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float moveSpeed;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float baseDamage;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float critChance;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float critMultiplier;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float attackSpeedMultiplier;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal int projectileCount;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float projectileSpreadAngle;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float weaponAttackSpeed;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal int chainCount;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal int splitCount;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float armor;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float dodgeChance;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float xpGainMultiplier;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float cooldownReduction;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float weaponDamageMin;
    [SerializeField, HideInInspector, Obsolete("Use entries instead")] internal float weaponDamageMax;
#pragma warning restore 649

    public IReadOnlyList<StatEntry> Entries => entries;

    public StatModifier Clone()
    {
        StatModifier copy = new StatModifier();
        if (entries != null)
        {
            foreach (StatEntry entry in entries)
            {
                if (entry == null)
                    continue;

                copy.AddEntry(entry.statId, entry.value, entry.operation);
            }
        }

        return copy;
    }

    public void AddEntry(string statId, float value, StatOperation operation = StatOperation.Default)
    {
        if (entries == null)
            entries = new List<StatEntry>();

        entries.Add(new StatEntry(statId, value, operation));
    }

    public void AddEntriesFrom(StatModifier other)
    {
        if (other?.entries == null || other.entries.Count == 0)
            return;

        if (entries == null)
            entries = new List<StatEntry>();

        entries.AddRange(other.entries);
    }

    public void ClearEntries()
    {
        entries?.Clear();
    }

    public void OnBeforeSerialize()
    {
        // Legacy fields are intentionally left as-is so the Unity editor can show their values
        // for older assets. They will be consumed on deserialize via the migration helper.
    }

    public void OnAfterDeserialize()
    {
        StatModifierMigrationUtility.MigrateLegacyValues(this);
    }
}
