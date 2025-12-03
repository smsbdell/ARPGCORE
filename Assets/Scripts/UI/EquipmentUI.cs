using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // for Keyboard.current

[System.Serializable]
public class EquipmentSlotUI
{
    public EquipmentSlot slot;
    public Button button;
    public Image iconImage;
    public TMP_Text label;
}

public class EquipmentUI : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("The GameObject that contains the equipment UI (usually your EquipmentPanel).")]
    public GameObject panelRoot;

    [Header("References")]
    public PlayerEquipment playerEquipment;
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemGenerator itemGenerator;
    [SerializeField] private EquipmentCurrencyActionsUI currencyActionsUI;

    [Header("Data Source")]
    [Tooltip("Fallback list for testing without a database asset.")]
    public List<EquipmentItem> fallbackItems = new List<EquipmentItem>();

    [Header("Generation")]
    [Tooltip("Base item ids to roll when the inventory does not have any equipment yet.")]
    public List<string> starterItemIds = new List<string> { "test_bow_mainhand" };
    [Tooltip("Item level used when rolling starter gear.")]
    public int starterItemLevel = 1;
    [Tooltip("Rarity used when rolling starter gear.")]
    public EquipmentRarity starterRarity = EquipmentRarity.Magic;

    [Header("Item List UI")]
    public Transform itemListParent;
    public EquipmentItemUIEntry itemEntryPrefab;

    [Header("Slot UI")]
    public List<EquipmentSlotUI> slotUIs = new List<EquipmentSlotUI>();

    [Header("Visibility")]
    [Tooltip("Whether the equipment panel should start visible when the game runs.")]
    public bool startVisible = false;

    private EquipmentDatabase _database;
    private EquipmentItem _selectedEquipment;
    private InventoryEquipmentItem _selectedItem;
    private bool _isVisible;

    private void Awake()
    {
        if (panelRoot == null)
        {
            // Fallback to this object if not assigned, but usually you want a separate panel object.
            panelRoot = gameObject;
        }

        _isVisible = startVisible;
        ApplyVisibility();
    }

    private void Start()
    {
        if (playerEquipment == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerEquipment = playerObj.GetComponent<PlayerEquipment>();
            }
        }

        if (inventory == null)
            inventory = Inventory.Instance;

        EnsureItemGenerator();
        EnsureInventoryHasEquipment();
        BuildItemList();
        HookUpSlotButtons();
        AutoEquipEmptySlots();
        RefreshSlots();
    }

    private void Update()
    {
        // New Input System: Keyboard.current.iKey
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleVisibility();
        }
    }

    private void ApplyVisibility()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(_isVisible);
        }
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        ApplyVisibility();
    }

    private void BuildItemList()
    {
        if (itemListParent == null || itemEntryPrefab == null)
            return;

        for (int i = itemListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemListParent.GetChild(i).gameObject);
        }

        foreach (InventoryEquipmentItem item in GetAvailableItems())
        {
            if (item == null)
                continue;

            EquipmentItemUIEntry entry = Instantiate(itemEntryPrefab, itemListParent);
            entry.Init(item, this);
        }
    }

    private void HookUpSlotButtons()
    {
        foreach (var slotUI in slotUIs)
        {
            if (slotUI.button == null)
                continue;

            slotUI.button.onClick.RemoveAllListeners();
            EquipmentSlot capturedSlot = slotUI.slot;
            slotUI.button.onClick.AddListener(() => OnSlotClicked(capturedSlot));
        }
    }

    private void EnsureItemGenerator()
    {
        if (itemGenerator == null)
            itemGenerator = FindObjectOfType<ItemGenerator>();

        if (itemGenerator == null)
        {
            GameObject generatorObj = new GameObject("ItemGenerator");
            itemGenerator = generatorObj.AddComponent<ItemGenerator>();
        }

        if (_database == null && itemGenerator != null && itemGenerator.equipmentDatabase != null)
            _database = itemGenerator.equipmentDatabase;

        if (_database == null)
        {
            TextAsset data = Resources.Load<TextAsset>("Data/equipment_database");
            if (data != null)
            {
                _database = ScriptableObject.CreateInstance<EquipmentDatabase>();
                _database.dataFile = data;
            }
        }

        if (itemGenerator != null && itemGenerator.equipmentDatabase == null)
            itemGenerator.equipmentDatabase = _database;
    }

    private void EnsureInventoryHasEquipment()
    {
        if (inventory == null)
            return;

        List<InventoryEquipmentItem> inventoryItems = GetInventoryEquipmentItems();
        if (inventoryItems.Count > 0)
            return;

        if (itemGenerator != null)
        {
            foreach (string baseItemId in starterItemIds)
            {
                if (string.IsNullOrWhiteSpace(baseItemId))
                    continue;

                InventoryEquipmentItem generated = itemGenerator.Generate(baseItemId, starterItemLevel, starterRarity);
                if (generated == null)
                    continue;

                inventory.AddItem(generated);
            }
        }

        if (GetInventoryEquipmentItems().Count == 0 && fallbackItems.Count > 0)
        {
            foreach (EquipmentItem fallback in fallbackItems)
            {
                InventoryEquipmentItem wrapped = WrapFallbackItem(fallback);
                if (wrapped == null)
                    continue;

                inventory.AddItem(wrapped);
            }
        }
    }

    private List<InventoryEquipmentItem> GetAvailableItems()
    {
        List<InventoryEquipmentItem> items = GetInventoryEquipmentItems();

        if (items.Count == 0 && fallbackItems.Count > 0 && inventory != null)
        {
            foreach (EquipmentItem fallback in fallbackItems)
            {
                InventoryEquipmentItem wrapped = WrapFallbackItem(fallback);
                if (wrapped == null)
                    continue;

                if (inventory.AddItem(wrapped))
                {
                    if (inventory.items[inventory.items.Count - 1] is InventoryEquipmentItem added)
                        items.Add(added);
                }
            }
        }

        return items;
    }

    private List<InventoryEquipmentItem> GetInventoryEquipmentItems()
    {
        List<InventoryEquipmentItem> items = new List<InventoryEquipmentItem>();

        if (inventory == null || inventory.items == null)
            return items;

        foreach (InventoryItem item in inventory.items)
        {
            if (item is InventoryEquipmentItem equipmentItem && equipmentItem.equipment != null)
            {
                items.Add(equipmentItem);
            }
        }

        return items;
    }

    private InventoryEquipmentItem WrapFallbackItem(EquipmentItem fallback)
    {
        if (fallback == null)
            return null;

        return new InventoryEquipmentItem
        {
            equipmentId = fallback.id,
            equipment = fallback,
            rarity = EquipmentRarity.Common,
            itemLevel = 1,
            itemId = fallback.id,
            displayName = fallback.displayName,
            description = fallback.description,
            icon = fallback.icon,
            isStackable = false,
            maxStack = 1,
            currentStack = 1
        };
    }

    public void OnItemSelected(InventoryEquipmentItem item)
    {
        _selectedItem = item;
        _selectedEquipment = item != null ? item.equipment : null;

        if (currencyActionsUI != null)
            currencyActionsUI.BindTarget(item);
        // Optional: add highlight behavior here.
    }

    private void OnSlotClicked(EquipmentSlot slot)
    {
        if (_selectedEquipment == null)
        {
            Debug.Log("EquipmentUI: No item selected to equip.");
            return;
        }

        if (_selectedEquipment.slot != slot)
        {
            Debug.Log($"EquipmentUI: '{_selectedEquipment.displayName}' cannot go into slot '{slot}'. It belongs to '{_selectedEquipment.slot}'.");
            return;
        }

        if (playerEquipment == null)
        {
            Debug.LogWarning("EquipmentUI: playerEquipment not assigned.");
            return;
        }

        playerEquipment.Equip(_selectedEquipment);
        RefreshSlots();
    }

    private void AutoEquipEmptySlots()
    {
        if (playerEquipment == null)
            return;

        foreach (InventoryEquipmentItem item in GetInventoryEquipmentItems())
        {
            if (item == null || item.equipment == null)
                continue;

            EquipmentSlot slot = item.equipment.slot;
            if (playerEquipment.GetEquipped(slot) == null)
            {
                playerEquipment.Equip(item.equipment);
            }
        }
    }

    public void RefreshSlots()
    {
        if (playerEquipment == null)
            return;

        foreach (var slotUI in slotUIs)
        {
            if (slotUI == null)
                continue;

            EquipmentItem equipped = playerEquipment.GetEquipped(slotUI.slot);

            if (equipped != null)
            {
                if (slotUI.iconImage != null)
                {
                    if (equipped.icon != null)
                    {
                        slotUI.iconImage.enabled = true;
                        slotUI.iconImage.sprite = equipped.icon;
                    }
                    else
                    {
                        slotUI.iconImage.enabled = false;
                        slotUI.iconImage.sprite = null;
                    }
                }

                if (slotUI.label != null)
                {
                    slotUI.label.text = equipped.displayName;
                }
            }
            else
            {
                if (slotUI.iconImage != null)
                {
                    slotUI.iconImage.enabled = false;
                    slotUI.iconImage.sprite = null;
                }

                if (slotUI.label != null)
                {
                    slotUI.label.text = slotUI.slot.ToString();
                }
            }
        }
    }
}
