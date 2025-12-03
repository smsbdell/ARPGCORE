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
            InventoryItem clone = newItem.CloneForStack(stackToCreate);

            items.Add(clone);
            newItem.currentStack -= stackToCreate;
        }

        return true;
    }

    public bool ContainsAtLeast(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        return GetTotalCount(itemId) >= amount;
    }

    public int GetTotalCount(string itemId)
    {
        int count = 0;
        if (string.IsNullOrWhiteSpace(itemId))
            return count;

        foreach (InventoryItem item in items)
        {
            if (item == null)
                continue;

            if (string.Equals(item.itemId, itemId, System.StringComparison.Ordinal))
            {
                count += Mathf.Max(0, item.currentStack);
            }
        }

        return count;
    }

    public bool TryConsume(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        int remaining = amount;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventoryItem item = items[i];
            if (item == null || !string.Equals(item.itemId, itemId, System.StringComparison.Ordinal))
                continue;

            int toTake = Mathf.Min(item.currentStack, remaining);
            item.currentStack -= toTake;
            remaining -= toTake;

            if (item.currentStack <= 0)
            {
                items.RemoveAt(i);
            }
        }

        return remaining == 0;
    }

    public void RemoveItem(InventoryItem item)
    {
        items.Remove(item);
    }
}
