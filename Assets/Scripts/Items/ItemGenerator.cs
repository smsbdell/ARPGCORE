using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentRarity
{
    Common,
    Magic,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class RarityDefinition
{
    public EquipmentRarity rarity;

    [Tooltip("Minimum number of affixes that will be rolled for this rarity.")]
    public int minAffixes = 0;

    [Tooltip("Maximum number of affixes that will be rolled for this rarity.")]
    public int maxAffixes = 0;
}

/// <summary>
/// Generates runtime equipment instances by combining base items with rolled affixes.
/// </summary>
public class ItemGenerator : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Base item database used as templates for generation.")]
    public EquipmentDatabase equipmentDatabase;

    [Tooltip("Pool of possible affixes.")]
    public AffixPool affixPool;

    [Header("Affix Counts By Rarity")]
    public List<RarityDefinition> rarityDefinitions = new List<RarityDefinition>
    {
        new RarityDefinition { rarity = EquipmentRarity.Common, minAffixes = 0, maxAffixes = 0 },
        new RarityDefinition { rarity = EquipmentRarity.Magic, minAffixes = 1, maxAffixes = 2 },
        new RarityDefinition { rarity = EquipmentRarity.Rare, minAffixes = 2, maxAffixes = 3 },
        new RarityDefinition { rarity = EquipmentRarity.Epic, minAffixes = 3, maxAffixes = 4 },
        new RarityDefinition { rarity = EquipmentRarity.Legendary, minAffixes = 4, maxAffixes = 5 },
    };

    private readonly Dictionary<EquipmentRarity, RarityDefinition> _rarityLookup = new Dictionary<EquipmentRarity, RarityDefinition>();
    private bool _rarityInitialized;

    private void Awake()
    {
        InitializeRarityLookup();
    }

    public InventoryEquipmentItem Generate(string baseItemId, int itemLevel, EquipmentRarity rarity)
    {
        if (string.IsNullOrWhiteSpace(baseItemId))
        {
            Debug.LogWarning("ItemGenerator: base item id was empty.");
            return null;
        }

        if (equipmentDatabase == null)
        {
            Debug.LogWarning("ItemGenerator: equipment database is not assigned.");
            return null;
        }

        EquipmentItem template = equipmentDatabase.GetItemOrDefault(baseItemId);
        if (template == null)
        {
            Debug.LogWarning($"ItemGenerator: base item '{baseItemId}' was not found.");
            return null;
        }

        StatModifier finalModifier = template.modifiers != null ? template.modifiers.Clone() : new StatModifier();
        List<AffixInstance> rolledAffixes = RollAffixes(template, itemLevel, rarity);

        foreach (AffixInstance affix in rolledAffixes)
        {
            finalModifier.AddEntriesFrom(affix.BuildModifier());
        }

        EquipmentItem generatedItem = ScriptableObject.CreateInstance<EquipmentItem>();
        generatedItem.id = template.id;
        generatedItem.displayName = BuildGeneratedName(template.displayName, rolledAffixes);
        generatedItem.description = template.description;
        generatedItem.icon = template.icon;
        generatedItem.slot = template.slot;
        generatedItem.tags = template.tags;
        generatedItem.modifiers = finalModifier;

        InventoryEquipmentItem inventoryItem = new InventoryEquipmentItem
        {
            equipmentId = template.id,
            equipment = generatedItem,
            affixes = rolledAffixes,
            itemId = template.id,
            displayName = generatedItem.displayName,
            description = template.description,
            icon = template.icon,
            isStackable = false,
            maxStack = 1,
            currentStack = 1
        };

        return inventoryItem;
    }

    private List<AffixInstance> RollAffixes(EquipmentItem template, int itemLevel, EquipmentRarity rarity)
    {
        List<AffixInstance> results = new List<AffixInstance>();
        if (affixPool == null)
            return results;

        List<AffixDefinition> eligible = affixPool.GetEligibleAffixes(template, itemLevel);
        if (eligible.Count == 0)
            return results;

        int affixCount = GetAffixCountForRarity(rarity);
        affixCount = Mathf.Clamp(affixCount, 0, eligible.Count);

        for (int i = 0; i < affixCount; i++)
        {
            AffixDefinition chosen = DrawWeightedAffix(eligible);
            if (chosen == null)
                break;

            results.Add(chosen.CreateInstance());
            eligible.Remove(chosen);
        }

        return results;
    }

    private AffixDefinition DrawWeightedAffix(List<AffixDefinition> candidates)
    {
        float totalWeight = 0f;
        foreach (AffixDefinition affix in candidates)
        {
            if (affix != null && affix.weight > 0f)
            {
                totalWeight += affix.weight;
            }
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (AffixDefinition affix in candidates)
        {
            if (affix == null || affix.weight <= 0f)
                continue;

            roll -= affix.weight;
            if (roll <= 0f)
            {
                return affix;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private int GetAffixCountForRarity(EquipmentRarity rarity)
    {
        InitializeRarityLookup();
        if (_rarityLookup.TryGetValue(rarity, out RarityDefinition definition))
        {
            return UnityEngine.Random.Range(definition.minAffixes, definition.maxAffixes + 1);
        }

        return 0;
    }

    private void InitializeRarityLookup()
    {
        if (_rarityInitialized)
            return;

        _rarityInitialized = true;
        _rarityLookup.Clear();
        foreach (RarityDefinition definition in rarityDefinitions)
        {
            if (definition == null)
                continue;

            _rarityLookup[definition.rarity] = definition;
        }
    }

    private string BuildGeneratedName(string baseName, IReadOnlyList<AffixInstance> affixes)
    {
        if (affixes == null || affixes.Count == 0)
            return baseName;

        // Simple format: first affix name + base name (e.g., "Fiery Sword").
        AffixInstance prefix = affixes[0];
        if (prefix == null || string.IsNullOrWhiteSpace(prefix.displayName))
            return baseName;

        return $"{prefix.displayName} {baseName}".Trim();
    }
}
