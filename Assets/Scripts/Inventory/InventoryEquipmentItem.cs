using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryEquipmentItem : InventoryItem
{
    [Tooltip("ID of the static equipment template this item was generated from.")]
    public string equipmentId;

    [Tooltip("Runtime equipment instance containing the rolled modifiers.")]
    public EquipmentItem equipment;

    [Tooltip("Affixes rolled onto this item at generation time.")]
    public List<AffixInstance> affixes = new List<AffixInstance>();

    public InventoryEquipmentItem()
    {
        isStackable = false;
        maxStack = 1;
        currentStack = 1;
    }

    public override InventoryItem CloneForStack(int stackSize)
    {
        InventoryEquipmentItem clone = (InventoryEquipmentItem)MemberwiseClone();
        clone.currentStack = stackSize;
        clone.affixes = new List<AffixInstance>();
        foreach (AffixInstance affix in affixes)
        {
            if (affix == null)
                continue;

            clone.affixes.Add(affix.Clone());
        }

        if (equipment != null)
        {
            EquipmentItem equipmentClone = ScriptableObject.CreateInstance<EquipmentItem>();
            equipmentClone.id = equipment.id;
            equipmentClone.displayName = equipment.displayName;
            equipmentClone.description = equipment.description;
            equipmentClone.icon = equipment.icon;
            equipmentClone.slot = equipment.slot;
            equipmentClone.tags = equipment.tags != null ? (string[])equipment.tags.Clone() : null;
            equipmentClone.modifiers = equipment.modifiers != null ? equipment.modifiers.Clone() : null;
            clone.equipment = equipmentClone;
        }

        return clone;
    }
}
