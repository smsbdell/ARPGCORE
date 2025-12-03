using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatRollRange
{
    [Tooltip("Stat identifier to roll (must exist in StatRegistry).")]
    public string statId;

    [Tooltip("Minimum possible rolled value.")]
    public float minValue = 1f;

    [Tooltip("Maximum possible rolled value.")]
    public float maxValue = 1f;

    [Tooltip("Operation used when applying this stat. Default defers to StatRegistry configuration.")]
    public StatOperation operation = StatOperation.Default;

    public float Roll()
    {
        float low = Mathf.Min(minValue, maxValue);
        float high = Mathf.Max(minValue, maxValue);
        return UnityEngine.Random.Range(low, high);
    }
}

[Serializable]
public class AffixDefinition
{
    public string id;
    public string displayName;
    [TextArea]
    public string description;

    [Tooltip("Relative selection weight when choosing among eligible affixes.")]
    public float weight = 1f;

    [Tooltip("Minimum required item level for this affix to appear.")]
    public int minLevel = 1;

    [Tooltip("Maximum item level for this affix. Use large values for no cap.")]
    public int maxLevel = 1000;

    [Tooltip("Optional slot whitelist. Leave empty to allow all slots.")]
    public EquipmentSlot[] allowedSlots = Array.Empty<EquipmentSlot>();

    [Tooltip("All tags the base item must include.")]
    public string[] requiredTags = Array.Empty<string>();

    [Tooltip("If the base item has any of these tags, the affix is disallowed.")]
    public string[] excludedTags = Array.Empty<string>();

    public List<StatRollRange> statRolls = new List<StatRollRange>();

    public bool IsEligible(EquipmentItem baseItem, int itemLevel)
    {
        if (baseItem == null)
            return false;

        if (itemLevel < minLevel || itemLevel > maxLevel)
            return false;

        if (allowedSlots != null && allowedSlots.Length > 0)
        {
            bool slotAllowed = false;
            foreach (EquipmentSlot slot in allowedSlots)
            {
                if (slot == baseItem.slot)
                {
                    slotAllowed = true;
                    break;
                }
            }

            if (!slotAllowed)
                return false;
        }

        if (HasTagConflict(baseItem.tags, excludedTags, true))
            return false;

        if (HasTagConflict(baseItem.tags, requiredTags, false))
            return false;

        return statRolls != null && statRolls.Count > 0;
    }

    public AffixInstance CreateInstance()
    {
        AffixInstance instance = new AffixInstance
        {
            id = id,
            displayName = displayName,
            description = description,
            rolledStats = new List<StatEntry>()
        };

        if (statRolls == null)
            return instance;

        foreach (StatRollRange roll in statRolls)
        {
            if (roll == null || string.IsNullOrWhiteSpace(roll.statId))
                continue;

            float value = roll.Roll();
            instance.rolledStats.Add(new StatEntry(roll.statId, value, roll.operation));
        }

        return instance;
    }

    private static bool HasTagConflict(IReadOnlyCollection<string> itemTags, IReadOnlyCollection<string> targetTags, bool disallowAny)
    {
        if (targetTags == null || targetTags.Count == 0)
            return false;

        if (itemTags == null)
            return disallowAny;

        int matches = 0;
        foreach (string tag in targetTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            foreach (string itemTag in itemTags)
            {
                if (string.Equals(tag, itemTag, StringComparison.OrdinalIgnoreCase))
                {
                    matches++;
                    if (disallowAny)
                        return true;
                    break;
                }
            }
        }

        if (disallowAny)
            return false;

        return matches < targetTags.Count;
    }
}

[Serializable]
public class AffixInstance
{
    public string id;
    public string displayName;
    public string description;
    public List<StatEntry> rolledStats = new List<StatEntry>();

    public StatModifier BuildModifier()
    {
        StatModifier modifier = new StatModifier();
        modifier.entries.AddRange(rolledStats);
        return modifier;
    }

    public AffixInstance Clone()
    {
        AffixInstance clone = new AffixInstance
        {
            id = id,
            displayName = displayName,
            description = description,
            rolledStats = new List<StatEntry>()
        };

        if (rolledStats != null)
        {
            foreach (StatEntry entry in rolledStats)
            {
                if (entry == null)
                    continue;

                clone.rolledStats.Add(new StatEntry(entry.statId, entry.value, entry.operation));
            }
        }

        return clone;
    }
}

[CreateAssetMenu(menuName = "ARPG/Affix Pool", fileName = "AffixPool")]
public class AffixPool : ScriptableObject
{
    [Tooltip("All affixes that can be rolled by the generator.")]
    public List<AffixDefinition> affixes = new List<AffixDefinition>();

    public List<AffixDefinition> GetEligibleAffixes(EquipmentItem item, int level)
    {
        List<AffixDefinition> eligible = new List<AffixDefinition>();
        if (affixes == null)
            return eligible;

        foreach (AffixDefinition affix in affixes)
        {
            if (affix != null && affix.IsEligible(item, level))
            {
                eligible.Add(affix);
            }
        }

        return eligible;
    }
}
