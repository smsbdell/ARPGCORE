using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemId;
    public string displayName;
    public string description;
    public Sprite icon;

    public bool isStackable = true;
    public int currentStack = 1;
    public int maxStack = 99;

    public virtual InventoryItem CloneForStack(int stackSize)
    {
        InventoryItem clone = (InventoryItem)MemberwiseClone();
        clone.currentStack = stackSize;
        return clone;
    }
}
