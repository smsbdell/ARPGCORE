using System.Collections.Generic;
using UnityEngine;

public class CurrencyService : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemGenerator itemGenerator;
    [SerializeField] private EquipmentDatabase equipmentDatabase;
    [SerializeField] private PlayerEquipment playerEquipment;

    [Header("Costs")]
    [SerializeField] private int rerollOrbCost = 1;
    [SerializeField] private int augmentShardCost = 3;

    [Header("Debug/Testing")]
    [SerializeField] private InventoryEquipmentItem debugTarget;

    private void Awake()
    {
        if (inventory == null)
            inventory = Inventory.Instance;

        if (itemGenerator == null)
            itemGenerator = FindObjectOfType<ItemGenerator>();

        if (equipmentDatabase == null && itemGenerator != null)
            equipmentDatabase = itemGenerator.equipmentDatabase;

        if (playerEquipment == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerEquipment = playerObj.GetComponent<PlayerEquipment>();
            }
        }
    }

    [ContextMenu("Reroll Target Item (Debug)")]
    public void RerollFromContextMenu()
    {
        if (debugTarget == null)
        {
            Debug.LogWarning("CurrencyService: Assign a target item before calling this debug entry point.");
            return;
        }

        TryRerollItem(debugTarget);
    }

    [ContextMenu("Augment Target Item (Debug)")]
    public void AugmentFromContextMenu()
    {
        if (debugTarget == null)
        {
            Debug.LogWarning("CurrencyService: Assign a target item before calling this debug entry point.");
            return;
        }

        TryAugmentItem(debugTarget);
    }

    public bool TryRerollItem(InventoryEquipmentItem target)
    {
        if (!ValidateTarget(target))
            return false;

        if (!ConsumeCurrency(CurrencyIds.RerollOrb, rerollOrbCost))
        {
            Debug.LogWarning("CurrencyService: Not enough Reroll Orbs.");
            return false;
        }

        EquipmentItem template = ResolveTemplate(target);
        if (template == null)
            return false;

        List<AffixInstance> newAffixes = itemGenerator.RollAffixes(template, target.itemLevel, target.rarity);
        ApplyAffixesToItem(target, template, newAffixes);

        return true;
    }

    public bool TryAugmentItem(InventoryEquipmentItem target)
    {
        if (!ValidateTarget(target))
            return false;

        int maxAffixes = itemGenerator.GetMaxAffixesForRarity(target.rarity);
        int currentAffixes = target.affixes != null ? target.affixes.Count : 0;
        if (currentAffixes >= maxAffixes)
        {
            Debug.Log("CurrencyService: Item already has maximum affixes for its rarity.");
            return false;
        }

        if (!ConsumeCurrency(CurrencyIds.AugmentShard, augmentShardCost))
        {
            Debug.LogWarning("CurrencyService: Not enough Augment Shards.");
            return false;
        }

        EquipmentItem template = ResolveTemplate(target);
        if (template == null)
            return false;

        List<AffixDefinition> eligible = itemGenerator.GetEligibleAffixes(template, target.itemLevel);
        RemoveExistingAffixes(eligible, target.affixes);
        if (eligible.Count == 0)
        {
            Debug.LogWarning("CurrencyService: No eligible affixes remain to augment.");
            return false;
        }

        AffixDefinition rolled = itemGenerator.DrawWeightedAffix(eligible);
        if (rolled == null)
            return false;

        List<AffixInstance> updatedAffixes = target.affixes != null ? new List<AffixInstance>(target.affixes) : new List<AffixInstance>();
        updatedAffixes.Add(rolled.CreateInstance());
        ApplyAffixesToItem(target, template, updatedAffixes);

        return true;
    }

    public void ApplyAffixesToItem(InventoryEquipmentItem target, EquipmentItem template, List<AffixInstance> affixes)
    {
        if (target == null || template == null)
            return;

        EquipmentItem rebuilt = itemGenerator.BuildEquipmentFromTemplate(template, affixes);
        if (rebuilt == null)
            return;

        EquipmentItem previousInstance = target.equipment;
        target.affixes = affixes ?? new List<AffixInstance>();
        target.equipment = rebuilt;
        target.displayName = rebuilt.displayName;
        target.description = rebuilt.description;
        target.icon = rebuilt.icon;

        SyncEquippedItem(target, previousInstance);
    }

    public bool ConsumeCurrency(string currencyId, int amount)
    {
        if (inventory == null)
        {
            Debug.LogWarning("CurrencyService: Inventory reference missing.");
            return false;
        }

        if (!inventory.ContainsAtLeast(currencyId, amount))
            return false;

        return inventory.TryConsume(currencyId, amount);
    }

    private EquipmentItem ResolveTemplate(InventoryEquipmentItem target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.equipmentId))
            return null;

        if (equipmentDatabase == null)
        {
            Debug.LogWarning("CurrencyService: Equipment database reference missing.");
            return null;
        }

        return equipmentDatabase.GetItemOrDefault(target.equipmentId);
    }

    private bool ValidateTarget(InventoryEquipmentItem target)
    {
        if (target == null)
        {
            Debug.LogWarning("CurrencyService: No item selected.");
            return false;
        }

        if (itemGenerator == null)
        {
            Debug.LogWarning("CurrencyService: ItemGenerator dependency missing.");
            return false;
        }

        if (target.equipment == null || string.IsNullOrWhiteSpace(target.equipmentId))
        {
            Debug.LogWarning("CurrencyService: Target item does not include generated equipment data.");
            return false;
        }

        return true;
    }

    private void RemoveExistingAffixes(List<AffixDefinition> eligible, List<AffixInstance> existing)
    {
        if (eligible == null || existing == null)
            return;

        HashSet<string> existingIds = new HashSet<string>();
        foreach (AffixInstance affix in existing)
        {
            if (affix == null || string.IsNullOrWhiteSpace(affix.id))
                continue;

            existingIds.Add(affix.id);
        }

        eligible.RemoveAll(def => def != null && existingIds.Contains(def.id));
    }

    private void SyncEquippedItem(InventoryEquipmentItem target, EquipmentItem previousInstance)
    {
        if (playerEquipment == null || target == null || target.equipment == null)
            return;

        EquipmentItem equipped = playerEquipment.GetEquipped(target.equipment.slot);
        if (equipped == target.equipment || equipped == previousInstance)
        {
            playerEquipment.Equip(target.equipment);
        }
    }
}
