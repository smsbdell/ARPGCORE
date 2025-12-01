using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Inventory Settings")]
    public int capacity = 30;

    [Header("Runtime")]
    public List<InventoryItem> items = new List<InventoryItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddItem(InventoryItem newItem)
    {
        if (newItem.isStackable)
        {
            InventoryItem existing = items.Find(i => i.itemId == newItem.itemId && i.currentStack < i.maxStack);
            if (existing != null)
            {
                int availableSpace = existing.maxStack - existing.currentStack;
                int toAdd = Mathf.Min(availableSpace, newItem.currentStack);
                existing.currentStack += toAdd;
                newItem.currentStack -= toAdd;

                if (newItem.currentStack <= 0)
                    return true;
            }
        }

        while (newItem.currentStack > 0)
        {
            if (items.Count >= capacity)
            {
                Debug.Log("Inventory full!");
                return false;
            }

            int stackToCreate = Mathf.Min(newItem.currentStack, newItem.maxStack);
            InventoryItem clone = new InventoryItem
            {
                itemId = newItem.itemId,
                displayName = newItem.displayName,
                description = newItem.description,
                icon = newItem.icon,
                isStackable = newItem.isStackable,
                maxStack = newItem.maxStack,
                currentStack = stackToCreate
            };

            items.Add(clone);
            newItem.currentStack -= stackToCreate;
        }

        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        items.Remove(item);
    }
}
