using UnityEngine;

public enum CurrencyType
{
    RerollOrb,
    AugmentShard
}

[CreateAssetMenu(menuName = "ARPG/Currency Item", fileName = "CurrencyItem")]
public class CurrencyItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public CurrencyType currencyType;
    public string id = CurrencyIds.RerollOrb;

    [Header("Presentation")]
    public string displayName = "Currency";
    [TextArea]
    public string description;
    public Sprite icon;

    [Header("Stacking")]
    public int maxStack = 20;

    public InventoryItem ToInventoryItem(int stackSize = 1)
    {
        InventoryItem item = new InventoryItem
        {
            itemId = string.IsNullOrWhiteSpace(id) ? currencyType.ToString() : id,
            displayName = displayName,
            description = description,
            icon = icon,
            isStackable = true,
            maxStack = Mathf.Max(1, maxStack),
            currentStack = Mathf.Clamp(stackSize, 1, Mathf.Max(1, maxStack))
        };

        return item;
    }
}

public static class CurrencyIds
{
    public const string RerollOrb = "RerollOrb";
    public const string AugmentShard = "AugmentShard";
}
